using System;
using System.Data;
using MDD.Common;
using MDD.DAL;
using MDD.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDD.BLL;

public class MainBLL
{
	public static ResultInfo GetAllNotice()
	{
		// 本地重建版：公告与会员信息本地生成
		ResultInfo resultInfo = new ResultInfo
		{
			Status = 200,
			Code = 200,
			Msg = "ok"
		};
		try
		{
			SysCommon.NoticeContent = "";
			PopNotice popNotice = new PopNotice();
			popNotice.Width = 420.0;
			popNotice.Height = 260.0;
			if (SysCommon.UserInfo == null)
			{
				SysCommon.UserInfo = new UserInfo
				{
					Username = "local",
					Vip = true,
					ExpirationDate = "2099-12-31",
					LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
					Token = "local"
				};
			}
			if (SysCommon.UserInfo.LocalTime == null)
			{
				SysCommon.UserInfo.LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			}
			SysCommon.PopNotice = popNotice;
		}
		catch (Exception e)
		{
			resultInfo.Code = -1;
			LogHelper.Debug("GetAllNotice", e);
		}
		return resultInfo;
	}

	public static string GetVerify(int point = 0)
	{
		// 本地重建版：无点数校验，始终通过
		return "1";
	}

	public static ResultInfo GetAllReason()
	{
		ResultInfo resultInfo = new ResultInfo
		{
			Status = 200,
			Code = 200,
			Msg = "ok"
		};
		resultInfo.Data = new JArray();
		return resultInfo;
	}

	public static DataTable Ceshi()
	{
		return MainDAL.Ceshi();
	}

	public static void SqlCommon()
	{
		MainDAL.SqlCommon();
	}
}
