using System;
using CefSharp;
using CefSharp.Handler;
using MDD.Models;

namespace MaoDuoDuo.Web;

public class CustomResourceRequestHandler : ResourceRequestHandler
{
	private MemoryStreamResponseFilter responseFilter;

	protected override void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
	{
	}

	protected override bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
	{
		string text = response.Headers["Set-Cookie"];
		string text2 = response.Headers["Date"];
		string text3 = request.Headers["User-Agent"];
		if (request.Url.Contains("www.kuaishou.com/search/video?searchKey"))
		{
			if (text.Contains("did"))
			{
				string[] array = text.Split(new string[1] { "did" }, StringSplitOptions.None);
				if (array.Length < 1)
				{
					return false;
				}
				string text4 = array[1].Split(';')[0];
				SysCommon.KsCookieInfo.did = text4.Replace("=", "");
			}
		}
		else if (request.Url.Contains("www.douyin.com/aweme/v1/web/search/item/?device_platform=webapp"))
		{
			string text5 = "";
		}
		if (!request.Url.Contains("rule_suggest_price") || request.ReferrerUrl.Contains("activityId=20981"))
		{
		}
		return false;
	}

	protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
	{
		if (request.Url.Contains("rule_suggest_price") && request.ReferrerUrl.Contains("activityId"))
		{
			responseFilter = new MemoryStreamResponseFilter();
			return responseFilter;
		}
		return null;
	}
}
