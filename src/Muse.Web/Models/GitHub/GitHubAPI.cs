using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Muse.Web.Models.GitHub;
using Newtonsoft.Json;
using RestSharp;

namespace Muse.Web.Models.Github
{
	public interface IGitHubApi
	{
		Task<ContentResult[]> GetContent(string path);
		Task<ContentResult[]> GetContent(string owner, string repo, string path);

		Task<TreeResult> GetTree(string sha);
		Task<TreeResult> GetTree(string owner, string repo, string sha);

		Task<TreeResult> GetTreeRecursively(string sha);
		Task<TreeResult> GetTreeRecursively(string owner, string repo, string sha);

		Task<Blob> GetBlob(string sha);
		Task<Blob> GetBlob(string owner, string repo, string sha);
	}

	public class GitHubAPI : IGitHubApi
	{
		readonly string owner;
		readonly string repo;
        readonly string branch;
		readonly RestClient gitHubClient;

		public GitHubAPI(string owner, string repo, string branch, string userAgent, string oAuthToken)
		{
			this.owner = owner;
			this.repo = repo;
            this.branch = branch;
			this.gitHubClient = new RestClient("https://api.github.com");
			this.gitHubClient.AddDefaultHeader("User-Agent", userAgent);

			if (!String.IsNullOrWhiteSpace(oAuthToken)) {
				this.gitHubClient.AddDefaultHeader("Authorization", "token " + oAuthToken);
			}
		}

		public Task<ContentResult[]> GetContent(string path)
		{
			return GetContent(this.owner, this.repo, path);
		}

		public Task<ContentResult[]> GetContent(string owner, string repo, string path)
		{
			var tcs = new TaskCompletionSource<ContentResult[]>();

			var request = new RestRequest("repos/{owner}/{repo}/contents/{path}", Method.GET)
				.AddUrlSegment("owner", owner)
				.AddUrlSegment("repo", repo)
				.AddUrlSegment("path", path);

            if (!String.IsNullOrWhiteSpace(branch) && branch != "master") {
                request.AddQueryParameter("ref", branch);
            }

			var handle = gitHubClient.ExecuteAsync(request, response => {
				var responseObj = JsonConvert.DeserializeObject<ContentResult[]>(response.Content);
				tcs.SetResult(responseObj);
			});

			return tcs.Task;
		}

		public Task<TreeResult> GetTree(string sha)
		{
			return GetTree(this.owner, this.repo, sha);
		}

		public Task<TreeResult> GetTree(string owner, string repo, string sha)
		{
			var tcs = new TaskCompletionSource<TreeResult>();

			var request = new RestRequest("repos/{owner}/{repo}/git/trees/{sha}", Method.GET)
				.AddUrlSegment("owner", owner)
				.AddUrlSegment("repo", repo)
				.AddUrlSegment("sha", sha);

			var handle = gitHubClient.ExecuteAsync(request, response => {
				var responseObj = JsonConvert.DeserializeObject<TreeResult>(response.Content);
				tcs.SetResult(responseObj);
			});

			return tcs.Task;
		}

		public Task<TreeResult> GetTreeRecursively(string sha)
		{
			return GetTreeRecursively(this.owner, this.repo, sha);
		}

		public Task<TreeResult> GetTreeRecursively(string owner, string repo, string sha)
		{
			var tcs = new TaskCompletionSource<TreeResult>();

			var request = new RestRequest("repos/{owner}/{repo}/git/trees/{sha}?recursive=1", Method.GET)
				.AddUrlSegment("owner", owner)
				.AddUrlSegment("repo", repo)
				.AddUrlSegment("sha", sha);

			var handle = gitHubClient.ExecuteAsync(request, response => {
				var responseObj = JsonConvert.DeserializeObject<TreeResult>(response.Content);
				tcs.SetResult(responseObj);
			});

			return tcs.Task;
		}

		public Task<Blob> GetBlob(string sha)
		{
			return GetBlob(this.owner, this.repo, sha);
		}

		public Task<Blob> GetBlob(string owner, string repo, string sha)
		{
			var tcs = new TaskCompletionSource<Blob>();

			var request = new RestRequest("repos/{owner}/{repo}/git/blobs/{sha}", Method.GET)
				.AddUrlSegment("owner", owner)
				.AddUrlSegment("repo", repo)
				.AddUrlSegment("sha", sha);

			var handle = gitHubClient.ExecuteAsync(request, response => {
				var responseObj = JsonConvert.DeserializeObject<Blob>(response.Content);
				tcs.SetResult(responseObj);
			});

			return tcs.Task;
		}
	}
}
