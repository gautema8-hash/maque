using MDD.Models;

namespace MDD.DAL;

public class LoginDAL
{
	// 本地重建版：登录已本地授权，不再调用原版厂商接口
	public static string Login(LoginInfo info)
	{
		return "{\"status\":200,\"code\":200,\"msg\":\"local\"}";
	}
}
