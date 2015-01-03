using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Muse.Web.Models.GitHub
{
	public class Blob
	{
		public string content { get; set; }
		public string encoding { get; set; }
		public string url { get; set; }
		public string sha { get; set; }
		public int size { get; set; }
	}
}