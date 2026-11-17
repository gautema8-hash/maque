using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using MDD.Utility;

namespace MDD.DAL;

public class LoginDAL
{
	// 登录口令盐值（与数据库预置账号的哈希保持一致）
	public const string Salt = "maque#2026";

	public static string Sha256Hex(string text)
	{
		using SHA256 sha = SHA256.Create();
		byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
		return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
	}

	/// <summary>校验账号密码是否正确（本地 MySQL maque.users 表）</summary>
	public static (bool ok, string msg) VerifyUser(string username, string password)
	{
		string safeName = (username ?? "").Replace("'", "''");
		DataTable dt = SqlHelper.Query("select password_hash from users where username = '" + safeName + "'");
		if (dt.Rows.Count == 0)
		{
			return (false, "账号不存在，请检查用户名");
		}
		string storedHash = Convert.ToString(dt.Rows[0][0]) ?? "";
		string inputHash = Sha256Hex((password ?? "") + "|" + Salt);
		if (!string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase))
		{
			return (false, "密码错误，请重新输入");
		}
		return (true, "登录成功");
	}

	/// <summary>查询用户角色</summary>
	public static string GetUserRole(string username)
	{
		string safeName = (username ?? "").Replace("'", "''");
		DataTable dt = SqlHelper.Query("select role from users where username = '" + safeName + "'");
		if (dt.Rows.Count == 0) return "user";
		return Convert.ToString(dt.Rows[0][0]) ?? "user";
	}
}
