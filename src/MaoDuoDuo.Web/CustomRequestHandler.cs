using CefSharp;
using CefSharp.Handler;

namespace MaoDuoDuo.Web;

public class CustomRequestHandler : RequestHandler
{
	public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
	{
		return false;
	}

	protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
	{
		return new CustomResourceRequestHandler();
	}
}
