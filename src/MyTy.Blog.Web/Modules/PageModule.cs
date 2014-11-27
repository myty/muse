using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.ViewModels;
using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml.Linq;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.Services;
using MyTy.Blog.Web.ViewModels;
using Nancy;

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

			Get["/{slug}"] = parameters => {
				if (parameters.slug == "sitemap") {
					return Sitemap();
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

		private dynamic Sitemap()
		{
			var apikey = Request.Query["key"];
			if (config.CanRefresh(apikey)) {
				//rebuild the pages and post dbs
				//POSTS
				var deletePosts = db.Posts
					.Select(p => p.FileLocation)
					.Select(f => Path.Combine(siteBasePath, f))
					.Where(f => !File.Exists(f));

				var postsUpdater = new PostUpdater(db);
				foreach (var file in deletePosts) {
					postsUpdater.FileDeleted(file);
				}

				var postsPath = HostingEnvironment.MapPath("~/Posts");
				foreach (var file in Directory.EnumerateFiles(postsPath, "*.md", SearchOption.AllDirectories)) {
					postsUpdater.FileUpdated(file);
				}

				//PAGES
				var deletePages = db.Pages
					.Select(p => p.FileLocation)
					.Select(f => Path.Combine(siteBasePath, f))
					.Where(f => !File.Exists(f));

				var pagesUpdater = new PageUpdater(db);
				foreach (var file in deletePages) {
					pagesUpdater.FileDeleted(file);
				}

				var pagesPath = HostingEnvironment.MapPath("~/Pages");
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