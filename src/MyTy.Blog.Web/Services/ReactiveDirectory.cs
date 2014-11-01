using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace MyTy.Blog.Web.Services
{
	public class ReactiveDirectory : IDisposable
	{
		readonly FileSystemWatcher watcher = new FileSystemWatcher();
		readonly Subject<ContentFile> fileChanges = new Subject<ContentFile>();

		public ReactiveDirectory(string directoryPath, string fileType)
		{
			var fileExtension = "." + fileType;

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
			watcher.Filter = "*" + fileExtension;

			var bufferedChanges = Observable.Merge(
				fileChanges,
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
					.Where(f => f.FilePath.EndsWith(fileExtension)),
				Observable.Merge(
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Changed += h,
						h => watcher.Changed -= h),
					Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
						h => watcher.Created += h,
						h => watcher.Created -= h))
					.Select(f => new ContentFile {
						FilePath = f.EventArgs.FullPath
					}))
				.GroupBy(k => k.FilePath)
				.SelectMany(k => k
					.Throttle(TimeSpan.FromSeconds(1)));

			UpdatedFiles = bufferedChanges.Where(b => !b.DeleteThis).Select(b => b.FilePath);
			DeletedFiles = bufferedChanges.Where(b => b.DeleteThis).Select(b => b.FilePath);
		}

		public IObservable<string> UpdatedFiles { get; private set; }
		public IObservable<string> DeletedFiles { get; private set; }

		public class ContentFile
		{
			public string FilePath { get; set; }
			public bool DeleteThis { get; set; }
		}

		public async Task Start()
		{
			watcher.EnableRaisingEvents = true;

			await Task.Factory.StartNew(() => { 
				var contentFiles = Directory.EnumerateFiles(watcher.Path, watcher.Filter, SearchOption.AllDirectories)
					.Select(e => new ContentFile {
						FilePath = e
					});

				foreach (var c in contentFiles) {
					fileChanges.OnNext(c);
				}
			});
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