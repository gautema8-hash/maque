using System.Collections.Generic;
using System.Data;
using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class MainDAL
{
	public static string GetAllNotice()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("token", SysCommon.UserInfo.Token);
		dictionary.Add("ss", "2");
		return WebHelper.PostAsync("member/login/allNotice", dictionary, SysCommon.UserInfo.Token);
	}

	public static string GetVerify(int point)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("username", SysCommon.Username);
		dictionary.Add("password", SysCommon.Password);
		dictionary.Add("ss", "2");
		dictionary.Add("source", SysCommon.SysUrl.source);
		if (point > 0)
		{
			dictionary.Add("point", "1");
		}
		return WebHelper.PostAsync("member/login/apiLoginVerify", dictionary, SysCommon.UserInfo.Token, isUrl1: true);
	}

	public static string GetAllReason()
	{
		Dictionary<string, string> pData = new Dictionary<string, string>();
		return WebHelper.PostAsync("member/login/getAppealreasonList", pData, SysCommon.UserInfo.Token);
	}

	public static DataTable Ceshi()
	{
		return SqlHelper.Query("select * from ceshi");
	}

	public static void SqlCommon()
	{
		new SqlHelper();
	}
}
