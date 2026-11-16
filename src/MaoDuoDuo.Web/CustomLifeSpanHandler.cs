using System;
using CefSharp;

namespace MaoDuoDuo.Web;

public class CustomLifeSpanHandler : ILifeSpanHandler
{
	public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
	{
		throw new NotImplementedException();
	}

	public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
	{
	}

	public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
	{
		throw new NotImplementedException();
	}

	public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
	{
		chromiumWebBrowser.Load(targetUrl);
		newBrowser = null;
		return true;
	}
}
