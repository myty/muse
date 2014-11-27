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
		readonly IFileUpdater pageUpdater;
		
		public PageScanner(BlogDB db)
		{
			this.db = db;
			this.pageUpdater = new PageUpdater(db);
			this.rxDirectory = new ReactiveDirectory(@"~/Pages", "md");
		}

		public async Task Start()
		{
			subscriptions[0] = rxDirectory.UpdatedFiles.Subscribe(this.pageUpdater.FileUpdated);
			subscriptions[1] = rxDirectory.DeletedFiles.Subscribe(this.pageUpdater.FileDeleted);

			var rxDirStartup = rxDirectory.Start();
			
			//remove any files that may have been deleted while server was not running
			var findDeletedFilesTask = Task.Factory.StartNew(() => { 
				var deleteFiles = db.Pages
					.Select(p => p.FileLocation)
					.Select(f => Path.Combine(siteBasePath, f))
					.Where(f => !File.Exists(f));

				foreach (var file in deleteFiles) {
					this.pageUpdater.FileDeleted(file);
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
	}
}