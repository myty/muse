using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyTy.Blog.Web.Models
{
	public class Post
	{
		public string FileLocation { get; set; }
		public string Href { get; set; }
		public string Title { get; set; }
		public string SubTitle { get; set; }
		public string HeaderBackgroundImage { get; set; }
		public DateTime PostDate { get; set; }
		public bool Comments { get; set; }
		public string Content { get; set; }
		public string Layout { get; set; }
	}
}