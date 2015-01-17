using System;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using Muse.Web.Models;

namespace Muse.Web.Services
{
    public interface IContentService
    {
        Task GetLatestContent(IApplicationConfiguration config);
    }

    public class ContentService: IContentService
    {
        readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
        readonly BlogDB db;

        public ContentService(BlogDB db)
        {
            this.db = db;
        }

        public async Task GetLatestContent(IApplicationConfiguration config)
        {
            if (!String.IsNullOrWhiteSpace(config.GitHubToken)) {
                var syncTasks = config.Sync.remoteFolders.Select(folder => {
                    var localDirectory = new DirectoryInfo(Path.Combine(config.Sync.locaStoragePath, folder));
                    if (!localDirectory.Exists) {
                        localDirectory.Create();
                    }

                    var githubMirror = GitHubMirror.Create(config.Sync, folder, config.GitHubToken);

                    if (folder == "_posts") {
                        return githubMirror.SynchronizeAsync().ContinueWith(t => {
                            var deletePosts = db.Posts
                                .Select(p => p.FileLocation)
                                .Select(f => Path.Combine(siteBasePath, f))
                                .Where(f => !File.Exists(f))
                                .ToArray();

                            var postsUpdater = new PostUpdater(db);
                            foreach (var file in deletePosts) {
                                postsUpdater.FileDeleted(file);
                            }

                            foreach (var file in Directory.EnumerateFiles(localDirectory.FullName, "*", SearchOption.AllDirectories)) {
                                postsUpdater.FileUpdated(file);
                            }
                        });
                    } else if (folder == "_pages") {
                        return githubMirror.SynchronizeAsync().ContinueWith(t => {
                            var deletePages = db.Pages
                                .Select(p => p.FileLocation)
                                .Select(f => Path.Combine(siteBasePath, f))
                                .Where(f => !File.Exists(f))
                                .ToArray();

                            var pagesUpdater = new PageUpdater(db);
                            foreach (var file in deletePages) {
                                pagesUpdater.FileDeleted(file);
                            }

                            foreach (var file in Directory.EnumerateFiles(localDirectory.FullName, "*", SearchOption.AllDirectories)) {
                                pagesUpdater.FileUpdated(file);
                            }
                        });
                    }

                    return githubMirror.SynchronizeAsync(false);
                });

                await Task.WhenAll(syncTasks);

                var atomFeed = GetAtomFeed(config);
                using (var xmlWriter = XmlWriter.Create(Path.Combine(siteBasePath, "App_Data/Content/atom.xml"))) {
                    atomFeed.SaveAsAtom10(xmlWriter);
                }

                var sitemap = GetSitemap(config);
                File.WriteAllText(Path.Combine(siteBasePath, "App_Data/Content/sitemap.xml"), sitemap.ToString());
            }
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
            var defaultAuthor = new SyndicationPerson {
                Name = "Michael Tyson",
                Uri = config.BaseUrl + "/about"
            };

            var feedItems = db.Posts
                .Select(p => {
                    var lastWriteTime = File.GetLastWriteTime(Path.Combine(siteBasePath, p.FileLocation));

                    var item = new SyndicationItem(
                        p.Title,
                        p.SubTitle,
                        new Uri(config.BaseUrl + p.Href),
                        p.Href,
                        new DateTimeOffset(lastWriteTime));

                    item.PublishDate = new DateTimeOffset(p.Date);

                    item.Authors.Add((p.AuthorName == null && p.AuthorUrl == null) ?
                        defaultAuthor :
                        new SyndicationPerson {
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