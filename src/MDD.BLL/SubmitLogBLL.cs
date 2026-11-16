using System.Data;
using MDD.DAL;
using MDD.Models;

namespace MDD.BLL;

public class SubmitLogBLL
{
	public static string SubmitStep(StepErrorInfo info)
	{
		return SubmitLogDAL.SubmitStep(info);
	}

	public static DataTable GetGoodId(string goodId)
	{
		return SubmitLogDAL.GetGoodId(goodId);
	}

	public static string AddShop(SubmitLogInfo info)
	{
		return SubmitLogDAL.AddShop(info);
	}
}
