using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MDD.Common;
using MDD.Models;
using MDD.Models.Video;
using Newtonsoft.Json;

namespace MDD.Utility;

public class WebHelper
{
	private static readonly string connStr = SysCommon.SysUrl.HttpUrl;

	private static readonly string connStr2 = SysCommon.SysUrl.HttpUrl2;

	private static readonly string connStr3 = SysCommon.SysUrl.HttpUrl3;

	public static async Task<string> Get(string url, string data)
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		url = connStr + url;
		string result = "";
		try
		{
			url = url + "?" + data;
			using (HttpClient client = new HttpClient())
			{
				HttpResponseMessage obj = await client.GetAsync(url);
				obj.EnsureSuccessStatusCode();
				result = await obj.Content.ReadAsStringAsync();
			}
			return result;
		}
		catch (Exception)
		{
			throw;
		}
	}

	public static string PostAsync(string url, Dictionary<string, string> pData = null, string token = "", bool isUrl1 = false)
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		url = (isUrl1 ? (connStr + url) : ((SysCommon.SysUrl.source == "1") ? (connStr2 + url) : ((!(SysCommon.SysUrl.source == "3")) ? (connStr + url) : (connStr3 + url))));
		string text = "";
		try
		{
			MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
			if (pData != null)
			{
				foreach (KeyValuePair<string, string> pDatum in pData)
				{
					multipartFormDataContent.Add(new StringContent(GetNullOrString(pDatum.Value)), pDatum.Key);
				}
			}
			LogHelper.Info("post请求：" + url + ",数据：" + JsonConvert.SerializeObject(pData) + ",token：" + token);
			text = new HttpClient
			{
				DefaultRequestHeaders = 
				{
					{ "user-agent", "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.91 Mobile Safari/537.36" },
					{ "token", token }
				}
			}.PostAsync(url, multipartFormDataContent).Result.Content.ReadAsStringAsync().Result;
		}
		catch (Exception ex)
		{
			LogHelper.Error("http请求：" + url + "；数据：" + JsonConvert.SerializeObject(pData) + "；token：" + token, ex);
			text = ex.Message;
		}
		LogHelper.Info("http请求返回结果：" + url + "；数据：" + JsonConvert.SerializeObject(pData) + "；result：" + text.ToString());
		return text;
	}

	public static string Post(string url, string data)
	{
		string result = "";
		try
		{
			StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
			HttpResponseMessage result2 = new HttpClient().PostAsync(url, content).Result;
			if (result2.IsSuccessStatusCode)
			{
				result = result2.Content.ReadAsStringAsync().Result;
			}
		}
		catch (Exception ex)
		{
			throw ex;
		}
		return result;
	}

	public static string GetKSVideo(KSVideoSearch ks, Dictionary<string, string> cookies = null)
	{
		string text = "https://www.kuaishou.com/graphql";
		string text2 = "";
		try
		{
			HttpClientHandler httpClientHandler = new HttpClientHandler();
			CookieContainer cookieContainer = new CookieContainer();
			if (cookies != null)
			{
				foreach (KeyValuePair<string, string> cookie in cookies)
				{
					if (!string.IsNullOrEmpty(cookie.Value))
					{
						cookieContainer.Add(new Uri("https://www.kuaishou.com/"), new Cookie(cookie.Key, cookie.Value));
					}
				}
			}
			httpClientHandler.CookieContainer = cookieContainer;
			HttpClient obj = new HttpClient(httpClientHandler)
			{
				DefaultRequestHeaders = 
				{
					{ "Pragma", "no-cache" },
					{ "Cache-Control", "no-cache" },
					{ "sec-ch-ua", "\"Google Chrome\";v=\"111\", \"Not(A: Brand\";v=\"8\", \"Chromium\";v=\"111\"" },
					{ "accept", "*/*" },
					{ "sec-ch-ua-mobile", "?0" },
					{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.51 Safari/537.36" },
					{ "sec-ch-ua-platform", "\"Windows\"" },
					{ "Origin", "https://www.kuaishou.com" },
					{ "Sec-Fetch-Site", "same-origin" },
					{ "Sec-Fetch-Mode", "cors" },
					{ "Sec-Fetch-Dest", "empty" },
					{ "Referer", "https://www.kuaishou.com/search/video?searchKey=%E5%9E%83%E5%9C%BE%E8%A2%8B" },
					{ "Accept-Language", "zh-TW,zh-CN;q=0.9,zh;q=0.8,en-US;q=0.7,en;q=0.6" }
				}
			};
			HttpContent content = new StringContent(JsonConvert.SerializeObject(ks), Encoding.UTF8, "application/json");
			return obj.PostAsync(text, content).Result.Content.ReadAsStringAsync().Result;
		}
		catch (Exception e)
		{
			LogHelper.Error("快手采集失败：" + text + "；数据：" + JsonConvert.SerializeObject(ks), e);
			return "500";
		}
	}

	public static async Task<string> GetDYVideo(string url, string cookieString = "")
	{
		string result;
		try
		{
			HttpClientHandler httpClientHandler = new HttpClientHandler();
			CookieContainer cookieContainer = new CookieContainer();
			if (!string.IsNullOrEmpty(cookieString))
			{
				string[] array = cookieString.Split(';');
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split(new char[1] { '=' }, 2);
					if (array2.Length > 1)
					{
						string name = array2[0].Replace(" ", "");
						string value = array2[1].Replace(" ", "");
						cookieContainer.Add(new Uri("https://www.douyin.com/"), new Cookie(name, value));
					}
				}
			}
			httpClientHandler.CookieContainer = cookieContainer;
			result = await (await new HttpClient(httpClientHandler)
			{
				DefaultRequestHeaders = 
				{
					{ "authority", "www.douyin.com" },
					{ "Pragma", "no-cache" },
					{ "Cache-Control", "no-cache" },
					{ "sec-ch-ua", "\"Google Chrome\";v=\"111\", \"Not(A: Brand\";v=\"8\", \"Chromium\";v=\"111\"" },
					{ "accept", "application/json, text/plain, */*" },
					{ "sec-ch-ua-mobile", "?0" },
					{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36" },
					{ "sec-ch-ua-platform", "\"Windows\"" },
					{ "Origin", "https://www.douyin.com" },
					{ "Sec-Fetch-Site", "same-origin" },
					{ "Sec-Fetch-Mode", "cors" },
					{ "Sec-Fetch-Dest", "empty" },
					{ "Referer", "https://www.douyin.com/search/%E7%BE%8E%E9%A3%9F?publish_time=0&sort_type=0&source=switch_tab&type=video" },
					{ "Accept-Language", "zh-TW,zh-CN;q=0.9,zh;q=0.8,en-US;q=0.7,en;q=0.6" }
				}
			}.GetAsync(url)).Content.ReadAsStringAsync();
		}
		catch (Exception e)
		{
			LogHelper.Error("抖音采集失败：" + url + "；cookie：" + cookieString, e);
			result = "500";
		}
		return result;
	}

	public static string DownloadFile(string url, string path, DownloadClass downClass)
	{
		try
		{
			WebRequest webRequest = WebRequest.Create(url);
			WebResponse respone = webRequest.GetResponse();
			long Maximum = respone.ContentLength;
			ThreadPool.QueueUserWorkItem(delegate
			{
				Stream responseStream = respone.GetResponseStream();
				Stream stream = new FileStream(path, FileMode.Create);
				byte[] array = new byte[1024];
				long num = 0L;
				int num2 = responseStream.Read(array, 0, array.Length);
				while (num2 > 0)
				{
					stream.Write(array, 0, num2);
					num += num2;
					num2 = responseStream.Read(array, 0, array.Length);
					downClass.DoSomethingAsync(Maximum, num);
				}
				responseStream.Close();
				stream.Close();
			}, null);
		}
		catch (Exception ex)
		{
			_ = ex.Message;
		}
		return "";
	}

	public static string GetNullOrString(object obj)
	{
		if (obj == null)
		{
			return string.Empty;
		}
		return obj.ToString();
	}
}
