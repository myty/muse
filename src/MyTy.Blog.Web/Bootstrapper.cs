using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				StaticContentConventionBuilder.AddDirectory("/img", "/Images"));
		}

		protected override void ConfigureApplicationContainer(IKernel existingContainer)
		{
			existingContainer
				.Bind<BlogDB>().ToSelf()
				.InSingletonScope();

			existingContainer
				.Bind<PostScanner>().ToSelf()
				.InSingletonScope();

			var scanner = existingContainer.TryGet<PostScanner>();
			if (scanner != null) {
				scanner.Start().ContinueWith(t => {
					Debug.WriteLine("Posts scanner started.");
				});
			}

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

			base.ApplicationStartup(container, pipelines);
		}
	}
}