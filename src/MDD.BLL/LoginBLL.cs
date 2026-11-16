using System;
using MDD.Common;
using MDD.Models;

namespace MDD.BLL;

public class LoginBLL
{
	public static (ResultInfo, UserInfo) Login(LoginInfo info)
	{
		// 本地重建版：不再依赖远程会员接口，本地直接授权
		ResultInfo resultInfo = new ResultInfo
		{
			Status = 200,
			Code = 200,
			Msg = "登录成功"
		};
		UserInfo userInfo = new UserInfo
		{
			Username = info.Username,
			Vip = true,
			IsPay = true,
			Token = "local-" + DateTime.Now.Ticks,
			ExpirationDate = "2099-12-31",
			LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
			MemberDescription = "麻雀一键铺货 本地授权版"
		};
		SysCommon.UserInfo = userInfo;
		SysCommon.Username = info.Username;
		SysCommon.Password = info.Password;
		return (resultInfo, userInfo);
	}
}
