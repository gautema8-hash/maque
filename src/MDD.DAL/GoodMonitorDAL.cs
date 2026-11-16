using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MDD.Common;
using MDD.Models.Good;
using MDD.Utility;

namespace MDD.DAL;

public class GoodMonitorDAL
{
	public static List<GoodMonitor> GetAllGoodMonitor(string state)
	{
		List<GoodMonitor> result = new List<GoodMonitor>();
		try
		{
			string text = "select * from dd_good_monitor where 1=1 ";
			if (!string.IsNullOrEmpty(state) && state != "all")
			{
				text = text + " and good_state = " + state;
			}
			result = ChangeInfo(SqlHelper.Query(text));
		}
		catch (Exception e)
		{
			LogHelper.Debug("GoodMonitorDAL-GetAllGoodMonitor", e);
		}
		return result;
	}

	public static GoodMonitor GetGoodMonitorInfo(string goodId)
	{
		GoodMonitor result = new GoodMonitor();
		try
		{
			result = ChangeInfo(SqlHelper.Query($"select * from dd_good_monitor where good_id = '{goodId}'")).FirstOrDefault();
		}
		catch (Exception e)
		{
			LogHelper.Debug("GoodMonitorDAL-GetGoodMonitorInfo", e);
		}
		return result;
	}

	public static string UpdateGoodMonitorState(GoodMonitor good)
	{
		return SqlHelper.Update(string.Format("update dd_good_monitor set good_state = {1},update_time='{2}' where good_id = '{0}'", good.GoodId, good.GoodState, good.UpdateTime));
	}

	public static string AddGoodMonitor(GoodMonitor good)
	{
		return SqlHelper.Update($"insert into dd_good_monitor(good_id,good_name,good_num,good_state,update_time,create_time) \r\n                    values('{good.GoodId}','{good.GoodName}',{good.GoodNum},{good.GoodState},'{good.UpdateTime}','{good.CreateTime}')");
	}

	public static GoodMonitorInfo GetGoodMonitorInfo(string goodId, string createTime)
	{
		GoodMonitorInfo result = new GoodMonitorInfo();
		try
		{
			result = ChangeGoodInfo(SqlHelper.Query($"select * from dd_good_monitor_info where good_id = '{goodId}' and create_time='{createTime}'")).FirstOrDefault();
		}
		catch (Exception e)
		{
			LogHelper.Debug("GoodMonitorDAL-GetGoodMonitorInfo2", e);
		}
		return result;
	}

	public static List<GoodMonitorInfo> GetAllGoodMonitorInfo(string goodId)
	{
		List<GoodMonitorInfo> result = new List<GoodMonitorInfo>();
		try
		{
			result = ChangeGoodInfo(SqlHelper.Query($"select * from dd_good_monitor_info where good_id = '{goodId}'"));
		}
		catch (Exception e)
		{
			LogHelper.Debug("GoodMonitorDAL-GetAllGoodMonitorInfo", e);
		}
		return result;
	}

	public static string UpdateGoodMonitorInfo(GoodMonitorInfo info)
	{
		return SqlHelper.Update(string.Format("update dd_good_monitor_info set good_num = {1},update_time='{2}' where good_id = '{0}'", info.GoodId, info.GoodNum, info.UpdateTime));
	}

	public static string AddGoodMonitorInfo(GoodMonitorInfo info)
	{
		return SqlHelper.Update($"insert into dd_good_monitor_info(good_id,good_name,good_num,update_time,create_time) \r\n                    values('{info.GoodId}','{info.GoodName}',{info.GoodNum},'{info.UpdateTime}','{info.CreateTime}')");
	}

	private static List<GoodMonitor> ChangeInfo(DataTable dt)
	{
		return (from row in dt.AsEnumerable()
			select new GoodMonitor
			{
				Id = Convert.ToInt32(row["Id"]),
				GoodId = Convert.ToString(row["good_id"]),
				GoodName = Convert.ToString(row["good_name"]),
				GoodNum = Convert.ToInt32(row["good_num"]),
				GoodState = Convert.ToInt32(row["good_state"]),
				UpdateTime = Convert.ToString(row["update_time"]),
				CreateTime = Convert.ToString(row["create_time"])
			}).ToList();
	}

	private static List<GoodMonitorInfo> ChangeGoodInfo(DataTable dt)
	{
		return (from row in dt.AsEnumerable()
			select new GoodMonitorInfo
			{
				Id = Convert.ToInt32(row["Id"]),
				GoodId = Convert.ToString(row["good_id"]),
				GoodName = Convert.ToString(row["good_name"]),
				GoodNum = Convert.ToInt32(row["good_num"]),
				UpdateTime = Convert.ToString(row["update_time"]),
				CreateTime = Convert.ToString(row["create_time"])
			}).ToList();
	}
}
