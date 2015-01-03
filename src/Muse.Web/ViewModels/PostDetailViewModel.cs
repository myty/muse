using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Muse.Web.Models;

namespace Muse.Web.ViewModels
{
	public class PostDetailViewModel
	{
        public Post Post { get; set; }
        public string EditLink { get; set; }
        public string DisqusShortName { get; set; }
	}
}
