using System.Collections.Generic;
using MDD.DAL;
using MDD.Models.Good;

namespace MDD.BLL;

public class GoodMonitorBLL
{
	public static List<GoodMonitor> GetAllGoodMonitor(string state)
	{
		return GoodMonitorDAL.GetAllGoodMonitor(state);
	}

	public static GoodMonitor GetGoodMonitorInfo(string goodId)
	{
		return GoodMonitorDAL.GetGoodMonitorInfo(goodId);
	}

	public static string UpdateGoodMonitorState(GoodMonitor good)
	{
		return GoodMonitorDAL.UpdateGoodMonitorState(good);
	}

	public static string AddGoodMonitor(GoodMonitor good)
	{
		return GoodMonitorDAL.AddGoodMonitor(good);
	}

	public static GoodMonitorInfo GetGoodMonitorInfo(string goodId, string createTime)
	{
		return GoodMonitorDAL.GetGoodMonitorInfo(goodId, createTime);
	}

	public static List<GoodMonitorInfo> GetAllGoodMonitorInfo(string goodId)
	{
		return GoodMonitorDAL.GetAllGoodMonitorInfo(goodId);
	}

	public static string UpdateGoodMonitorInfo(GoodMonitorInfo info)
	{
		return GoodMonitorDAL.UpdateGoodMonitorInfo(info);
	}

	public static string AddGoodMonitorInfo(GoodMonitorInfo info)
	{
		return GoodMonitorDAL.AddGoodMonitorInfo(info);
	}
}
