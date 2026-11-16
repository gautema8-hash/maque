using CefSharp;

namespace MaoDuoDuo;

public class MyJsDialogHandler : IJsDialogHandler
{
	public bool OnBeforeUnloadDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, string messageText, bool isReload, IJsDialogCallback callback)
	{
		callback.Continue(success: true);
		return true;
	}

	public void OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser)
	{
	}

	public bool OnJSDialog(IWebBrowser browserControl, IBrowser browser, string originUrl, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
	{
		suppressMessage = true;
		return true;
	}

	public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
	{
	}
}
