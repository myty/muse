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

		public BlogDB()
		{
			Posts = new BiggyList<Post>(new JsonStore<Post>(dbPath:
				HostingEnvironment.MapPath("~/App_Data/"))); 
		}  

	}
}