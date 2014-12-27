using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.Modules;
using MyTy.Blog.Web.Services;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Ninject;
using Nancy.Conventions;
using Nancy.TinyIoc;
using Ninject;

namespace MyTy.Blog.Web
{
	public class CustomBoostrapper : NinjectNancyBootstrapper
	{
		protected override void ConfigureConventions(NancyConventions conventions)
		{
			base.ConfigureConventions(conventions);

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/js-base"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/css-base"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/fonts"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/scripts"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/img", "/App_Data/Content/Images"));
		}

		protected override void ConfigureApplicationContainer(IKernel existingContainer)
		{
			existingContainer
				.Bind<BlogDB>().ToSelf()
				.InSingletonScope();

			existingContainer
				.Bind<IApplicationConfiguration>()
				.To<ApplicationConfiguration>()
				.InSingletonScope();

			base.ConfigureApplicationContainer(existingContainer);
		}

		protected override void ConfigureRequestContainer(IKernel container, NancyContext context)
		{
			base.ConfigureRequestContainer(container, context);
		}

		protected override void ApplicationStartup(IKernel container, IPipelines pipelines)
		{
			var scriptBundle = new ScriptBundle("~/js-base")
				.Include("~/Scripts/jquery-{version}.js")
				.Include("~/Scripts/bootstrap.js")
				.Include("~/Scripts/clean-blog.js");

			var styleBundle = new StyleBundle("~/css-base")
				.Include("~/Content/bootstrap.css")
				.Include("~/Content/clean-blog.css");

			BundleTable.Bundles.Add(scriptBundle);
			BundleTable.Bundles.Add(styleBundle);

			BundleTable.EnableOptimizations = false;

            var db = container.Get<BlogDB>();
            var appConfig = container.Get<IApplicationConfiguration>();

            if (!db.Pages.Any()) {
                var pagesUpdater = new PageUpdater(db);
                foreach (var file in Directory.EnumerateFiles(appConfig.PagesSync.locaPath, "*", SearchOption.AllDirectories)) {
                    pagesUpdater.FileUpdated(file);
                }
            }

            if (!db.Posts.Any()) {
                var postsUpdater = new PostUpdater(db);
                foreach (var file in Directory.EnumerateFiles(appConfig.PostsSync.locaPath, "*", SearchOption.AllDirectories)) {
                    postsUpdater.FileUpdated(file);
                }
            }

			base.ApplicationStartup(container, pipelines);
		}
	}
}
