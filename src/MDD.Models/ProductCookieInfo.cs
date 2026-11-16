using System.Collections.Generic;
using System.Net;

namespace MDD.Models;

public class ProductCookieInfo
{
	public string accessToken { get; set; }

	public string verifyAuthToken { get; set; }

	public string cookie { get; set; }

	public List<Cookie> webCookies { get; set; }
}
