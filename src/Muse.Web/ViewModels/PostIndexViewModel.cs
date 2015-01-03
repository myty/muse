using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Muse.Web.Models;

namespace Muse.Web.ViewModels
{
	public class PostIndexViewModel
	{
        public string DisqusShortName { get; set; }
		public int Page { get; set; }
        public int TotalPageCount { get; set; }
		public IEnumerable<Post> Posts { get; set; }
	}
}
