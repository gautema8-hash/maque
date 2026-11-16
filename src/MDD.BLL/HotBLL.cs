using System;
using System.Collections.Generic;
using MDD.Common;
using MDD.DAL;
using MDD.Models;
using Newtonsoft.Json;

namespace MDD.BLL;

public class HotBLL
{
	public static (ResultInfo, List<HotShopInfo>) GetHotInfo(HotSearch hot)
	{
		ResultInfo item = new ResultInfo();
		List<HotShopInfo> item2 = new List<HotShopInfo>();
		string hotInfo = HotDAL.GetHotInfo(hot);
		try
		{
			item = JsonConvert.DeserializeObject<ResultInfo>(hotInfo);
		}
		catch (Exception e)
		{
			LogHelper.Debug("GetHotInfo", e);
		}
		return (item, item2);
	}
}
