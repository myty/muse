using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Web.Hosting;
using Biggy;
using Biggy.JSON;
using MyTy.Blog.Web.Models;

namespace MyTy.Blog.Web.Services
{
	public class ReactiveDirectory : IDisposable
	{
		readonly FileSystemWatcher watcher = new FileSystemWatcher();
		readonly Subject<ContentFile> fileChangeStream = new Subject<ContentFile>();

		public ReactiveDirectory(string directoryPath)
		{
			if (String.IsNullOrWhiteSpace(directoryPath)) {
				throw new ArgumentNullException("directoryPath");
			}

			// Set the FileSystemWatcher properties
			watcher.Path = (directoryPath.Length > 1 && directoryPath.Substring(0, 1) == "~") ?
				HostingEnvironment.MapPath(directoryPath) :
				directoryPath;
			watcher.IncludeSubdirectories = true;
			watcher.NotifyFilter = NotifyFilters.Attributes |
				NotifyFilters.CreationTime |
				NotifyFilters.FileName |
				NotifyFilters.LastAccess |
				NotifyFilters.LastWrite |
				NotifyFilters.Size |
				NotifyFilters.Security;
			watcher.Filter = "*.md";

			var bufferedChangeStream = Observable.Merge(
				fileChangeStream
					.GroupBy(k => k.FilePath).Select(k => k
					.Throttle(TimeSpan.FromSeconds(1))));

			UpdatedFiles = bufferedChangeStream.Where(b => !b.DeleteThis).Select(b => b.FilePath);
			DeletedFiles = bufferedChangeStream.Where(b => b.DeleteThis).Select(b => b.FilePath);

			Observable.Merge(
				Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
					h => watcher.Deleted += h,
					h => watcher.Deleted -= h)
					.Select(f => new ContentFile {
						DeleteThis = true,
						FilePath = f.EventArgs.FullPath
					}),
				Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
					h => watcher.Renamed += h,
					h => watcher.Renamed -= h)
					.SelectMany(f => new ContentFile[] {
						new ContentFile {
							DeleteThis = true,
							FilePath = f.EventArgs.OldFullPath
						},
						new ContentFile {
							FilePath = f.EventArgs.FullPath
						}
					})
					.Where(f => f.FilePath.EndsWith(".md")),
				Observable.Merge(
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Changed += h,
						h => watcher.Changed -= h),
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Created += h,
						h => watcher.Created -= h))
					.Select(f => new ContentFile {
						FilePath = f.EventArgs.FullPath
					})
				)
				.Subscribe(c => fileChangeStream.OnNext(c));
		}

		public IObservable<string> UpdatedFiles { get; private set; }
		public IObservable<string> DeletedFiles { get; private set; }

		public class ContentFile
		{
			public string FilePath { get; set; }
			public bool DeleteThis { get; set; }
		}

		public void CheckFiles(IEnumerable<string> filePaths)
		{
			var baseDir = HostingEnvironment.MapPath("~/");
			foreach (var file in filePaths.Select(f => Path.Combine(baseDir, f))) {
				if (!File.Exists(file)) {
					fileChangeStream.OnNext(new ContentFile { 
						DeleteThis = true,
						FilePath = file
					});
				}
			}
		}

		public void Start()
		{
			watcher.EnableRaisingEvents = true;
			var contentFiles = Directory.EnumerateFiles(watcher.Path, watcher.Filter, SearchOption.AllDirectories)
				.Select(e => new ContentFile {
					FilePath = e
				});

			foreach (var c in contentFiles) {
				fileChangeStream.OnNext(c);
			}
		}

		public void Stop()
		{
			watcher.EnableRaisingEvents = false;
		}

		public bool IsScanning { get { return watcher.EnableRaisingEvents; } }

		public void Dispose()
		{
			if (watcher.EnableRaisingEvents) {
				Stop();
			}

			watcher.Dispose();
		}
	}
}