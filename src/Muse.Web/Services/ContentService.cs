using Muse.Web.Models;
using ReactiveGit;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using LibGit2Sharp;

namespace Muse.Web.Services
{
    public interface IContentService
    {
        Task GetLatestContent(IApplicationConfiguration config);
    }

    public class ContentService : IContentService
    {
        readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
        readonly BlogDB db;

        public ContentService(BlogDB db)
        {
            this.db = db;
        }

        public async Task GetLatestContent(IApplicationConfiguration config)
        {
            var repoURL = String.Format("https://github.com/{0}/{1}.git",
                config.Sync.owner, config.Sync.repo);
            bool cloneRepo = false;
            var cloneObserver = new ReplaySubject<Tuple<string, int>>();

            var localDirectory = new DirectoryInfo(config.Sync.locaStoragePath);
            if (!localDirectory.Exists) {
                localDirectory.Create();
            }

            if (!Directory.Exists(config.Sync.locaStoragePath + @"\.git")) {
                //flag to clone repo
                cloneRepo = true;
            } else {
                try {
                    //pull changes from repo
                    using (var repo = new ObservableRepository(config.Sync.locaStoragePath)) {
                        var u = await repo.Checkout(repo.Inner.Branches["master"], cloneObserver);
                        var mergeResult = await repo.Pull(cloneObserver);
                        if (config.Sync.branch != "master") {
                            u = await repo.Checkout(repo.Inner.Branches["origin/" + config.Sync.branch], cloneObserver);
                        }
                    }
                } catch {
                    //if cant pull changes, flag that the content should be cloned
                    cloneRepo = true;
                }
            }

            if (cloneRepo) {
                //clean directory if any files or subdirectories exist
                foreach (FileInfo file in localDirectory.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in localDirectory.GetDirectories()) {
                    dir.Delete(true);
                }

                using (var repo = await ObservableRepository.Clone(
                    repoURL,
                    config.Sync.locaStoragePath,
                    cloneObserver)) {
                    if (config.Sync.branch != "master") {
                        var u = await repo.Checkout(repo.Inner.Branches["origin/" + config.Sync.branch], cloneObserver);
                    }
                }
            }

            //scan _posts directory
            var deletePosts = db.Posts
                .Select(p => p.FileLocation)
                .Select(f => Path.Combine(siteBasePath, f))
                .Where(f => !File.Exists(f))
                .ToArray();
            var postsUpdater = new PostUpdater(db);
            foreach (var file in deletePosts) {
                postsUpdater.FileDeleted(file);
            }

            var posts = localDirectory.GetDirectories()
                .Where(d => d.Name == "_posts")
                .SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories));
            foreach (var file in posts) {
                postsUpdater.FileUpdated(file.FullName);
            }

            //scan _pages directory
            var deletePages = db.Pages
                .Select(p => p.FileLocation)
                .Select(f => Path.Combine(siteBasePath, f))
                .Where(f => !File.Exists(f))
                .ToArray();
            var pagesUpdater = new PageUpdater(db);
            foreach (var file in deletePages) {
                pagesUpdater.FileDeleted(file);
            }

            var pages = localDirectory.GetDirectories()
                .Where(d => d.Name == "_pages")
                .SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories));
            foreach (var file in posts) {
                pagesUpdater.FileUpdated(file.FullName);
            }

            await config.ScanEnvironmentConfigFile();

            var atomFeed = GetAtomFeed(config);
            using (var xmlWriter = XmlWriter.Create(Path.Combine(siteBasePath, "App_Data/Content/atom.xml"))) {
                atomFeed.SaveAsAtom10(xmlWriter);
            }

            var sitemap = GetSitemap(config);
            File.WriteAllText(Path.Combine(siteBasePath, "App_Data/Content/sitemap.xml"), sitemap.ToString());
        }

        private XElement GetSitemap(IApplicationConfiguration config)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var homepage = new XElement[] {
                    new XElement(ns + "url",
                        new XElement(ns + "loc", config.BaseUrl)
                    )
                };

            var pages = db.Pages.Select(p => new XElement(ns + "url",
                new XElement(ns + "loc", config.BaseUrl + p.Href)
            ));

            var posts = db.Posts.Select(p => new XElement(ns + "url",
                new XElement(ns + "loc", config.BaseUrl + p.Href)
            ));

            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            return new XElement(ns + "urlset",
                new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(xsi + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                homepage.Concat(pages).Concat(posts).ToArray()
            );
        }

        private SyndicationFeed GetAtomFeed(IApplicationConfiguration config)
        {
            //TODO: Make configurable
            var defaultAuthor = new SyndicationPerson
            {
                Name = "Michael Tyson",
                Uri = config.BaseUrl + "/about"
            };

            var feedItems = db.Posts
                .Select(p => {
                    var lastWriteTime = File.GetLastWriteTime(Path.Combine(siteBasePath, p.FileLocation));

                    var item = new SyndicationItem(
                        p.Title,
                        SyndicationContent.CreateXhtmlContent(p.Content),
                        new Uri(config.BaseUrl + p.Href),
                        p.Href,
                        new DateTimeOffset(lastWriteTime));

                    item.PublishDate = new DateTimeOffset(p.Date);

                    item.Authors.Add((p.AuthorName == null && p.AuthorUrl == null) ?
                        defaultAuthor :
                        new SyndicationPerson
                        {
                            Name = p.AuthorName,
                            Uri = (!p.AuthorUrl.StartsWith("http")) ?
                                config.BaseUrl + p.AuthorUrl :
                                p.AuthorUrl
                        });

                    return item;
                });

            var feed = new SyndicationFeed(
                config.SiteTitle,
                config.SiteSubTitle,
                new Uri(config.BaseUrl),
                config.BaseUrl,
                feedItems.Max(i => i.LastUpdatedTime),
                feedItems);

            var feedAuthors = feedItems.SelectMany(i => i.Authors)
                .GroupBy(a => a.Name)
                .Select(g => g.FirstOrDefault());

            foreach (var author in feedAuthors) {
                feed.Authors.Add(author);
            }

            return feed;
        }
    }
}