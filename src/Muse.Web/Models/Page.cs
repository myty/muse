using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Muse.Web.Models
{
	public class Page
	{
		public string FileLocation { get; set; }
		public string Href { get; set; }
		public string Title { get; set; }
		public string SubTitle { get; set; }
		public string HeaderBackgroundImage { get; set; }
		public DateTime Date { get; set; }
		public string Content { get; set; }
        public string Layout { get; set; }
        public string SiteMenu { get; set; }
        public int? SiteMenuOrder { get; set; }

        public string AuthorName { get; set; }
        public string AuthorUrl { get; set; }
	}
}