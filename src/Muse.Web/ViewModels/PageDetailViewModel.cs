using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Muse.Web.Models;

namespace Muse.Web.ViewModels
{
	public class PageDetailViewModel
	{
        public Page Page { get; set; }
        public string EditLink { get; set; }
	}
}