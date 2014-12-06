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

			Get["/{slug}"] = (parameters) => {
				if (parameters.slug == "sitemap") {
					return Sitemap();
				}

				var fileLocation = String.Format("App_Data\\Content\\Pages\\{0}.md", parameters.slug);

				var page = db.Pages.FirstOrDefault(p => p.FileLocation == fileLocation);

				if (page == null) {
					return Response.AsError(HttpStatusCode.NotFound);
				}

				return View[page.Layout, new PageDetailViewModel {
					Page = page
				}];
			};

			Post["/{slug}", true] = async (parameters, ct) => {
				if (parameters.slug == "sitemap") {
					if (config.CanRefresh(Request)) {
						await GetLatestContent();
					}

					return Sitemap();
				}

				return Response.AsError(HttpStatusCode.ServiceUnavailable);
			};
		}

		private async Task GetLatestContent()
		{
			var postsPath = HostingEnvironment.MapPath("~/App_Data/Content/Posts");
			var pagesPath = HostingEnvironment.MapPath("~/App_Data/Content/Pages");
			var imagesPath = HostingEnvironment.MapPath("~/App_Data/Content/Images");

			var token = config.GitHubToken;
			if (!String.IsNullOrWhiteSpace(token)) {
				var postsMirror = new GitHubMirror("myty", "slick-blog-content", "_posts", postsPath, token);
				var pagesMirror = new GitHubMirror("myty", "slick-blog-content", "_pages", pagesPath, token);
				var imagesMirror = new GitHubMirror("myty", "slick-blog-content", "_imgs", imagesPath, token);

				await Task.WhenAll(
					postsMirror.SynchronizeAsync(),
					pagesMirror.SynchronizeAsync(),
					imagesMirror.SynchronizeAsync(false)
				);
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

			foreach (var file in Directory.EnumerateFiles(postsPath, "*", SearchOption.AllDirectories)) {
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

			foreach (var file in Directory.EnumerateFiles(pagesPath, "*", SearchOption.AllDirectories)) {
				pagesUpdater.FileUpdated(file);
			}
		}

		private dynamic Sitemap()
		{
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