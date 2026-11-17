using System;
using MDD.Common;
using MDD.Models;

namespace MDD.BLL;

public class LoginBLL
{
	public static (ResultInfo, UserInfo) Login(LoginInfo info)
	{
		// 本地 MySQL 账号验证（maque.users 表，SHA256+盐比对）
		ResultInfo resultInfo = new ResultInfo();
		(UserInfo userInfo, string err) = (new UserInfo(), "");
		try
		{
			(bool ok, string msg) = MDD.DAL.LoginDAL.VerifyUser(info.Username, info.Password);
			if (!ok)
			{
				resultInfo.Status = 401;
				resultInfo.Code = 401;
				resultInfo.Msg = msg;
				return (resultInfo, new UserInfo());
			}
			userInfo = new UserInfo
			{
				Username = info.Username,
				Vip = true,
				IsPay = true,
				Token = "local-" + DateTime.Now.Ticks,
				ExpirationDate = "2099-12-31",
				LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
				MemberDescription = "麻雀一键铺货 本地授权版"
			};
			resultInfo.Status = 200;
			resultInfo.Code = 200;
			resultInfo.Msg = "登录成功";
			SysCommon.UserInfo = userInfo;
			SysCommon.Username = info.Username;
			SysCommon.Password = info.Password;
			SysCommon.LoginMethod = "password";
			SysCommon.UserRole = MDD.DAL.LoginDAL.GetUserRole(info.Username);
			SysCommon.CardNo = "";
		}
		catch (Exception e)
		{
			resultInfo.Status = 500;
			resultInfo.Code = 500;
			resultInfo.Msg = "登录服务异常：" + e.Message;
			LogHelper.Debug("LoginBLL", e);
			return (resultInfo, new UserInfo());
		}
		return (resultInfo, userInfo);
	}
}
