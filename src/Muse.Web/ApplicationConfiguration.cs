using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using Muse.Web.Models;
using Muse.Web.Services;
using Nancy;
using Newtonsoft.Json;

namespace Muse.Web
{
    public interface IApplicationConfiguration
    {
        bool CanRefresh(Request request);
        string BaseUrl { get; }
        string GitHubToken { get; }
        string DisqusShortName { get; }
        GitHubDirectorySync Sync { get; }

        string SiteID { get; }
        string SiteTitle { get; }
        string SiteSubTitle { get; }
        string DefaultHeaderImage { get; }
    }

    public class ApplicationConfiguration : IApplicationConfiguration
    {
        private string refreshToken;
        private string baseUrl;
        private string gitHubToken;
        private string disqusShortName;
        private GitHubDirectorySync sync;

        readonly FileSystemWatcher watcher = new FileSystemWatcher();

        private string siteID;
        private string siteTitle;
        private string siteSubTitle;
        private string defaultHeaderImage;

        readonly BlogDB db;
        readonly IContentService contentService;

        public ApplicationConfiguration(BlogDB db, IContentService contentService)
        {
            this.db = db;
            this.contentService = contentService;

            var environmentConfigFilePath = HostingEnvironment.MapPath("~/env.config.json");
            if (File.Exists(environmentConfigFilePath)) {
                Task.WaitAll(ScanEnvironmentConfigFile(environmentConfigFilePath));
            }

            watcher.Path = Path.GetDirectoryName(environmentConfigFilePath);
            watcher.IncludeSubdirectories = false;
            watcher.NotifyFilter = NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Security;
            watcher.Filter = Path.GetFileName(environmentConfigFilePath);

            watcher.Changed += async (sender, e) => {
                await ScanEnvironmentConfigFile(e.FullPath);
            };

            watcher.Created += async (sender, e) => {
                await ScanEnvironmentConfigFile(e.FullPath);
            };

            watcher.EnableRaisingEvents = true;
        }

        private async Task ScanEnvironmentConfigFile(string environmentConfigFilePath)
        {
            var environmentConfig = JsonConvert.DeserializeObject<EnvironmentConfig>(
                File.ReadAllText(environmentConfigFilePath));

            sync = ResolvePaths(environmentConfig.sync);
            refreshToken = environmentConfig.refreshToken;
            baseUrl = environmentConfig.baseUrl;
            gitHubToken = environmentConfig.gitHubToken;

            var shouldSync = sync.remoteFolders
                .Select(f => Path.Combine(sync.locaStoragePath, f))
                .Any(p => !Directory.Exists(p))
                || !db.Pages.Any()
                || !db.Posts.Any();

            if (shouldSync) {
                await contentService.GetLatestContent(this);
            }

            var siteConfigFilePath = Path.Combine(sync.locaStoragePath, "_site/config.json");
            var siteConfig = JsonConvert.DeserializeObject<SiteConfig>(
                File.ReadAllText(siteConfigFilePath));

            disqusShortName = siteConfig.disqus_shortname;
            siteID = siteConfig.id;
            siteTitle = siteConfig.siteTitle;
            siteSubTitle = siteConfig.siteSubTitle;
            defaultHeaderImage = siteConfig.defaultImg;
        }

        public bool CanRefresh(Request request)
        {
            string apiKey = request.Query["key"];

            return apiKey != null && apiKey.Equals(refreshToken);
        }

        public string BaseUrl { get { return baseUrl; } }
        public string GitHubToken { get { return gitHubToken; } }
        public string DisqusShortName { get { return disqusShortName; } }

        public string SiteID { get { return siteID; } }
        public string SiteTitle { get { return siteTitle; } }
        public string SiteSubTitle { get { return siteSubTitle; } }
        public string DefaultHeaderImage { get { return defaultHeaderImage; } }

        public GitHubDirectorySync Sync { get { return sync; } }

        private GitHubDirectorySync ResolvePaths(GitHubDirectorySync dirSync)
        {
            if (dirSync.locaStoragePath.StartsWith("~/")) {
                dirSync.locaStoragePath = HostingEnvironment.MapPath(dirSync.locaStoragePath);
            }
            return dirSync;
        }
    }

    public class EnvironmentConfig
    {
        public string baseUrl { get; set; }
        public string refreshToken { get; set; }
        public string gitHubToken { get; set; }
        public GitHubDirectorySync sync { get; set; }
    }

    public class SiteConfig
    {
        public string id { get; set; }
        public string siteTitle { get; set; }
        public string siteSubTitle { get; set; }
        public string disqus_shortname { get; set; }
        public string defaultImg { get; set; }
    }

    public class GitHubDirectorySync
    {
        public string owner { get; set; }
        public string repo { get; set; }
        public string branch { get; set; }
        public string[] remoteFolders { get; set; }
        public string locaStoragePath { get; set; }
    }
}
