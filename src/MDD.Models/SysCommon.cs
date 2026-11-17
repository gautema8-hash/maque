using System.Collections.Generic;
using System.Text;
using MDD.Models.Video;

namespace MDD.Models;

public static class SysCommon
{
	public static bool isEnd = false;

	public static bool isSuofang = true;

	public static StringBuilder ResponseJson { get; set; } = new StringBuilder();


	public static JsonResponse ResponseData { get; set; }

	public static string Miyao { get; set; }

	public static string Path { get; set; }

	public static UserInfo UserInfo { get; set; }

	public static string Username { get; set; }

	public static string Password { get; set; }

	/// <summary>登录方式：password（账号密码） / card（卡密）</summary>
	public static string LoginMethod { get; set; } = "password";

	/// <summary>当前登录角色：admin / user / card</summary>
	public static string UserRole { get; set; } = "user";

	/// <summary>当前卡密号（卡密登录时）</summary>
	public static string CardNo { get; set; } = "";

	public static SysUrl SysUrl { get; set; } = new SysUrl();


	public static string NoticeContent { get; set; }

	public static PopNotice PopNotice { get; set; }

	public static ConfigInfo ConfigInfo { get; set; }

	public static KsCookieInfo KsCookieInfo { get; set; } = new KsCookieInfo();


	public static List<DYVideoInfo> DyVideoList { get; set; }

	public static bool isDyCj { get; set; } = false;


	public static string VideoPath { get; set; }

	public static List<StoreInfo> SelectStore { get; set; } = new List<StoreInfo>();


	public static ImgCode ImgCode { get; set; } = new ImgCode();

}
