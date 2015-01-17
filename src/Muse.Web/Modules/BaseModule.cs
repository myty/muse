using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Muse.Web.Models;
using Nancy;

namespace Muse.Web.Modules
{
    public class BaseModule : NancyModule
    {
        public string GetEditLink(GitHubDirectorySync dirSync, string remoteFolderPath, string filePath)
        {
            return String.Format(
                "https://github.com/{0}/{1}/edit/{2}/{3}/{4}",
                dirSync.owner,
                dirSync.repo,
                dirSync.branch,
                remoteFolderPath,
                filePath);
        }
    }
}