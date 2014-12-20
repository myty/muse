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
        const int MAX_POSTS_PER_PAGE = 10;

        public PostModule(BlogDB db, IApplicationConfiguration config)
		{
			Get["/"] = parameters => {
				ViewBag.Title = "Blog Title Goes Here";

                var page = (int)Request.Query["page"];
                page = (page <= 1) ? 0 : (page - 1);

				return View["Home", new PostIndexViewModel {
                    DisqusShortName = config.DisqusShortName,
                    Page = page + 1,
					Posts = db.Posts
                        .OrderByDescending(p => p.Date)
                        .Skip(MAX_POSTS_PER_PAGE * page)
                        .Take(MAX_POSTS_PER_PAGE)
                        .ToArray()
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
                    DisqusShortName = config.DisqusShortName,
					Post = post
				}];
			};
		}
	}
}
