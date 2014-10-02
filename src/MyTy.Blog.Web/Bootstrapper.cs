using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;

namespace MyTy.Blog.Web
{
	public class CustomBoostrapper : DefaultNancyBootstrapper
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
				StaticContentConventionBuilder.AddDirectory("/img", "/Images"));
		}

		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			base.ConfigureApplicationContainer(container);
		}

		protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
		{
			base.ConfigureRequestContainer(container, context);
		}

		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
		{
			//base.ApplicationStartup(container, pipelines);

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
		}
	}
}