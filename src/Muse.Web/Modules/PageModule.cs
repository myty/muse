using System;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using Muse.Web.Models;
using Muse.Web.Models.Github;
using Muse.Web.Services;
using Muse.Web.ViewModels;
using Nancy;

namespace Muse.Web.Modules
{
    public class PageModule : BaseModule
    {
        public PageModule(BlogDB db, IApplicationConfiguration config, IContentService contentService)
        {
            Get["/{slug}"] = (parameters) => {
                if (!db.Pages.Any()) {
                    return Response.AsError(HttpStatusCode.InternalServerError);
                }

                var fileLocation = String.Format("App_Data\\Content\\_pages\\{0}.md", parameters.slug);

                var page = db.Pages.FirstOrDefault(p => p.FileLocation == fileLocation);

                if (page == null) {
                    return Response.AsError(HttpStatusCode.NotFound);
                }

                ViewBag.PageTitle = " - " + page.Title;

                return View[page.Layout, new PageDetailViewModel {
                    Page = page,
                    EditLink = GetEditLink(config.Sync, "_pages", parameters.slug + ".md")
                }];
            };

            Post["/update", true] = async (parameters, ct) => {
                if (config.CanRefresh(Request)) {
                    await contentService.GetLatestContent(config);
                }

                return Response.AsText("Success");
            };
        }
    }
}
