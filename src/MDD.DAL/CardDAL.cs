using System;
using System.Data;
using MDD.Utility;

namespace MDD.DAL;

/// <summary>
/// 卡密（授权码）数据访问：生成 / 校验 / 作废。
/// 卡密状态：active=未使用，used=已使用，disabled=已失效。
/// 有效期单位：hour=小时，day=天，month=月，year=年。
/// </summary>
public class CardDAL
{
	/// <summary>校验卡密是否可登录。返回 (是否可用, 提示信息, 到期时间)</summary>
	public static (bool ok, string msg, DateTime expireAt) VerifyCard(string cardNo)
	{
		string no = (cardNo ?? "").Trim();
		if (no.Length == 0)
		{
			return (false, "卡密号不能为空", DateTime.MinValue);
		}
		string safeNo = no.Replace("'", "''");
		DataTable dt = SqlHelper.Query("select validity_value, validity_unit, status, created_at, used_at, expire_at, bind_user from cards where is_deleted=0 and card_no = '" + safeNo + "'");
		if (dt.Rows.Count == 0)
		{
			return (false, "卡密不存在，请检查卡密号", DateTime.MinValue);
		}
		DataRow row = dt.Rows[0];
		string status = Convert.ToString(row["status"]) ?? "active";
		if (string.Equals(status, "disabled", StringComparison.OrdinalIgnoreCase))
		{
			return (false, "卡密已失效，请联系管理员", DateTime.MinValue);
		}
		if (string.Equals(status, "used", StringComparison.OrdinalIgnoreCase))
		{
			return (false, "卡密已被使用", DateTime.MinValue);
		}

		int value = 1;
		int.TryParse(Convert.ToString(row["validity_value"]), out value);
		string unit = Convert.ToString(row["validity_unit"]) ?? "day";
		string createdAt = Convert.ToString(row["created_at"]);
		DateTime baseTime = DateTime.Now;
		DateTime parsed;
		if (DateTime.TryParse(createdAt, out parsed))
		{
			baseTime = parsed;
		}
		DateTime expireAt = AddValidity(baseTime, value, unit);

		// 若已显式写入 expire_at，以数据库值为准
		string storedExpire = Convert.ToString(row["expire_at"]);
		if (DateTime.TryParse(storedExpire, out parsed) && parsed > DateTime.MinValue)
		{
			expireAt = parsed;
		}

		if (DateTime.Now > expireAt)
		{
			return (false, "卡密已过期（到期时间：" + expireAt.ToString("yyyy-MM-dd HH:mm") + "）", expireAt);
		}
		return (true, "卡密有效", expireAt);
	}

	/// <summary>按单位计算到期时间</summary>
	public static DateTime AddValidity(DateTime from, int value, string unit)
	{
		if (value <= 0) value = 1;
		switch ((unit ?? "day").ToLowerInvariant())
		{
			case "hour": return from.AddHours(value);
			case "month": return from.AddMonths(value);
			case "year": return from.AddYears(value);
			case "day":
			default: return from.AddDays(value);
		}
	}

	/// <summary>生成卡密；卡密号统一为 32 位 UUID（无连字符）</summary>
	public static string GenerateCard(string cardNo, int value, string unit, string note)
	{
		if (value <= 0) value = 1;
		if (string.IsNullOrWhiteSpace(unit)) unit = "day";
		// 卡密号强制为 32 位 UUID，忽略手工输入
		string no = Guid.NewGuid().ToString("N");
		string safeNote = (note ?? "").Replace("'", "''");
		string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		DateTime expire = AddValidity(DateTime.Now, value, unit);
		string expireStr = expire.ToString("yyyy-MM-dd HH:mm:ss");
		return SqlHelper.Update("insert into cards (card_no, validity_value, validity_unit, status, created_at, expire_at, note, " + AuditHelper.InsertColumns + ") values ('" + no + "', " + value + ", '" + unit + "', 'active', '" + now + "', '" + expireStr + "', '" + safeNote + "', " + AuditHelper.InsertValues + ")");
	}

	/// <summary>返回卡密的展示状态：已使用/已失效/过期/未使用，并实时计算剩余时间</summary>
	public static (string statusText, string remainText) GetCardDisplay(DataRow row)
	{
		string status = Convert.ToString(row["status"]) ?? "active";
		string expireStr = Convert.ToString(row["expire_at"]);
		DateTime expire;
		bool hasExpire = DateTime.TryParse(expireStr, out expire);
		bool isExpired = hasExpire && DateTime.Now > expire;

		string statusText;
		if (string.Equals(status, "used", StringComparison.OrdinalIgnoreCase)) statusText = "已使用";
		else if (string.Equals(status, "disabled", StringComparison.OrdinalIgnoreCase)) statusText = "已失效";
		else if (isExpired) statusText = "过期";
		else statusText = "未使用";

		string remainText;
		if (isExpired) remainText = "已过期";
		else if (!hasExpire) remainText = "—";
		else
		{
			TimeSpan ts = expire - DateTime.Now;
			if (ts.TotalDays >= 1) remainText = ((int)ts.TotalDays) + "天" + ts.Hours + "小时";
			else if (ts.TotalHours >= 1) remainText = ((int)ts.TotalHours) + "小时" + ts.Minutes + "分";
			else remainText = Math.Max(0, (int)ts.TotalMinutes) + "分钟";
		}
		return (statusText, remainText);
	}

