using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MDD.Common;
using MDD.Models.Video;
using MDD.Utility;

namespace MDD.DAL;

public class CaptureVideoDAL
{
	public static string GetKSVideo(KSVideoSearch ks, Dictionary<string, string> cookies = null)
	{
		return WebHelper.GetKSVideo(ks, cookies);
	}

	public static async Task<string> GetDYVideo(string url, string cookieString)
	{
		return await WebHelper.GetDYVideo(url, cookieString);
	}

	public static string DownloadFile(string url, string path, DownloadClass downClass)
	{
		return WebHelper.DownloadFile(url, path, downClass);
	}

	public static DataTable GetDyCookie()
	{
		return SqlHelper.Query("select * from dy_login where id = 1");
	}

	public static string UpdateDyCookie(string name, string value)
	{
		return SqlHelper.Update($"update dy_login set name = '{name}',value = '{value}'" + AuditHelper.UpdateSet + " where id = 1");
	}
}
