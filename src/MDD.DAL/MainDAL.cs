using System.Data;
using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class MainDAL
{
	// 本地重建版：公告/校验/申诉理由全部本地化，不再调用原版厂商接口
	public static string GetAllNotice()
	{
		return "{\"status\":200,\"code\":200,\"msg\":\"local\"}";
	}

	public static string GetVerify(int point)
	{
		return "1";
	}

	public static string GetAllReason()
	{
		return "{\"status\":200,\"code\":200,\"msg\":\"local\"}";
	}

	public static DataTable Ceshi()
	{
		return SqlHelper.Query("select * from ceshi");
	}

	public static void SqlCommon()
	{
		new SqlHelper();
	}
}
