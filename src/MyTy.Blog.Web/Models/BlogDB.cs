using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Biggy;
using Biggy.JSON;

namespace MyTy.Blog.Web.Models
{
	public class BlogDB
	{
		public BiggyList<Post> Posts;
		public BiggyList<Page> Pages;

		public BlogDB()
		{
			var app_data = HostingEnvironment.MapPath("~/App_Data/");

			Posts = new BiggyList<Post>(new JsonStore<Post>(dbPath: app_data));

			Pages = new BiggyList<Page>(new JsonStore<Page>(dbPath: app_data)); 
		}  

	}
}