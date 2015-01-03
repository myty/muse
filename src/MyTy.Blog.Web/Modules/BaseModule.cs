using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyTy.Blog.Web.Models;
using Nancy;

namespace MyTy.Blog.Web.Modules
{
    public class BaseModule : NancyModule
    {
        public string GetEditLink(GitHubDirectorySync dirSync, string filePath)
        {
            return String.Format(
                "https://github.com/{0}/{1}/edit/{2}/{3}/{4}",
                dirSync.owner,
                dirSync.repo,
                dirSync.branch,
                dirSync.remotePath,
                filePath);
        }
    }
}