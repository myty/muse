using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Muse.Web.Models;
using Muse.Web.Services;
using Muse.Web.ViewModels;
using Nancy;
using Nancy.Responses;

namespace Muse.Web.Modules
{
    public class PostModule : BaseModule
    {
        const int MAX_POSTS_PER_PAGE = 10;

        public PostModule(BlogDB db, IApplicationConfiguration config)
        {
            Get["/"] = parameters => {
                var page = Int32.Parse((string)Request.Query["page"] ?? "1");
                page = (page <= 1) ? 0 : (page - 1);

                var allPosts = db.Posts
                    .OrderByDescending(p => p.Date);

                if (!allPosts.Any()) {
                    return Response.AsError(HttpStatusCode.InternalServerError);
                }

                var posts = allPosts
                    .Skip(MAX_POSTS_PER_PAGE * page)
                    .Take(MAX_POSTS_PER_PAGE)
                    .ToArray();

                if (!posts.Any()) {
                    return Response.AsRedirect("~/");
                } else {
                    return View["Home", new PostIndexViewModel {
                        DisqusShortName = config.DisqusShortName,
                        Page = page + 1,
                        TotalPageCount = Convert.ToInt32(Math.Ceiling((double)allPosts.Count() / (double)MAX_POSTS_PER_PAGE)),
                        Posts = posts
                    }];
                }
            };

            Get["/{year}/{month}/{day}/{slug}"] = parameters => {
                var fileLocation = String.Format("App_Data\\Content\\Posts\\{0}\\{0}-{1}-{2}-{3}.md",
                    parameters.year, parameters.month, parameters.day, parameters.slug);

                var post = db.Posts.FirstOrDefault(p => p.FileLocation == fileLocation);

                if (post == null) {
                    return Response.AsError(HttpStatusCode.NotFound);
                }

                ViewBag.PageTitle = " - " + post.Title;

                return View[post.Layout, new PostDetailViewModel {
                    DisqusShortName = config.DisqusShortName,
                    EditLink = GetEditLink(config.PostsSync, String.Format(
                        "{0}/{0}-{1}-{2}-{3}.md",
                        parameters.year, parameters.month, parameters.day, parameters.slug)),
                    Post = post
                }];
            };
        }
    }
}
