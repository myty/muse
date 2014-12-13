using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyTy.Blog.Web.Models;

namespace MyTy.Blog.Web.ViewModels
{
	public class PostIndexViewModel
	{
        public string DisqusShortName { get; set; }
		public int Page { get; set; }
		public IEnumerable<Post> Posts { get; set; }
	}
}
