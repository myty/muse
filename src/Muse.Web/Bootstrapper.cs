using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Optimization;
using Muse.Web.Models;
using Muse.Web.Modules;
using Muse.Web.Services;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Ninject;
using Nancy.Conventions;
using Nancy.TinyIoc;
using Ninject;

namespace Muse.Web
{
	public class CustomBoostrapper : NinjectNancyBootstrapper
	{
        readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
        private BlogDB db;
        private IApplicationConfiguration appConfig;

		protected override void ConfigureConventions(NancyConventions conventions)
		{
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddFile("/atom.xml", Path.Combine(siteBasePath, "App_Data/Content/atom.xml")));

            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddFile("/sitemap.xml", Path.Combine(siteBasePath, "App_Data/Content/sitemap.xml")));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/js-base"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/css-base"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/fonts"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/scripts"));

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/img", "/App_Data/Content/_imgs"));
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

            existingContainer
                .Bind<IContentService>()
                .To<ContentService>();

            db = existingContainer.Get<BlogDB>();
            appConfig = existingContainer.Get<IApplicationConfiguration>();

			base.ConfigureApplicationContainer(existingContainer);
		}

		protected override void ConfigureRequestContainer(IKernel container, NancyContext context)
		{
            base.ConfigureRequestContainer(container, context);

            context.ViewBag.Title = appConfig.SiteTitle;
            context.ViewBag.SubTitle = appConfig.SiteSubTitle;
            context.ViewBag.DefaultHeaderImage = "/img/" + appConfig.DefaultHeaderImage;
            context.ViewBag.SocialLinks = appConfig.SocialLinks;

            var siteMenu = new Dictionary<string, string>();
            siteMenu.Add("Home", "/");
            foreach (var menuItem in db.Pages
                .Where(p => !String.IsNullOrWhiteSpace(p.SiteMenu))
                .OrderBy(p => p.SiteMenuOrder)) {
                siteMenu.Add(menuItem.SiteMenu, menuItem.Href);
            }

            context.ViewBag.SiteMenu = siteMenu;
		}

		protected override void ApplicationStartup(IKernel container, IPipelines pipelines)
		{
			var scriptBundle = new ScriptBundle("~/js-base")
				.Include("~/Scripts/jquery-{version}.js")
				.Include("~/Scripts/bootstrap.js")
				.Include("~/Scripts/clean-blog.js");

			var styleBundle = new StyleBundle("~/css-base")
                .Include("~/Content/bootstrap.css")
                .Include("~/Content/clean-blog.css")
                .Include("~/Content/site.css");

			BundleTable.Bundles.Add(scriptBundle);
			BundleTable.Bundles.Add(styleBundle);

			BundleTable.EnableOptimizations = false;

			base.ApplicationStartup(container, pipelines);
		}
	}
}
