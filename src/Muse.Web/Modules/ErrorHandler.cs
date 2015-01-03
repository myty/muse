using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Muse.Web.ViewModels;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;

namespace Muse.Web.Modules
{
	public class ErrorHandler : IStatusCodeHandler
	{
		private IViewRenderer viewRenderer;
		public ErrorHandler(IViewRenderer viewRenderer)
		{
			this.viewRenderer = viewRenderer;
		}

		public void Handle(Nancy.HttpStatusCode statusCode, Nancy.NancyContext context)
		{
			var response = viewRenderer.RenderView(context, "Error", new ErrorViewModel {
				Code = (int)statusCode
			});
			response.StatusCode = statusCode;
			context.Response = response;
		}

		public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, Nancy.NancyContext context)
		{
			return (int)statusCode >= 400;
		}
	}

	public static class ErrorExtensions
	{
		public static Response AsError(this IResponseFormatter formatter, HttpStatusCode statusCode)
		{
			return new Response {
				StatusCode = statusCode,
				ContentType = "text/plain",
				Contents = stream => (new StreamWriter(stream) { AutoFlush = true }).Write(String.Empty)
			};
		}
	}
}
