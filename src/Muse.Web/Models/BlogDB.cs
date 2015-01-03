using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Biggy;
using Biggy.Core;
using Biggy.Data.Json;

namespace Muse.Web.Models
{
	public class BlogDB
	{
		public BiggyList<Post> Posts;
		public BiggyList<Page> Pages;

		public BlogDB()
		{
			var app_data = HostingEnvironment.MapPath("~/App_Data/DB");

            var dbCore = new JsonDbCore(app_data, "v2");

            var pageStore = new JsonStore<Page>(dbCore);
            var postStore = new JsonStore<Post>(dbCore);

            Posts = new BiggyList<Post>(postStore);
            Pages = new BiggyList<Page>(pageStore);
		}

	}
}
