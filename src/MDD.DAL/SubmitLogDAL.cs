using System.Data;
using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class SubmitLogDAL
{
	public static string SubmitStep(StepErrorInfo info)
	{
		string data = $"ss={info.SS.ToString()}&appname={info.appname}&type={info.type.ToString()}&msg={info.msg}&step={info.step.ToString()}&username={info.username}&goodid={info.goodid}";
		return WebHelper.Get("member/client/log", data).ToString();
	}

	public static DataTable GetGoodId(string goodId)
	{
		return SqlHelper.Query("select * from submit_log where good_id = '" + goodId + "'");
	}

	public static string AddShop(SubmitLogInfo info)
	{
		return SqlHelper.Update($"insert into submit_log(good_id,num,create_time) \r\n                    values('{info.goodId}','{info.num}','{info.createTime}')");
	}
}
