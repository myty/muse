using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Muse.Web.Models.GitHub
{
	public class UserSearchResult
	{
		public int TotalCount { get; set; }
		public bool IncompleteResults { get; set; }
		public IList<User> Items { get; set; }
	}
}