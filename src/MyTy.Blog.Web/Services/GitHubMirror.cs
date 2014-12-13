using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MyTy.Blog.Web.Models;
using MyTy.Blog.Web.Models.Github;
using RestSharp;

namespace MyTy.Blog.Web.Services
{
	public class GitHubMirror
	{
		readonly string gitHubBaseDir;
		readonly string localDir;
		readonly GitHubAPI gitHubAPI;

		public GitHubMirror(string gitHubOwner, string gitHubRepo, string gitHubBaseDir, string localDir, string oAuthToken)
		{
			this.gitHubBaseDir = gitHubBaseDir;
			this.localDir = localDir;
			this.gitHubAPI = new GitHubAPI(gitHubOwner, gitHubRepo, "myty-blog-engine", oAuthToken);
		}

		public async Task<GitHubMirrorSynchronizeResult> SynchronizeAsync(bool okToDeleteFiles = true)
		{
			var result = new GitHubMirrorSynchronizeResult();

			try {

				var pagesContentResult = await gitHubAPI.GetContent(gitHubBaseDir);

				var treeContents = await Task.WhenAll(pagesContentResult.Where(p => p.type == "dir").Select(async p => {
					var treeResults = await gitHubAPI.GetTreeRecursively(p.sha);
					return new {
						p.path,
						treeResults
					};
				}));

				var filesPathSha = pagesContentResult.Where(p => p.type == "file").Select(f => new {
					path = f.path.Replace(gitHubBaseDir + "/", ""),
					sha = f.sha
				}).Concat(treeContents.SelectMany(t => t.treeResults.tree.Where(p => p.type == "blob").Select(p => new {
					path = t.path.Replace(gitHubBaseDir + "/", "") + "/" + p.path,
					sha = p.sha
				})));

				var remoteFilesSynced = (await Task.WhenAll(filesPathSha.Select(async f => {
					var getBlobTask = gitHubAPI.GetBlob(f.sha);

					string status = null;
					var localFileInfo = new System.IO.FileInfo(localDir + "\\" + f.path.Replace("/", "\\"));

					var blob = await getBlobTask;

					byte[] fileContents = Convert.FromBase64String(blob.content);
					FileStream fileStream = null;

					if (!localFileInfo.Exists) {
						status = "added";
						localFileInfo.Directory.Create();

						fileStream = new System.IO.FileStream(
							localFileInfo.FullName,
							System.IO.FileMode.Create,
							System.IO.FileAccess.Write);

					} else {
						using (var md5 = MD5.Create()) {
							fileStream = new System.IO.FileStream(
								localFileInfo.FullName,
								System.IO.FileMode.Open,
								System.IO.FileAccess.Read);

							if (!SameContents(fileStream, fileContents)) {
                                fileStream.Close();
                                fileStream = new System.IO.FileStream(
                                    localFileInfo.FullName,
                                    System.IO.FileMode.Truncate,
                                    System.IO.FileAccess.Write);
								status = "update";
							}
						}
					}

					if (status != null && fileStream != null) {
						fileStream.Write(fileContents, 0, fileContents.Length);
						fileStream.Close();
					}

					return new {
						localPath = localFileInfo.FullName,
						status
					};
				}))).ToArray();

				result.FilesUpdated = remoteFilesSynced
					.Where(f => f.status == "updated")
					.Select(f => f.localPath)
					.ToArray();

				result.FilesAdded = remoteFilesSynced
					.Where(f => f.status == "added")
					.Select(f => f.localPath)
					.ToArray();

				result.FilesDeleted = Directory
					.EnumerateFiles(localDir, "*", SearchOption.AllDirectories)
					.Where(f => !remoteFilesSynced.Any(s => s.localPath == f))
					.Select(f => {
						if (okToDeleteFiles) { File.Delete(f); }
						return f;
					})
					.ToArray();

			} catch (Exception ex) {
				throw ex;
			}

			return result;
		}

		public static bool SameContents(Stream oldFileContents, byte[] newFileContents)
		{
			var result = true;
            var bufferSize = 16 * 1024;
            var oldFileBuffer = new byte[bufferSize];

			int oldBufferReadSize;
            int repeatCount = 0;
			while ((oldBufferReadSize = oldFileContents.Read(oldFileBuffer, 0, oldFileBuffer.Length)) > 0) {
                var newFileBuffer = newFileContents
                    .Skip(bufferSize * repeatCount++)
                    .Take(oldBufferReadSize)
                    .ToArray();

                var sameContents = Enumerable.SequenceEqual(
                    oldFileBuffer.Take(oldBufferReadSize),
                    newFileBuffer);

                if (!sameContents) {
                    result = false;
                    break;
                }
			}

            return result;
		}
	}

	public class GitHubMirrorSynchronizeResult
	{
		public GitHubMirrorSynchronizeResult()
		{
			FilesUpdated = Enumerable.Empty<string>();
			FilesAdded = Enumerable.Empty<string>();
			FilesDeleted = Enumerable.Empty<string>();
		}

		public IEnumerable<string> FilesAdded { get; set; }
		public IEnumerable<string> FilesUpdated { get; set; }
		public IEnumerable<string> FilesDeleted { get; set; }
	}
}
