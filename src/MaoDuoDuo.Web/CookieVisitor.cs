using System;
using CefSharp;

namespace MaoDuoDuo.Web;

public class CookieVisitor : ICookieVisitor, IDisposable
{
	public event Action<Cookie> SendCookie;

	public void Dispose()
	{
	}

	public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
	{
		deleteCookie = false;
		this.SendCookie?.Invoke(cookie);
		return true;
	}
}
