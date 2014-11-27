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
	public class SiteMapModule : NancyModule
	{
		readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");

		public SiteMapModule(BlogDB db, IApplicationConfiguration config)
		{
			Get["/s/sitemap.xml"] = parameters => {
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
			};
		}
	}
}