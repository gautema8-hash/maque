using System;
using System.Collections.Generic;
using System.Data;
using MDD.Common;
using MDD.DAL;
using MDD.Models;
using MDD.Models.Video;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDD.BLL;

public class CaptureVideoBLL
{
	public static string GetKSVideo(KSVideoSearch ks, Dictionary<string, string> cookies)
	{
		CaptureVideoDAL.GetKSVideo(ks, cookies);
		return "";
	}

	public static async void GetDYVideo(string url, string cookieString)
	{
		string text = await CaptureVideoDAL.GetDYVideo(url, cookieString);
		try
		{
			if (text == "500" || !text.Contains("aweme_info"))
			{
				return;
			}
			JArray obj = (JArray)JsonConvert.DeserializeObject(((JObject)JsonConvert.DeserializeObject(text))["data"].ToString());
			List<DYVideoInfo> list = new List<DYVideoInfo>();
			foreach (JToken item in obj)
			{
				int result = 0;
				DYVideoInfo dYVideoInfo = new DYVideoInfo();
				dYVideoInfo.AwemeId = item["aweme_info"]["aweme_id"].ToString();
				dYVideoInfo.Desc = item["aweme_info"]["desc"].ToString();
				int.TryParse(item["aweme_info"]["statistics"]["digg_count"].ToString(), out result);
				dYVideoInfo.DiggCount = result;
				dYVideoInfo.Uid = item["aweme_info"]["author"]["uid"].ToString();
				dYVideoInfo.NickName = item["aweme_info"]["author"]["nickname"].ToString();
				dYVideoInfo.KeyWord = "";
				if (item["aweme_info"]["video"]["play_addr"] != null && item["aweme_info"]["video"]["play_addr"]["url_list"] != null)
				{
					dYVideoInfo.VideoUrl = item["aweme_info"]["video"]["play_addr"]["url_list"][0].ToString();
				}
				else
				{
					dYVideoInfo.VideoUrl = "";
				}
				list.Add(dYVideoInfo);
			}
			SysCommon.DyVideoList = list;
		}
		catch (Exception e)
		{
			LogHelper.Error("抖音数据解析失败失败：" + url + "；数据：" + text, e);
		}
	}

	public static string DownloadFile(string url, string path, DownloadClass downClass)
	{
		return CaptureVideoDAL.DownloadFile(url, path, downClass);
	}

	public static DataTable GetDyCookie()
	{
		return CaptureVideoDAL.GetDyCookie();
	}

	public static string UpdateDyCookie(string name, string value)
	{
		return CaptureVideoDAL.UpdateDyCookie(name, value);
	}
}
