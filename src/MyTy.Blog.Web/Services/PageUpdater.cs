using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using MyTy.Blog.Web.Models;

namespace MyTy.Blog.Web.Services
{
	public class PageUpdater : IFileUpdater
	{
		readonly BlogDB db;
		readonly string siteBasePath = HostingEnvironment.MapPath(@"~/");

		public PageUpdater(BlogDB db)
		{
			this.db = db;
		}

		public void FileUpdated(string file)
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

		public void FileDeleted(string file)
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