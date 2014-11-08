using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using Biggy;
using Biggy.JSON;
using MyTy.Blog.Web.Models;

namespace MyTy.Blog.Web.Services
{
	public class PageScanner
	{
		readonly BlogDB db;
		readonly ReactiveDirectory rxDirectory;
		readonly IDisposable[] subscriptions = new IDisposable[2];
		readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");
		
		public PageScanner(BlogDB db)
		{
			this.db = db;
			this.rxDirectory = new ReactiveDirectory(@"~/Pages", "md");
		}

		public async Task Start()
		{
			subscriptions[0] = rxDirectory.UpdatedFiles.Subscribe(FileUpdated);
			subscriptions[1] = rxDirectory.DeletedFiles.Subscribe(FileDeleted);

			var rxDirStartup = rxDirectory.Start();
			
			//remove any files that may have been deleted while server was not running
			var findDeletedFilesTask = Task.Factory.StartNew(() => { 
				var deleteFiles = db.Pages
					.Select(p => p.FileLocation)
					.Select(f => Path.Combine(siteBasePath, f))
					.Where(f => !File.Exists(f));

				foreach (var file in deleteFiles) {
					FileDeleted(file);
				}
			});

			await Task.WhenAll(rxDirStartup, findDeletedFilesTask);
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
			var pageFilePath = file;
			var fileText = File.ReadAllText(pageFilePath);
			var metadata = fileText.YamlHeader();

			//validate that there is a title
			if (metadata.ContainsKey("title") && (metadata["title"] == null || (string)metadata["title"] == "nil")) return;

			var fileLocation = pageFilePath.Replace(this.siteBasePath, "");

			var subTitle = (metadata.ContainsKey("subTitle") && metadata["subTitle"] != null && (string)metadata["subTitle"] != "nil") ?
				metadata["subTitle"] as string :
				String.Empty;

			var headerBg = (metadata.ContainsKey("headerBg") && metadata["headerBg"] != null && (string)metadata["headerBg"] != "nil") ?
				metadata["headerBg"] as string :
				String.Empty;

			var layout = (metadata.ContainsKey("layout") && metadata["layout"] != null && (string)metadata["layout"] != "nil") ?
				metadata["layout"] as string :
				"page";

			var pageDate = (metadata.ContainsKey("date") && metadata["date"] != null && (string)metadata["date"] != "nil") ?
				DateTime.Parse((string)metadata["date"]) :
				DateTime.MaxValue;

			var title = metadata["title"] as string;

			var content = CommonMark.CommonMarkConverter.Convert(fileText.ExcludeHeader());
			var filename = Path.GetFileName(pageFilePath);
			var href = GetHref(filename);

			var page = db.Pages.FirstOrDefault(p => p.FileLocation == fileLocation);
			if (page == null) {
				db.Pages.Add(new Page {
					Href = href,
					Title = title,
					Content = content,
					SubTitle = subTitle,
					HeaderBackgroundImage = headerBg,
					FileLocation = fileLocation,
					Date = pageDate,
					Layout = layout

				});
			} else {
				if (page.Date != pageDate) {
					page.Date = pageDate;
				}

				if (page.FileLocation != fileLocation) {
					page.FileLocation = fileLocation;
				}

				if (page.Href != href) {
					page.Href = href;
				}

				if (page.Title != title) {
					page.Title = title;
				}

				if (page.HeaderBackgroundImage != headerBg) {
					page.HeaderBackgroundImage = headerBg;
				}

				if (page.Content != content) {
					page.Content = content;
				}

				if (page.SubTitle != subTitle) {
					page.SubTitle = subTitle;
				}

				if (page.Layout != layout) {
					page.Layout = layout;
				}

				db.Pages.Update(page);
			}
		}

		private void FileDeleted(string file)
		{
			var deletePage = db.Pages.FirstOrDefault(p =>
					p.FileLocation == file.Replace(this.siteBasePath, ""));

			if (deletePage != null) {
				db.Pages.Remove(deletePage);
			}
		}

		private string GetHref(string filePath)
		{
			var filename = Path.GetFileName(filePath);
			var slug = filename.Substring(0, filename.Length - 3);

			return String.Format("/{0}", slug);
		}
	}
}