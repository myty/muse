using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml.Linq;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.Models.Github;
using MyTy.Blog.Web.Services;
using MyTy.Blog.Web.ViewModels;
using Nancy;
using Refit;

namespace MyTy.Blog.Web.Modules
{
	public class PageModule : NancyModule
	{
		readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
		readonly BlogDB db;
		readonly IApplicationConfiguration config;

		public PageModule(BlogDB db, IApplicationConfiguration config)
		{
			this.db = db;
			this.config = config;

			Get["/{slug}", true] = async (parameters, ct) => {
				if (parameters.slug == "sitemap") {
					return await Sitemap();
				}

				var fileLocation = String.Format("Pages\\{0}.md", parameters.slug);

				var page = db.Pages.FirstOrDefault(p => p.FileLocation == fileLocation);

				if (page == null) {
					return Response.AsError(HttpStatusCode.NotFound);
				}

				return View[page.Layout, new PageDetailViewModel {
					Page = page
				}];
			};
		}

		private async Task<dynamic> Sitemap()
		{
			var apikey = Request.Query["key"];
			if (config.CanRefresh(apikey)) {
				var postsPath = HostingEnvironment.MapPath("~/Posts");
				var pagesPath = HostingEnvironment.MapPath("~/Pages");

				//TODO: Get token from Github and store in config file that does not get pushed to source control
				var token = "";
				if (!String.IsNullOrWhiteSpace(token)) {
					var postsMirror = new GitHubMirror("myty", "shiny-myty-website", "src/MyTy.Blog.Web/Posts", postsPath, token);
					var pagesMirror = new GitHubMirror("myty", "shiny-myty-website", "src/MyTy.Blog.Web/Pages", pagesPath, token);

					var postsSynced = await postsMirror.SynchronizeAsync();
					var pagesSynced = await pagesMirror.SynchronizeAsync();
				}

				//POSTS
				var deletePosts = db.Posts
					.Select(p => p.FileLocation)
					.Select(f => Path.Combine(siteBasePath, f))
					.Where(f => !File.Exists(f))
					.ToArray();

				var postsUpdater = new PostUpdater(db);
				foreach (var file in deletePosts) {
					postsUpdater.FileDeleted(file);
				}

				foreach (var file in Directory.EnumerateFiles(postsPath, "*.md", SearchOption.AllDirectories)) {
					postsUpdater.FileUpdated(file);
				}

				//PAGES
				var deletePages = db.Pages
					.Select(p => p.FileLocation)
					.Select(f => Path.Combine(siteBasePath, f))
					.Where(f => !File.Exists(f))
					.ToArray();

				var pagesUpdater = new PageUpdater(db);
				foreach (var file in deletePages) {
					pagesUpdater.FileDeleted(file);
				}

				foreach (var file in Directory.EnumerateFiles(pagesPath, "*.md", SearchOption.AllDirectories)) {
					pagesUpdater.FileUpdated(file);
				}
			}

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