using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class HomeDAL
{
	public static string GetHomeInfo()
	{
		return WebHelper.PostAsync("api/index/config", null, SysCommon.UserInfo.Token);
	}
}
