﻿using System;
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

                var page = Int32.Parse((string)Request.Query["page"] ?? "1");
                page = (page <= 1) ? 0 : (page - 1);

                var allPosts = db.Posts
                    .OrderByDescending(p => p.Date);

                var posts = allPosts
                    .Skip(MAX_POSTS_PER_PAGE * page)
                    .Take(MAX_POSTS_PER_PAGE)
                    .ToArray();

                if (!posts.Any()) {
                    Response.AsRedirect("/");
                }

                return View["Home", new PostIndexViewModel {
                    DisqusShortName = config.DisqusShortName,
                    Page = page + 1,
                    TotalPageCount = Convert.ToInt32(Math.Ceiling((double)allPosts.Count() / (double)MAX_POSTS_PER_PAGE)),
                    Posts = posts
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
