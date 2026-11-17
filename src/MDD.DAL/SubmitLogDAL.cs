using System.Data;
using MDD.Models;
using MDD.Common;
using MDD.Utility;

namespace MDD.DAL;

public class SubmitLogDAL
{
	// 本地重建版：铺货异常遥测不再上报厂商服务器
	public static string SubmitStep(StepErrorInfo info)
	{
		LogHelper.Debug("SubmitStep(本地化): step=" + info.step + " goodid=" + info.goodid + " msg=" + info.msg, null);
		return "local";
	}

	public static DataTable GetGoodId(string goodId)
	{
		return SqlHelper.Query("select * from submit_log where good_id = '" + goodId + "'");
	}

	public static string AddShop(SubmitLogInfo info)
	{
		return SqlHelper.Update($"insert into submit_log(good_id,num,create_time,{AuditHelper.InsertColumns}) values('{info.goodId}','{info.num}','{info.createTime}',{AuditHelper.InsertValues})");
	}
}
