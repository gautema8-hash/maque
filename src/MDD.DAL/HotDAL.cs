using System.Collections.Generic;
using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class HotDAL
{
	public static string GetHotInfo(HotSearch hot)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("openTime", hot.OpenTime);
		dictionary.Add("endTime", hot.EndTime);
		dictionary.Add("px", hot.Px.ToString());
		dictionary.Add("pz", hot.Pz.ToString());
		dictionary.Add("templateGoodsName", hot.TemplateGoodsName);
		dictionary.Add("sort", hot.Sort.ToString());
		return WebHelper.PostAsync("api/word/getExpendituresData", dictionary, SysCommon.UserInfo.Token);
	}
}
