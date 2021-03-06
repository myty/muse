﻿using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Nancy;
using Owin;
using Nancy.Owin;

[assembly: OwinStartup(typeof(Muse.Web.BlogStartup))]

namespace Muse.Web
{
	public class BlogStartup
	{
		public void Configuration(IAppBuilder app)
		{
            app.UseNancy();
			app.UseStageMarker(PipelineStage.MapHandler);
		}
	}
}
