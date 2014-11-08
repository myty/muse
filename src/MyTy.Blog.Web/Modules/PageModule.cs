using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.ViewModels;
using Nancy;

namespace MyTy.Blog.Web.Modules
{
	public class PageModule : NancyModule
	{
		public PageModule(BlogDB db)
		{
			Get["/{slug}"] = parameters => {
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
	}
}