using MDD.Models;
using Newtonsoft.Json;

namespace MDD.DAL;

public class HotDAL
{
	// 本地重建版：爆款推荐数据本地生成（无远程厂商接口）
	public static string GetHotInfo(HotSearch hot)
	{
		var result = new { code = 200, msg = "本地模式（无远程数据）", data = new object[0] };
		return JsonConvert.SerializeObject(result);
	}
}
