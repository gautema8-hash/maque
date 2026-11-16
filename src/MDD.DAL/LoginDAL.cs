using System.Collections.Generic;
using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class LoginDAL
{
	public static string Login(LoginInfo info)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("username", info.Username);
		dictionary.Add("password", info.Password);
		dictionary.Add("ss", "2");
		return WebHelper.PostAsync("member/login/apiLoginGo", dictionary);
	}
}
