using System;
using System.Data;
using MDD.DAL;
using MDD.Models;

namespace MDD.BLL;

/// <summary>卡密业务逻辑</summary>
public class CardBLL
{
	/// <summary>卡密登录：校验通过后标记已使用并返回用户信息</summary>
	public static (ResultInfo, UserInfo) LoginByCard(string cardNo)
	{
		ResultInfo resultInfo = new ResultInfo();
		DateTime expireAt;
		string msg;
		bool ok;
		(ok, msg, expireAt) = CardDAL.VerifyCard(cardNo);
		if (!ok)
		{
			resultInfo.Status = 401;
			resultInfo.Code = 401;
			resultInfo.Msg = msg;
			return (resultInfo, new UserInfo());
		}

		string no = (cardNo ?? "").Trim();
		string bindUser = "卡密用户-" + no;
		string markResult = CardDAL.MarkUsed(no, bindUser);
		if (markResult != "true")
		{
			resultInfo.Status = 500;
			resultInfo.Code = 500;
			resultInfo.Msg = "卡密写入失败：" + markResult;
			return (resultInfo, new UserInfo());
		}

		UserInfo userInfo = new UserInfo
		{
			Username = bindUser,
			Vip = true,
			IsPay = true,
			Token = "card-" + DateTime.Now.Ticks,
			ExpirationDate = expireAt.ToString("yyyy-MM-dd HH:mm:ss"),
			LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
			MemberDescription = "麻雀一键铺货 卡密授权版"
		};
		SysCommon.UserInfo = userInfo;
		SysCommon.Username = bindUser;
		SysCommon.LoginMethod = "card";
		SysCommon.UserRole = "card";
		SysCommon.CardNo = no;

		resultInfo.Status = 200;
		resultInfo.Code = 200;
		resultInfo.Msg = "卡密登录成功";
		return (resultInfo, userInfo);
	}

	public static string GenerateCard(string cardNo, int value, string unit, string note)
	{
		return CardDAL.GenerateCard(cardNo, value, unit, note);
	}

	public static string DisableCard(string cardNo)
	{
		return CardDAL.DisableCard(cardNo);
	}

	public static string DeleteCard(string cardNo)
	{
		return CardDAL.DeleteCard(cardNo);
	}

	public static string UpdateCard(string cardNo, int value, string unit, string note, string status)
	{
		return CardDAL.UpdateCard(cardNo, value, unit, note, status);
	}

	public static DataTable ListCards()
	{
		CardDAL.BackfillExpire();
		return CardDAL.ListCards();
	}

	public static DataTable SearchCards(string keyword)
	{
		CardDAL.BackfillExpire();
		return CardDAL.SearchCards(keyword);
	}

	/// <summary>供主窗口校验卡密登录会话是否过期</summary>
	public static (bool ok, string msg) CheckCardSession()
	{
		if (SysCommon.LoginMethod != "card") return (true, "");
		string no = SysCommon.CardNo;
		if (string.IsNullOrWhiteSpace(no)) return (false, "卡密会话无效，请重新登录");
		string safeNo = no.Replace("'", "''");
		DataTable dt = MDD.Utility.SqlHelper.Query("select status, expire_at from cards where is_deleted=0 and card_no = '" + safeNo + "'");
		if (dt.Rows.Count == 0) return (false, "卡密不存在，请重新登录");
		string status = Convert.ToString(dt.Rows[0]["status"]);
		if (status == "disabled") return (false, "卡密已失效，请重新登录");
		DateTime expire;
		if (!DateTime.TryParse(Convert.ToString(dt.Rows[0]["expire_at"]), out expire)) return (true, "");
		if (DateTime.Now > expire) return (false, "卡密已过期，请重新登录");
		return (true, "");
	}
}
