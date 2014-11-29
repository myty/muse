using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MyTy.Blog.Web.Models.GitHub;
using Refit;

namespace MyTy.Blog.Web.Models.Github
{
	[Headers("User-Agent: shiny-myty-website")]
	public interface IGitHubApi
	{
		[Get("/repos/{owner}/{repo}/contents/{path}")]
		Task<ContentResult[]> GetContent(string owner, string repo, string path, [Header("Authorization")] string token);

		[Get("/repos/{owner}/{repo}/git/trees/{sha}")]
		Task<TreeResult> GetTree(string owner, string repo, string sha, [Header("Authorization")] string token);

		[Get("/repos/{owner}/{repo}/git/trees/{sha}?recursive=1")]
		Task<TreeResult> GetTreeRecursively(string owner, string repo, string sha, [Header("Authorization")] string token);

		[Get("/repos/{owner}/{repo}/git/blobs/{sha}")]
		Task<Blob> GetBlob(string owner, string repo, string sha, [Header("Authorization")] string token);
	}
}
