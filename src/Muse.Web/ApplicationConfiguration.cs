using System.Collections.Generic;
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
        Task ScanEnvironmentConfigFile();

        string BaseUrl { get; }
        string GitHubToken { get; }
        string DisqusShortName { get; }
        string GoogleAnalyticsTrackingCode { get; }
        GitHubDirectorySync Sync { get; }

        IDictionary<string, string> SocialLinks { get; }

        string SiteID { get; }
        string SiteTitle { get; }
        string SiteSubTitle { get; }
        string DefaultHeaderImage { get; }
    }

    public class ApplicationConfiguration : IApplicationConfiguration
    {
        private string refreshToken;
        readonly string environmentConfigFilePath = HostingEnvironment.MapPath("~/env.config.json");

        readonly FileSystemWatcher watcher = new FileSystemWatcher();
        readonly BlogDB db;
        readonly IContentService contentService;

        public ApplicationConfiguration(BlogDB db, IContentService contentService)
        {
            this.db = db;
            this.contentService = contentService;

            Task.WaitAll(ScanEnvironmentConfigFile());

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

        public async Task ScanEnvironmentConfigFile()
        {
            if (File.Exists(environmentConfigFilePath)) {
                await ScanEnvironmentConfigFile(environmentConfigFilePath);
            }
        }

        private async Task ScanEnvironmentConfigFile(string environmentConfigFilePath)
        {
            var environmentConfig = JsonConvert.DeserializeObject<EnvironmentConfig>(
                File.ReadAllText(environmentConfigFilePath));

            refreshToken = environmentConfig.refreshToken;

            Sync = ResolvePaths(environmentConfig.sync);
            BaseUrl = environmentConfig.baseUrl;
            GitHubToken = environmentConfig.gitHubToken;
            DisqusShortName = environmentConfig.disqus_shortname;
            GoogleAnalyticsTrackingCode = environmentConfig.ga_tracking_code;

            var shouldSync = Sync.remoteFolders
                .Select(f => Path.Combine(Sync.locaStoragePath, f))
                .Any(p => !Directory.Exists(p))
                || !db.Pages.Any()
                || !db.Posts.Any();

            if (shouldSync) {
                await contentService.GetLatestContent(this);
            }

            var siteConfigFilePath = Path.Combine(Sync.locaStoragePath, "_site/config.json");
            var siteConfig = JsonConvert.DeserializeObject<SiteConfig>(
                File.ReadAllText(siteConfigFilePath));

            SiteID = siteConfig.id;
            SiteTitle = siteConfig.siteTitle;
            SiteSubTitle = siteConfig.siteSubTitle;
            DefaultHeaderImage = siteConfig.defaultImg;
            SocialLinks = siteConfig.socialLinks;
        }

        public string BaseUrl { get; private set; }
        public string GoogleAnalyticsTrackingCode { get; private set; }
        public string GitHubToken { get; private set; }
        public string DisqusShortName { get; private set; }
        public string SiteID { get; private set; }
        public string SiteTitle { get; private set; }
        public string SiteSubTitle { get; private set; }
        public string DefaultHeaderImage { get; private set; }
        public IDictionary<string, string> SocialLinks { get; private set; }
        public GitHubDirectorySync Sync { get; private set; }

        public bool CanRefresh(Request request)
        {
            string apiKey = request.Query["key"];

            return apiKey != null && apiKey.Equals(refreshToken);
        }

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
        public string disqus_shortname { get; set; }
        public string ga_tracking_code { get; set; }
        public string refreshToken { get; set; }
        public string gitHubToken { get; set; }
        public GitHubDirectorySync sync { get; set; }
    }

    public class SiteConfig
    {
        public string id { get; set; }
        public string siteTitle { get; set; }
        public string siteSubTitle { get; set; }
        public string defaultImg { get; set; }
        public Dictionary<string, string> socialLinks { get; set; }
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
