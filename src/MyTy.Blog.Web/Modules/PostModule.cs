using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;

namespace MyTy.Blog.Web.Modules
{
	public class PostModule : NancyModule
	{
		public PostModule()
		{
			Get["/"] = parameters => {
				ViewBag.Title = "Test Blog Post";
				return View["Home"];
			};

			Get["/{slug}"] = parameters => {
				return parameters.slug;
			};
		}
	}
}