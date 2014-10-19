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
					Posts = db.Posts.OrderByDescending(p => p.PostDate).ToArray()
				}];
			};

			Get["/{slug}"] = parameters => {
				var post = db.Posts.FirstOrDefault(p => p.Slug == parameters.slug);

				return View["Post", new PostDetailViewModel {
					Post = post
				}];
			};
		}
	}
}