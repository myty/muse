using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml.Linq;
using Muse.Web.Models;
using Muse.Web.Models.Github;
using Muse.Web.Services;
using Muse.Web.ViewModels;
using Nancy;

namespace Muse.Web.Modules
{
    public class PageModule : BaseModule
    {
        readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
        readonly BlogDB db;
        readonly IApplicationConfiguration config;

        public PageModule(BlogDB db, IApplicationConfiguration config)
        {
            this.db = db;
            this.config = config;

            Get["/{slug}"] = (parameters) => {
                if (parameters.slug == "sitemap") {
                    return Sitemap();
                }

                if (!db.Pages.Any()) {
                    return Response.AsError(HttpStatusCode.InternalServerError);
                }

                var fileLocation = String.Format("App_Data\\Content\\Pages\\{0}.md", parameters.slug);

                var page = db.Pages.FirstOrDefault(p => p.FileLocation == fileLocation);

                if (page == null) {
                    return Response.AsError(HttpStatusCode.NotFound);
                }

                ViewBag.PageTitle = " - " + page.Title;

                return View[page.Layout, new PageDetailViewModel {
                    Page = page,
                    EditLink = GetEditLink(config.PagesSync, parameters.slug + ".md")
                }];
            };

            Post["/{slug}", true] = async (parameters, ct) => {
                if (parameters.slug == "sitemap") {
                    if (config.CanRefresh(Request)) {
                        await GetLatestContent();
                    }

                    return Sitemap();
                }

                return Response.AsError(HttpStatusCode.ServiceUnavailable);
            };
        }

        private async Task GetLatestContent()
        {
            if (!String.IsNullOrWhiteSpace(config.GitHubToken)) {
                var updatePostsTask = (config.PostsSync == null) ? Task.Delay(0) : GitHubMirror.Create(config.PostsSync, config.GitHubToken)
                    .SynchronizeAsync().ContinueWith(t => {
                        var deletePosts = db.Posts
                            .Select(p => p.FileLocation)
                            .Select(f => Path.Combine(siteBasePath, f))
                            .Where(f => !File.Exists(f))
                            .ToArray();

                        var postsUpdater = new PostUpdater(db);
                        foreach (var file in deletePosts) {
                            postsUpdater.FileDeleted(file);
                        }

                        foreach (var file in Directory.EnumerateFiles(config.PostsSync.locaPath, "*", SearchOption.AllDirectories)) {
                            postsUpdater.FileUpdated(file);
                        }
                    });

                var updatePagesTask = (config.PostsSync == null) ? Task.Delay(0) : GitHubMirror.Create(config.PagesSync, config.GitHubToken)
                    .SynchronizeAsync().ContinueWith(t => {
                        var deletePages = db.Pages
                            .Select(p => p.FileLocation)
                            .Select(f => Path.Combine(siteBasePath, f))
                            .Where(f => !File.Exists(f))
                            .ToArray();

                        var pagesUpdater = new PageUpdater(db);
                        foreach (var file in deletePages) {
                            pagesUpdater.FileDeleted(file);
                        }

                        foreach (var file in Directory.EnumerateFiles(config.PagesSync.locaPath, "*", SearchOption.AllDirectories)) {
                            pagesUpdater.FileUpdated(file);
                        }
                    });

                var updateOtherDirsTask = (config.OthersSync == null) ? Task.Delay(0) : Task.WhenAll(
                    config.OthersSync.Select(o =>
                        GitHubMirror.Create(o, config.GitHubToken).SynchronizeAsync(false)));

                await Task.WhenAll(
                    updatePostsTask,
                    updatePagesTask,
                    updateOtherDirsTask
                );
            }
        }

        private dynamic Sitemap()
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

            return Response.AsXml(homepage.Concat(pages).Concat(posts).ToArray());
        }
    }
}
