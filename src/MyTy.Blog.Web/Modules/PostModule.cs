using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.ViewModels;
using Nancy;

namespace MyTy.Blog.Web.Modules
{
	public class PostModule : NancyModule
	{
		public PostModule(BlogDB db)
		{
			Get["/"] = parameters => {
				ViewBag.Title = "Blog Title Goes Here";
				
				return View["Home", new PostIndexViewModel {
					Page = 1,
					Posts = db.Posts.OrderByDescending(p => p.Date).ToArray()
				}];
			};

			Get["/{year}/{month}/{day}/{slug}"] = parameters => {
				var fileLocation = String.Format("App_Data\\Content\\Posts\\{0}\\{0}-{1}-{2}-{3}.md",
					parameters.year, parameters.month, parameters.day, parameters.slug);

				var post = db.Posts.FirstOrDefault(p => p.FileLocation == fileLocation);

				if (post == null) {
					return Response.AsError(HttpStatusCode.NotFound);
				}

				return View[post.Layout, new PostDetailViewModel {
					Post = post
				}];
			};
		}
	}
}