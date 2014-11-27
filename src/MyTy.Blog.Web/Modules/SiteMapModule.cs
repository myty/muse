using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.ViewModels;
using Nancy;

namespace MyTy.Blog.Web.Modules
{
	public class SiteMapModule : NancyModule
	{		
		public SiteMapModule(BlogDB db, IApplicationConfiguration config)
		{
			Get["/sitemap.xml"] = parameters => {
				var apikey = Request.Query["key"];
				if (config.CanRefresh(apikey)) {
					//rebuild the pages and post dbs

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

				return Response.AsXml(homepage.Concat(pages).Concat(posts));
			};
		}
	}
}