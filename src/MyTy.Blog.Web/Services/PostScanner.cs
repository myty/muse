using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Web.Hosting;
using Biggy;
using Biggy.JSON;
using MyTy.Blog.Web.Models;

namespace MyTy.Blog.Web.Services
{
	public class PostScanner
	{
		readonly BlogDB db;
		readonly ReactiveDirectory rxDirectory;
		readonly IDisposable[] subscriptions = new IDisposable[2];
		readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
		
		public PostScanner(BlogDB db)
		{
			this.db = db;
			this.rxDirectory = new ReactiveDirectory(@"~/Posts", "md");
		}

		public void Start()
		{
			subscriptions[0] = rxDirectory.UpdatedFiles.Subscribe(FileUpdated);
			subscriptions[1] = rxDirectory.DeletedFiles.Subscribe(FileDeleted);

			rxDirectory.Start();
			
			//remove any files that may have been deleted while server was not running
			var deleteFiles = db.Posts
				.Select(p => p.FileLocation)
				.Select(f => Path.Combine(siteBasePath, f))
				.Where(f => !File.Exists(f));

			foreach (var file in deleteFiles) {
				FileDeleted(file);
			}
		}

		public void Stop()
		{
			if (rxDirectory.IsScanning) {
				rxDirectory.Stop();
			}
		}

		public void Dispose()
		{
			if (rxDirectory.IsScanning || subscriptions.Any(s => s != null)) {
				this.Stop();
			}

			foreach (var s in subscriptions.Where(s => s != null)) {
				s.Dispose();
			}

			rxDirectory.Dispose();
		}

		private void FileUpdated(string file)
		{
			var postFilePath = file;
			var fileText = File.ReadAllText(postFilePath);
			var metadata = fileText.YamlHeader();

			//validate that there is a title
			if (metadata.ContainsKey("title") && (metadata["title"] == null || (string)metadata["title"] == "nil")) return;

			var fileLocation = postFilePath.Replace(this.siteBasePath, "");

			var subTitle = (metadata.ContainsKey("subTitle") && metadata["subTitle"] != null && (string)metadata["subTitle"] != "nil") ?
				metadata["subTitle"] as string :
				String.Empty;

			var headerBg = (metadata.ContainsKey("headerBg") && metadata["headerBg"] != null && (string)metadata["headerBg"] != "nil") ?
				metadata["headerBg"] as string :
				String.Empty;

			var layout = (metadata.ContainsKey("layout") && metadata["layout"] != null && (string)metadata["layout"] != "nil") ?
				metadata["layout"] as string :
				"page";

			var allowComments = (metadata.ContainsKey("comments") && metadata["comments"] != null && (string)metadata["comments"] != "nil") ?
				Boolean.Parse((string)metadata["comments"]) :
				false;

			var postDate = (metadata.ContainsKey("date") && metadata["date"] != null && (string)metadata["date"] != "nil") ?
				DateTime.Parse((string)metadata["date"]) :
				DateTime.MaxValue;

			var title = metadata["title"] as string;

			var content = CommonMark.CommonMarkConverter.Convert(fileText.ExcludeHeader());
			var filename = Path.GetFileName(postFilePath);
			var href = GetHref(filename);

			var post = db.Posts.FirstOrDefault(p => p.FileLocation == fileLocation);
			if (post == null) {
				db.Posts.Add(new Post {
					Href = href,
					Title = title,
					Content = content,
					SubTitle = subTitle,
					HeaderBackgroundImage = headerBg,
					Comments = allowComments,
					FileLocation = fileLocation,
					PostDate = postDate,
					Layout = layout

				});
			} else {
				if (post.PostDate != postDate) {
					post.PostDate = postDate;
				}

				if (post.FileLocation != fileLocation) {
					post.FileLocation = fileLocation;
				}

				if (post.Href != href) {
					post.Href = href;
				}

				if (post.Title != title) {
					post.Title = title;
				}

				if (post.HeaderBackgroundImage != headerBg) {
					post.HeaderBackgroundImage = headerBg;
				}

				if (post.Content != content) {
					post.Content = content;
				}

				if (post.SubTitle != subTitle) {
					post.SubTitle = subTitle;
				}

				if (post.Comments != allowComments) {
					post.Comments = allowComments;
				}

				if (post.Layout != layout) {
					post.Layout = layout;
				}

				db.Posts.Update(post);
			}
		}

		private void FileDeleted(string file)
		{
			var deletePost = db.Posts.FirstOrDefault(p =>
					p.FileLocation == file.Replace(this.siteBasePath, ""));

			if (deletePost != null) {
				db.Posts.Remove(deletePost);
			}
		}

		private string GetHref(string filePath)
		{
			var filename = Path.GetFileName(filePath);
			var slug = filename.Substring(11, filename.Length - 14);
			var year = filename.Substring(0, 4);
			var month = filename.Substring(5, 2);
			var day = filename.Substring(8, 2);

			return String.Format("/{0}/{1}/{2}/{3}", year, month, day, slug);
		}
	}
}