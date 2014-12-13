using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyTy.Blog.Web.Models;

namespace MyTy.Blog.Web.ViewModels
{
	public class PostDetailViewModel
	{
        public Post Post { get; set; }
        public string DisqusShortName { get; set; }
	}
}
