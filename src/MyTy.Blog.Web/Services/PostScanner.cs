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
		readonly FileSystemWatcher watcher = new FileSystemWatcher();
		readonly string contactsSearchPath = @"~/Posts";
		private IDisposable fileSysSubscription;
		readonly IObservable<PostFile> fileSysObservable;
		readonly BlogDB db;

		public PostScanner(BlogDB db)
		{
			this.db = db;

			// Set the FileSystemWatcher properties.
			watcher.Path = HostingEnvironment.MapPath(contactsSearchPath);
			watcher.IncludeSubdirectories = true;
			watcher.NotifyFilter = NotifyFilters.Attributes |
				NotifyFilters.CreationTime |
				NotifyFilters.FileName |
				NotifyFilters.LastAccess |
				NotifyFilters.LastWrite |
				NotifyFilters.Size |
				NotifyFilters.Security;
			watcher.Filter = "*.md";

			var postsToRemove = Enumerable.Empty<PostFile>();

			var fileList = Directory
				.EnumerateFiles(watcher.Path, watcher.Filter, SearchOption.AllDirectories)
				.Select(f => f.Replace(watcher.Path, ""))
				.Distinct()
				.ToArray();

			postsToRemove = db.Posts
				.Where(doc => !fileList.Any(f => f == doc.FileLocation))
				.Select(doc => new PostFile {
					ChangeType = WatcherChangeTypes.Deleted,
					FullPath = new string[] { Path.Combine(watcher.Path, doc.FileLocation) }
				});

			fileSysObservable =
				Observable.Merge(
					Directory.EnumerateFiles(watcher.Path, watcher.Filter, SearchOption.AllDirectories)
						.Select(e => new PostFile {
							ChangeType = WatcherChangeTypes.Changed,
							FullPath = new string[] { e }
						}).ToObservable(),
					postsToRemove.ToObservable(),
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Deleted += h,
						h => watcher.Deleted -= h)
						.Select(e => new PostFile { ChangeType = e.EventArgs.ChangeType, FullPath = new string[] { e.EventArgs.FullPath } }),
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Changed += h,
						h => watcher.Changed -= h)
						.Select(e => new PostFile { ChangeType = e.EventArgs.ChangeType, FullPath = new string[] { e.EventArgs.FullPath } }),
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Created += h,
						h => watcher.Created -= h)
						.Select(e => new PostFile { ChangeType = e.EventArgs.ChangeType, FullPath = new string[] { e.EventArgs.FullPath } }),
					Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
						h => watcher.Renamed += h,
						h => watcher.Renamed -= h)
						.Select(e => new PostFile {
							ChangeType = e.EventArgs.ChangeType,
							FullPath = new string[] { e.EventArgs.FullPath, e.EventArgs.OldFullPath }
						}))
				.GroupBy(m => m.FullPath.First())
				.SelectMany(g => g.Throttle(TimeSpan.FromSeconds(1)));
		}

		class PostFile
		{
			public WatcherChangeTypes ChangeType { get; set; }
			public string[] FullPath { get; set; }
		}

		void ProcessServiceFile(PostFile x)
		{
			switch (x.ChangeType) {
				case WatcherChangeTypes.Deleted:
					var deletePost = db.Posts.FirstOrDefault(p => p.FileLocation ==
						x.FullPath.First().Replace(watcher.Path, ""));

					if (deletePost != null) {
						db.Posts.Remove(deletePost);
					}

					break;
				case WatcherChangeTypes.Renamed:
					var renamePost = db.Posts.FirstOrDefault(p => p.FileLocation ==
						x.FullPath.Last().Replace(watcher.Path, ""));

					if (renamePost != null) {
						renamePost.FileLocation = x.FullPath.First().Replace(watcher.Path, "");

						db.Posts.Update(renamePost);
					}

					break;
				default:
					var postFilePath = x.FullPath.First();
					var fileText = File.ReadAllText(postFilePath);
					var metadata = fileText.YamlHeader();

					//validate that there is a title
					if (metadata.ContainsKey("title") && (metadata["title"] == null || (string)metadata["title"] == "nil")) return;

					var fileLocation = postFilePath.Replace(watcher.Path, "");
					var post = db.Posts.FirstOrDefault(p => p.FileLocation == fileLocation);

					var subTitle = (metadata.ContainsKey("subTitle") && metadata["subTitle"] != null && (string)metadata["subTitle"] != "nil") ?
						metadata["subTitle"] as string :
						String.Empty;

					var headerBg = (metadata.ContainsKey("headerBg") && metadata["headerBg"] != null && (string)metadata["headerBg"] != "nil") ?
						metadata["headerBg"] as string :
						String.Empty;

					var allowComments = (metadata.ContainsKey("comments") && metadata["comments"] != null && (string)metadata["comments"] != "nil") ?
						Boolean.Parse((string)metadata["comments"]) :
						false;

					var postDate = (metadata.ContainsKey("date") && metadata["date"] != null && (string)metadata["date"] != "nil") ?
						DateTime.Parse((string)metadata["date"]) :
						DateTime.MaxValue;

					var title = metadata["title"] as string;

					var content = CommonMark.CommonMarkConverter.Convert(fileText.ExcludeHeader());

					if (x.ChangeType == WatcherChangeTypes.Created || post == null) {
						var filename = Path.GetFileName(postFilePath);
						db.Posts.Add(new Post {
							Slug = filename.Substring(11, filename.Length - 14),
							Title = title,
							Content = content,
							SubTitle = subTitle,
							HeaderBackgroundImage = headerBg,
							Comments = allowComments,
							FileLocation = fileLocation,
							PostDate = postDate
						});
					} else {
						if (post.PostDate != postDate) {
							post.PostDate = postDate;
						}

						if (post.FileLocation != fileLocation) {
							post.FileLocation = fileLocation;
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

						db.Posts.Update(post);
					}

					break;
			}

		}


		public void Start()
		{
			watcher.EnableRaisingEvents = true;
			fileSysSubscription = fileSysObservable.Subscribe(ProcessServiceFile);
		}

		public void Stop()
		{
			watcher.EnableRaisingEvents = false;
			fileSysSubscription.Dispose();
			fileSysSubscription = null;
		}

		public void Dispose()
		{
			if (watcher.EnableRaisingEvents || fileSysSubscription != null) {
				Stop();
			}

			watcher.Dispose();
		}
	}
}