	/// <summary>标记卡密已使用</summary>
	public static string MarkUsed(string cardNo, string bindUser)
	{
		string no = (cardNo ?? "").Replace("'", "''");
		string user = (bindUser ?? "").Replace("'", "''");
		string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		return SqlHelper.Update("update cards set status = 'used', used_at = '" + now + "', bind_user = '" + user + "'" + AuditHelper.UpdateSet + " where card_no = '" + no + "'");
	}

	/// <summary>作废卡密</summary>
	public static string DisableCard(string cardNo)
	{
		string no = (cardNo ?? "").Replace("'", "''");
		return SqlHelper.Update("update cards set status = 'disabled'" + AuditHelper.UpdateSet + " where card_no = '" + no + "'");
	}

	/// <summary>删除卡密（逻辑删除：is_deleted=1）</summary>
	public static string DeleteCard(string cardNo)
	{
		string no = (cardNo ?? "").Replace("'", "''");
		return SqlHelper.Update("update cards set is_deleted = 1" + AuditHelper.UpdateSet + " where card_no = '" + no + "'");
	}

	/// <summary>编辑卡密：有效期数值/单位/备注/状态</summary>
	public static string UpdateCard(string cardNo, int value, string unit, string note, string status)
	{
		string no = (cardNo ?? "").Replace("'", "''");
		if (value <= 0) value = 1;
		if (string.IsNullOrWhiteSpace(unit)) unit = "day";
		if (string.IsNullOrWhiteSpace(status)) status = "active";
		string safeNote = (note ?? "").Replace("'", "''");
		// 重新计算到期时间（以创建时间为基准）
		DataTable dt = SqlHelper.Query("select created_at from cards where is_deleted=0 and card_no = '" + no + "'");
		DateTime baseTime = DateTime.Now;
		if (dt.Rows.Count > 0) DateTime.TryParse(Convert.ToString(dt.Rows[0]["created_at"]), out baseTime);
		string expire = AddValidity(baseTime, value, unit).ToString("yyyy-MM-dd HH:mm:ss");
		return SqlHelper.Update("update cards set validity_value=" + value + ", validity_unit='" + unit +
			"', note='" + safeNote + "', status='" + status + "', expire_at='" + expire + "'" + AuditHelper.UpdateSet + " where card_no='" + no + "'");
	}

	/// <summary>按关键字查询卡密（卡密号/备注/绑定用户）</summary>
	public static DataTable SearchCards(string keyword)
	{
		string kw = (keyword ?? "").Trim().Replace("'", "''");
		if (kw.Length == 0) return ListCards();
		return SqlHelper.Query("select card_no, validity_value, validity_unit, status, created_at, used_at, expire_at, bind_user, note from cards where is_deleted=0 and (card_no like '%" + kw + "%' or note like '%" + kw + "%' or bind_user like '%" + kw + "%') order by id desc");
	}

	/// <summary>查询全部卡密</summary>
	public static DataTable ListCards()
	{
		return SqlHelper.Query("select card_no, validity_value, validity_unit, status, created_at, used_at, expire_at, bind_user, note from cards where is_deleted=0 order by id desc");
	}

	/// <summary>修正旧卡密缺失的到期时间（按创建时间+有效期回填）</summary>
	public static void BackfillExpire()
	{
		DataTable dt = SqlHelper.Query("select card_no, validity_value, validity_unit, created_at from cards where is_deleted=0 and (expire_at is null or expire_at = '')");
		foreach (DataRow row in dt.Rows)
		{
			string no = Convert.ToString(row["card_no"]).Replace("'", "''");
			int value = 1;
			int.TryParse(Convert.ToString(row["validity_value"]), out value);
			string unit = Convert.ToString(row["validity_unit"]) ?? "day";
			DateTime from = DateTime.Now;
			DateTime.TryParse(Convert.ToString(row["created_at"]), out from);
			string expire = AddValidity(from, value, unit).ToString("yyyy-MM-dd HH:mm:ss");
			SqlHelper.Update("update cards set expire_at = '" + expire + "' where card_no = '" + no + "'");
		}
	}
}
