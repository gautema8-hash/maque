using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MDD.Common;
using MDD.Models;
using MDD.Models.Enum;
using Newtonsoft.Json;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class ProductReviewsDownload : Window
{
	private ProductInfo product;

	private Thread thread;

	private int downloadCount = 0;

	private string savePath;







	public ProductReviewsDownload()
	{
		InitializeComponent();
	}

	public ProductReviewsDownload(ProductInfo productInfo)
	{
		InitializeComponent();
		product = productInfo;
		Init();
	}

	private void Init()
	{
		try
		{
			downloadProgressBar.Visibility = Visibility.Hidden;
			chsbSavePath.Text = SysConfig.GetConfigString("productDownloadPathDefault");
			if (product != null)
			{
			}
		}
		catch (Exception)
		{
		}
	}

	public void DragWindow(object sender, MouseButtonEventArgs args)
	{
		try
		{
			DragMove();
		}
		catch
		{
		}
	}

	private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void btnDownloadReviews_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(chsbSavePath.Text.Trim()))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "保存路径不能为空！请选择保存路径。", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		if (!Directory.Exists(chsbSavePath.Text))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "保存路径不存在！请选择保存路径。", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		if (string.IsNullOrEmpty(txtDownloadCount.Text.Trim()))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "评论下载数量不能为空！请填写正确的数字。", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		if (!int.TryParse(txtDownloadCount.Text.Trim(), out downloadCount))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "评论下载数量请填写正确的数字！", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		savePath = chsbSavePath.Text + "\\" + product.mallName + "\\" + product.goodsName + "\\商品评论";
		thread = new Thread(ThreadStart);
		thread.Start();
	}

	private void ThreadStart()
	{
		try
		{
			int num = 10;
			int num2 = downloadCount / num;
			if (downloadCount % num != 0)
			{
				num2++;
			}
			int rowIndex = 0;
			base.Dispatcher.Invoke(delegate
			{
				btnDownloadReviews.IsEnabled = false;
				downloadProgressBar.Value = 0.0;
				downloadProgressBar.Maximum = downloadCount;
				downloadProgressBar.Visibility = Visibility.Visible;
			});
			for (int i = 1; i <= num2; i++)
			{
				ProductReviews productReviews = new ProductReviews();
				using (WebClient webClient = new WebClient())
				{
					webClient.Headers.Add("authority", "mobile.pinduoduo.com");
					webClient.Headers.Add("accept", "application/json, text/plain, */*");
					webClient.Headers.Add("accesstoken", product.cookieInfo.accessToken);
					webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) QtWebEngine/5.15.2 Chrome/83.0.4103.122 Safari/537.36");
					webClient.Headers.Add("verifyauthtoken", product.cookieInfo.verifyAuthToken);
					webClient.Headers.Add("sec-fetch-site", "same-origin");
					webClient.Headers.Add("sec-fetch-mode", "cors");
					webClient.Headers.Add("sec-fetch-dest", "empty");
					webClient.Headers.Add("content-type", "application/json;charset=UTF-8");
					webClient.Headers.Add("referer", "https://mobile.pinduoduo.com/goods_comments.html?goods_id=266249279677&force_use_web_bundle=1&mall_id=621053946&refer_page_name=goods_detail&refer_page_id=10014_1665151165992_jpvdzti1gd&refer_page_sn=10014");
					webClient.Headers.Add("cookie", product.cookieInfo.cookie);
					Cookie cookie = product.cookieInfo.webCookies.FirstOrDefault((Cookie a) => a.Name == "pdd_user_id");
					string address = string.Format("https://mobile.pinduoduo.com/proxy/api/reviews/{0}/list?label_id=0&page={1}&size={2}&enable_video=1&enable_group_review=1&pdduid={3}", product.goodsID, i, num, (cookie == null) ? "2011587188604" : cookie.Value);
					string value = webClient.DownloadString(address);
					productReviews = JsonConvert.DeserializeObject<ProductReviews>(value);
				}
				if (productReviews.data == null || productReviews.data.Count <= 0)
				{
					continue;
				}
				foreach (ReviewData datum in productReviews.data)
				{
					if (rowIndex >= downloadCount)
					{
						break;
					}
					rowIndex++;
					if (!Directory.Exists(savePath + "\\" + rowIndex))
					{
						Directory.CreateDirectory(savePath + "\\" + rowIndex);
					}
					string path = savePath + "\\" + rowIndex + "\\评论内容.txt";
					using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
					{
						using StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
						streamWriter.WriteLine("下载时间：" + DateTime.Now.ToString("yyyy年MM月dd日 HH时mm分ss秒"));
						streamWriter.WriteLine("买家名称：" + datum.name);
						List<ProductSkusSpecs> source = JsonConvert.DeserializeObject<List<ProductSkusSpecs>>(datum.specs);
						streamWriter.WriteLine("规格：" + string.Join(" ", source.Select((ProductSkusSpecs a) => a.spec_key + "：" + a.spec_value)));
						streamWriter.WriteLine("描述评分：" + datum.desc_score);
						streamWriter.WriteLine("物流评分：" + datum.logistics_score);
						streamWriter.WriteLine("服务评分：" + datum.service_score);
						streamWriter.WriteLine("综合评分：" + datum.comprehensive_dsr);
						DateTime? dateTime = GetDateTime(datum.time);
						if (dateTime.HasValue)
						{
							streamWriter.WriteLine("评论时间：" + dateTime.Value.ToString("yyyy年MM月dd日"));
						}
						streamWriter.WriteLine("评论内容：" + datum.comment.Replace("\n", "\n\t\t\t"));
						if (!string.IsNullOrEmpty(datum.append_time_text))
						{
							streamWriter.WriteLine(datum.append_time_text);
							DateTime? dateTime2 = GetDateTime(datum.append.time);
							if (dateTime2.HasValue)
							{
								streamWriter.WriteLine("追评时间：" + dateTime2.Value.ToString("yyyy年MM月dd日"));
							}
							streamWriter.WriteLine("追评内容：" + datum.append.comment.Replace("\n", "\n\t\t\t"));
						}
						if (!string.IsNullOrEmpty(datum.reply_count) && datum.reply_count != "0")
						{
							streamWriter.WriteLine("回复数量：" + datum.reply_count);
							streamWriter.WriteLine("回复内容(简)：");
							List<ReviewReply> list = datum.reply_list.OrderBy((ReviewReply a) => a.time).ToList();
							string text = "";
							foreach (ReviewReply item in list)
							{
								text = text + "\n\t\t\t[" + item.user_info.nick_name + "]" + ((item.parent_user_info != null) ? ("回复[" + item.parent_user_info.nick_name + "]") : "") + "：" + item.content;
							}
							streamWriter.WriteLine(text);
						}
					}
					if (datum.pictures != null && datum.pictures.Count > 0)
					{
						int num3 = 1;
						foreach (ReviewPicture picture in datum.pictures)
						{
							using (WebClient webClient2 = new WebClient())
							{
								webClient2.Headers.Add("authority", "mobile.pinduoduo.com");
								webClient2.Headers.Add("accept", "application/json, text/plain, */*");
								webClient2.Headers.Add("accesstoken", product.cookieInfo.accessToken);
								webClient2.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) QtWebEngine/5.15.2 Chrome/83.0.4103.122 Safari/537.36");
								webClient2.Headers.Add("verifyauthtoken", product.cookieInfo.verifyAuthToken);
								webClient2.Headers.Add("sec-fetch-site", "same-origin");
								webClient2.Headers.Add("sec-fetch-mode", "cors");
								webClient2.Headers.Add("sec-fetch-dest", "empty");
								webClient2.Headers.Add("content-type", "application/json;charset=UTF-8");
								webClient2.Headers.Add("referer", "https://mobile.pinduoduo.com/goods_comments.html?goods_id=266249279677&force_use_web_bundle=1&mall_id=621053946&refer_page_name=goods_detail&refer_page_id=10014_1665151165992_jpvdzti1gd&refer_page_sn=10014");
								webClient2.Headers.Add("cookie", product.cookieInfo.cookie);
								string text2 = savePath + "\\" + rowIndex + "\\评论图片" + num3;
								string text3 = ".jpeg";
								if (picture.url.LastIndexOf('.') != -1)
								{
									text3 = picture.url.Substring(picture.url.LastIndexOf('.'));
								}
								text2 += text3;
								webClient2.DownloadFile(picture.url, text2);
							}
							num3++;
							Thread.Sleep(300);
						}
					}
					if (datum.video != null && !string.IsNullOrEmpty(datum.video.url))
					{
						int num4 = 1;
						using WebClient webClient3 = new WebClient();
						webClient3.Headers.Add("authority", "mobile.pinduoduo.com");
						webClient3.Headers.Add("accept", "application/json, text/plain, */*");
						webClient3.Headers.Add("accesstoken", product.cookieInfo.accessToken);
						webClient3.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) QtWebEngine/5.15.2 Chrome/83.0.4103.122 Safari/537.36");
						webClient3.Headers.Add("verifyauthtoken", product.cookieInfo.verifyAuthToken);
						webClient3.Headers.Add("sec-fetch-site", "same-origin");
						webClient3.Headers.Add("sec-fetch-mode", "cors");
						webClient3.Headers.Add("sec-fetch-dest", "empty");
						webClient3.Headers.Add("content-type", "application/json;charset=UTF-8");
						webClient3.Headers.Add("referer", "https://mobile.pinduoduo.com/goods_comments.html?goods_id=266249279677&force_use_web_bundle=1&mall_id=621053946&refer_page_name=goods_detail&refer_page_id=10014_1665151165992_jpvdzti1gd&refer_page_sn=10014");
						webClient3.Headers.Add("cookie", product.cookieInfo.cookie);
						string text4 = savePath + "\\" + rowIndex + "\\评论视频" + num4;
						string text5 = ".mp4";
						if (datum.video.url.LastIndexOf('.') != -1)
						{
							text5 = datum.video.url.Substring(datum.video.url.LastIndexOf('.'));
						}
						text4 += text5;
						webClient3.DownloadFile(datum.video.url, text4);
					}
					if (datum.append != null && datum.append.pictures != null && datum.append.pictures.Count > 0)
					{
						int num5 = 1;
						foreach (ReviewPicture picture2 in datum.append.pictures)
						{
							using (WebClient webClient4 = new WebClient())
							{
								webClient4.Headers.Add("authority", "mobile.pinduoduo.com");
								webClient4.Headers.Add("accept", "application/json, text/plain, */*");
								webClient4.Headers.Add("accesstoken", product.cookieInfo.accessToken);
								webClient4.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) QtWebEngine/5.15.2 Chrome/83.0.4103.122 Safari/537.36");
								webClient4.Headers.Add("verifyauthtoken", product.cookieInfo.verifyAuthToken);
								webClient4.Headers.Add("sec-fetch-site", "same-origin");
								webClient4.Headers.Add("sec-fetch-mode", "cors");
								webClient4.Headers.Add("sec-fetch-dest", "empty");
								webClient4.Headers.Add("content-type", "application/json;charset=UTF-8");
								webClient4.Headers.Add("referer", "https://mobile.pinduoduo.com/goods_comments.html?goods_id=266249279677&force_use_web_bundle=1&mall_id=621053946&refer_page_name=goods_detail&refer_page_id=10014_1665151165992_jpvdzti1gd&refer_page_sn=10014");
								webClient4.Headers.Add("cookie", product.cookieInfo.cookie);
								string text6 = savePath + "\\" + rowIndex + "\\追评图片" + num5;
								string text7 = ".jpeg";
								if (picture2.url.LastIndexOf('.') != -1)
								{
									text7 = picture2.url.Substring(picture2.url.LastIndexOf('.'));
								}
								text6 += text7;
								webClient4.DownloadFile(picture2.url, text6);
							}
							num5++;
							Thread.Sleep(300);
						}
					}
					if (datum.append != null && datum.append.video != null && !string.IsNullOrEmpty(datum.append.video.url))
					{
						int num6 = 1;
						using WebClient webClient5 = new WebClient();
						webClient5.Headers.Add("authority", "mobile.pinduoduo.com");
						webClient5.Headers.Add("accept", "application/json, text/plain, */*");
						webClient5.Headers.Add("accesstoken", product.cookieInfo.accessToken);
						webClient5.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) QtWebEngine/5.15.2 Chrome/83.0.4103.122 Safari/537.36");
						webClient5.Headers.Add("verifyauthtoken", product.cookieInfo.verifyAuthToken);
						webClient5.Headers.Add("sec-fetch-site", "same-origin");
						webClient5.Headers.Add("sec-fetch-mode", "cors");
						webClient5.Headers.Add("sec-fetch-dest", "empty");
						webClient5.Headers.Add("content-type", "application/json;charset=UTF-8");
						webClient5.Headers.Add("referer", "https://mobile.pinduoduo.com/goods_comments.html?goods_id=266249279677&force_use_web_bundle=1&mall_id=621053946&refer_page_name=goods_detail&refer_page_id=10014_1665151165992_jpvdzti1gd&refer_page_sn=10014");
						webClient5.Headers.Add("cookie", product.cookieInfo.cookie);
						string text8 = savePath + "\\" + rowIndex + "\\追评视频" + num6;
						string text9 = ".mp4";
						if (datum.append.video.url.LastIndexOf('.') != -1)
						{
							text9 = datum.append.video.url.Substring(datum.append.video.url.LastIndexOf('.'));
						}
						text8 += text9;
						webClient5.DownloadFile(datum.append.video.url, text8);
					}
					base.Dispatcher.Invoke(delegate
					{
						txtDownloadedCount.Text = rowIndex.ToString();
						downloadProgressBar.Value = rowIndex;
					});
					Thread.Sleep(300);
				}
			}
			base.Dispatcher.Invoke(delegate
			{
				SysMessage.MessageShow(Window.GetWindow(this), "评论下载成功。", "提示：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception exception = ex2;
			base.Dispatcher.Invoke(delegate
			{
				SysMessage.MessageShow(Window.GetWindow(this), "下载商品详情出错！" + exception.Message, "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			});
		}
		finally
		{
			base.Dispatcher.Invoke(delegate
			{
				btnDownloadReviews.IsEnabled = true;
				downloadProgressBar.Visibility = Visibility.Hidden;
			});
		}
	}

	private DateTime? GetDateTime(string timestamp)
	{
		if (string.IsNullOrEmpty(timestamp))
		{
			return null;
		}
		try
		{
			DateTime dateTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
			long ticks = long.Parse(timestamp + "0000000");
			TimeSpan value = new TimeSpan(ticks);
			return dateTime.Add(value).AddHours(8.0);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private string GetReviewDataReplyListComment(string pid, int depth, List<ReviewReply> reply_list)
	{
		string text = "";
		string text2 = "";
		for (int i = 0; i < depth; i++)
		{
			text2 += "\t\t";
		}
		text2 = "\n" + text2;
		List<ReviewReply> list = new List<ReviewReply>();
		if (depth == 1)
		{
			(from a in reply_list
				where a.root_reply_id == ""
				orderby a.time
				select a).ToList();
		}
		else
		{
			(from a in reply_list
				where a.parent_reply_id == pid
				orderby a.time
				select a).ToList();
		}
		foreach (ReviewReply item in list)
		{
			text = text + text2 + "[" + item.user_info.nick_name + "]：" + item.content;
			text += GetReviewDataReplyListComment(item.reply_id, depth + 1, reply_list);
		}
		return text;
	}

	private void chsbSavePath_TextChanged(object sender, TextChangedEventArgs e)
	{
		string text = (sender as ChooseBox).Text;
		if (!string.IsNullOrEmpty(text) && text != SysConfig.GetConfigString("productDownloadPathDefault"))
		{
			SysConfig.UpdateConfigSetting("productDownloadPathDefault", text);
		}
	}


}
