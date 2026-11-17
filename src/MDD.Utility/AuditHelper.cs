using System;
using MDD.Models;

namespace MDD.Utility;

/// <summary>
/// 审计字段统一取值：创建人/更新人、创建时间/更新时间、逻辑删除。
/// 卡密登录时操作人用卡密号代替用户名。
/// </summary>
public static class AuditHelper
{
	/// <summary>当前操作人：卡密登录=卡密号；否则=用户名</summary>
	public static string CurrentUser
	{
		get
		{
			if (SysCommon.LoginMethod == "card" && !string.IsNullOrWhiteSpace(SysCommon.CardNo))
				return SysCommon.CardNo;
			return string.IsNullOrWhiteSpace(SysCommon.Username) ? "system" : SysCommon.Username;
		}
	}

	/// <summary>当前时间字符串</summary>
	public static string Now => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

	/// <summary>SQL 片段：新增记录时的审计列与值（不含外键字段）</summary>
	public static string InsertColumns => "created_by, created_at, updated_by, updated_at, is_deleted";

	public static string InsertValues
	{
		get
		{
			string u = CurrentUser.Replace("'", "''");
			return "'" + u + "', '" + Now + "', '" + u + "', '" + Now + "', 0";
		}
	}

	/// <summary>SQL 片段：更新记录时的审计列赋值（前面需加逗号）</summary>
	public static string UpdateSet
	{
		get
		{
			string u = CurrentUser.Replace("'", "''");
			return ", updated_by='" + u + "', updated_at='" + Now + "'";
		}
	}
}
