using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyTy.Blog.Web
{
	public interface IApplicationConfiguration
	{
		bool CanRefresh(string passcode);
		string BaseUrl { get; }
	}

	public class ApplicationConfiguration : IApplicationConfiguration
	{
		public ApplicationConfiguration()
		{

		}

		public bool CanRefresh(string passcode)
		{
			return true;
		}

		public string BaseUrl
		{
			get { return "http://mytyblog.tysonofyork.com"; }
		}
	}
}