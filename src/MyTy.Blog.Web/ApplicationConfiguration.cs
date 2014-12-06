using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
		string GitHubToken { get; }
	}

	public class ApplicationConfiguration : IApplicationConfiguration
	{
		readonly string refreshToken;
		readonly string baseUrl;
		readonly string gitHubToken;

		public ApplicationConfiguration()
		{
			var environmentConfigFilePath = HostingEnvironment.MapPath("~/env.config.json");
			if (File.Exists(environmentConfigFilePath)) {
				var environmentConfig = JsonConvert.DeserializeObject<EnvironmentConfig>(
					File.ReadAllText(environmentConfigFilePath));

				refreshToken = environmentConfig.refreshToken;
				baseUrl = environmentConfig.baseUrl;
				gitHubToken = environmentConfig.gitHubToken;
			}
		}

		public bool CanRefresh(Request request)
		{
			string apiKey = request.Query["key"];

			return apiKey != null && apiKey.Equals(refreshToken); 
		}

		public string BaseUrl
		{
			get { return baseUrl; }
		}

		public string GitHubToken
		{
			get { return gitHubToken; }
		}
	}

	public class EnvironmentConfig
	{
		public string refreshToken { get; set; }
		public string baseUrl { get; set; }
		public string gitHubToken { get; set; }
	}

}