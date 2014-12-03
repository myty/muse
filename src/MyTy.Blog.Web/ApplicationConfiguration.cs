using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Nancy;
using Newtonsoft.Json;

namespace MyTy.Blog.Web
{
	public interface IApplicationConfiguration
	{
		bool CanRefresh(Request request);
		string BaseUrl { get; }
	}

	public class ApplicationConfiguration : IApplicationConfiguration
	{
		readonly string[] acceptRefreshIPAddresses;
		readonly string baseUrl;

		public ApplicationConfiguration()
		{
			var environmentConfigFilePath = HostingEnvironment.MapPath("~/env.config.json");
			if (File.Exists(environmentConfigFilePath)) {
				var environmentConfig = JsonConvert.DeserializeObject<EnvironmentConfig>(
					File.ReadAllText(environmentConfigFilePath));

				acceptRefreshIPAddresses = environmentConfig.refreshIPs;
				baseUrl = environmentConfig.baseUrl;
			}
		}

		public bool CanRefresh(Request request)
		{
			return acceptRefreshIPAddresses.Contains(request.UserHostAddress);
		}

		public string BaseUrl
		{
			get { return baseUrl; }
		}
	}


	public class EnvironmentConfig
	{
		public string[] refreshIPs { get; set; }
		public string baseUrl { get; set; }
	}

}