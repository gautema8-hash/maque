using System;
using System.Collections.Generic;
using MDD.Common;
using MDD.DAL;
using MDD.Models;
using Newtonsoft.Json.Linq;

namespace MDD.BLL;

public class HomeBLL
{
	public static ResultInfo GetHomeInfo()
	{
		// 本地重建版：首页数据本地生成（直连入口/热搜/行情为空数据）
		ResultInfo resultInfo = new ResultInfo
		{
			Status = 200,
			Code = 200,
			Msg = "ok"
		};
		try
		{
			SysCommon.ConfigInfo = new ConfigInfo
			{
				DirectEntry = new List<DirectEntryInfo>(),
				HotSearch = new string[0],
				marketData = new JObject()
			};
		}
		catch (Exception e)
		{
			resultInfo.Code = -1;
			resultInfo.Msg = e.Message;
			LogHelper.Debug("GetHomeInfo", e);
		}
		return resultInfo;
	}
}
