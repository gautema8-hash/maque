using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CefSharp;
using CefSharp.Wpf;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using MDD.Models.Video;
using MaoDuoDuo.Web;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class VideoCapture : UserControl
{
	public MainWindow _mainWindow;

	public static Window dyWindow;

	private ChromiumWebBrowser dy_browser;

	private static bool win_dy = true;

	private static bool cj_dy = false;

	private string dy_cookie = "";

	private List<DYVideoInfo> dyList;






	public VideoCapture()
	{
		InitializeComponent();
		Loaded += (s, e) => { if (_mainWindow == null) _mainWindow = Window.GetWindow(this) as MainWindow; };
		initData();
	}

	private void initData()
	{
		text_Cjpath.Text = SysCommon.VideoPath;
		AddDyWindow();
	}

	private void AddDyWindow()
	{
		RequestContextSettings settings = new RequestContextSettings
		{
			CachePath = AppDomain.CurrentDomain.BaseDirectory + "cache/dycache",
			PersistUserPreferences = true,
			PersistSessionCookies = true
		};
		RequestContext requestContext = new RequestContext(settings);
		dy_browser = new ChromiumWebBrowser();
		dy_browser.RequestContext = requestContext;
		dy_browser.JavascriptObjectRepository.Register("colynJsObj", new CallbackObjectForJs(), isAsync: false);
		dy_browser.RequestHandler = new CustomRequestHandler();
		dyWindow = new Window
		{
			Title = "My Browser",
			Content = dy_browser,
			Width = 800.0,
			Height = 500.0
		};
		dy_browser.Address = "https://www.douyin.com";
		dyWindow.Closed += DYWindow_Closed;
		dyWindow.Hide();
		win_dy = true;
	}

	private void SetDyBrowserCookie()
	{
		DataTable dyCookie = CaptureVideoBLL.GetDyCookie();
		if (dyCookie.Rows.Count > 0)
		{
			ICookieManager globalCookieManager = Cef.GetGlobalCookieManager();
			globalCookieManager.SetCookieAsync("http://www.douyin.com", new Cookie
			{
				Domain = "douyin.com",
				Name = dyCookie.Rows[0][0].ToString(),
				Value = dyCookie.Rows[0][1].ToString()
			});
		}
	}

	private void GetDyBrowserCookie()
	{
		dy_cookie = "";
		CookieVisitor cookieVisitor = new CookieVisitor();
		cookieVisitor.SendCookie += Visitor_SendCookie;
		ICookieManager cookieManager = dy_browser.GetCookieManager();
		cookieManager.VisitAllCookies(cookieVisitor);
	}

	private void GetKsVideo(string key_word, int startPage, int endPage)
	{
		KSVideoSearch kSVideoSearch = new KSVideoSearch();
		kSVideoSearch.variables.keyword = key_word;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("kpf", "PC_WEB");
		dictionary.Add("clientid", "3");
		dictionary.Add("did", "web_412dd2a3d0e560b5600ac6eafc00db79");
		dictionary.Add("kpn", "KUAISHOU_VISION");
		dictionary.Add("ktrace-context", "1|MS43NjQ1ODM2OTgyODY2OTgyLjk3MTY3NTY2LjE2ODEzNjMzMTAxMDkuNDA1OTI0|MS43NjQ1ODM2OTgyODY2OTgyLjMyODEzODM4LjE2ODEzNjMzMTAxMDkuNDA1OTI1|0|graphql-server|webservice|false|NA");
		for (int i = startPage; i <= endPage; i++)
		{
			kSVideoSearch.variables.pcursor = i.ToString() ?? "";
			CaptureVideoBLL.GetKSVideo(kSVideoSearch, dictionary);
		}
	}

	private static void DYWindow_Closed(object sender, EventArgs e)
	{
		win_dy = false;
		cj_dy = false;
		SysCommon.isDyCj = false;
	}

	private void btnDyCj_Click(object sender, RoutedEventArgs e)
	{
		if (cj_dy)
		{
			int num = DyVideoList.Items.Count - 1;
			if (num >= 0)
			{
				DyVideoList.SelectedIndex = 0;
				object itemAt = DyVideoList.Items.GetItemAt(0);
				DyVideoList.ScrollIntoView(itemAt);
			}
			txt_CJ.Text = "开始采集";
			cj_dy = false;
			SysCommon.isDyCj = false;
			return;
		}
		if (dyWindow != null && win_dy)
		{
			dyWindow.Show();
		}
		else
		{
			AddDyWindow();
			dyWindow.Show();
		}
		string text = dy_keyword.Text;
		if (string.IsNullOrEmpty(text))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "搜索关键词不能为空,请重新输入", "采集失败", MessageBoxButton.OK);
		}
		else if (isDyCj())
		{
			GetDyBrowserCookie();
			dy_browser.Load("https://www.douyin.com/search/" + text + "?publish_time=0&sort_type=0&source=switch_tab&type=video");
			txt_CJ.Text = "停止采集";
			cj_dy = true;
			SysCommon.isDyCj = true;
			Task task = Task.Run((Action)CaijiDyVideo);
		}
		else
		{
			SysMessage.MessageShow(Window.GetWindow(this), "未检测到抖音登录状态，请先点击「抖音登录」扫码登录后再采集", "提示", MessageBoxButton.OK);
		}
	}

	private void Visitor_SendCookie(Cookie obj)
	{
		dy_cookie = dy_cookie + obj.Name + "=" + obj.Value + ";";
		CaptureVideoBLL.UpdateDyCookie("cookie", dy_cookie);
	}

	private void btnDyLogin_Click(object sender, RoutedEventArgs e)
	{
		if (dyWindow == null || !win_dy)
		{
			AddDyWindow();
			dyWindow.Show();
			return;
		}
		string text = "";
		dyWindow.Show();
		Thread.Sleep(2000);
		try
		{
			Task<JavascriptResponse> task = dy_browser.EvaluateScriptAsync("document.getElementsByTagName('html')[0].innerHTML;");
			task.Wait();
			if (task.Result.Success && task.Result.Result != null)
			{
				text = task.Result.Result.ToString();
			}
			if (text.Contains("登录后即可观看喜欢"))
			{
				SysMessage.MessageShow(Window.GetWindow(this), "抖音还没登陆，请先登录", "注意", MessageBoxButton.OK);
			}
			else
			{
				GetDyBrowserCookie();
			}
		}
		catch
		{
		}
	}

	private void btnDyClean_Click(object sender, RoutedEventArgs e)
	{
		dyList = new List<DYVideoInfo>();
		DyVideoList.ItemsSource = null;
	}

	private void btnDyDownload_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(SysCommon.VideoPath))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "保存路径没有设置，请去采集设置里设置保存路径", "注意", MessageBoxButton.OK);
			return;
		}
		if (File.Exists(SysCommon.VideoPath))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "保存路径不存在，请重新去采集设置里设置保存路径", "注意", MessageBoxButton.OK);
			return;
		}
		Button button = sender as Button;
		button.Content = "重新下载";
		DYVideoInfo info = button.DataContext as DYVideoInfo;
		DownWindow downWindow = new DownWindow(_mainWindow.Top + _mainWindow.Height / 2.0, _mainWindow.Left + _mainWindow.Width / 3.0, info);
		downWindow.ShowDialog();
	}

	private void btnDyPositive_Click(object sender, RoutedEventArgs e)
	{
		if (txt_CJ.Text == "停止采集")
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先停止采集", "注意", MessageBoxButton.OK);
			return;
		}
		List<DYVideoInfo> source = DyVideoList.ItemsSource as List<DYVideoInfo>;
		source = source.OrderBy((DYVideoInfo it) => it.DiggCount).ToList();
		for (int i = 0; i < source.Count; i++)
		{
			source[i].Id = (i + 1).ToString() ?? "";
		}
		DyVideoList.ItemsSource = null;
		DyVideoList.ItemsSource = source;
	}

	private void btnDyReverse_Click(object sender, RoutedEventArgs e)
	{
		if (txt_CJ.Text == "停止采集")
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先停止采集", "注意", MessageBoxButton.OK);
			return;
		}
		dyList = dyList.OrderByDescending((DYVideoInfo it) => it.DiggCount).ToList();
		for (int i = 0; i < dyList.Count; i++)
		{
			dyList[i].Id = (i + 1).ToString() ?? "";
		}
		DyVideoList.ItemsSource = null;
		DyVideoList.ItemsSource = dyList;
	}

	private void btnDyChoose_Click(object sender, RoutedEventArgs e)
	{
		if (txt_CJ.Text == "停止采集")
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先停止采集", "注意", MessageBoxButton.OK);
			return;
		}
		Button button = sender as Button;
		int chooseState = 0;
		if (button.Content.ToString() == "全选")
		{
			button.Content = "取消";
			chooseState = 1;
		}
		else
		{
			button.Content = "全选";
		}
		foreach (DYVideoInfo dy in dyList)
		{
			dy.ChooseState = chooseState;
		}
		DyVideoList.ItemsSource = null;
		DyVideoList.ItemsSource = dyList;
	}

	private void btnDyAllDown_Click(object sender, RoutedEventArgs e)
	{
		if (txt_CJ.Text == "停止采集")
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先停止采集", "注意", MessageBoxButton.OK);
			return;
		}
		List<DYVideoInfo> list = dyList.Where((DYVideoInfo it) => it.ChooseState == 1).ToList();
		if (list.Count == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请选中需要下载的视频", "注意", MessageBoxButton.OK);
			return;
		}
		if (string.IsNullOrEmpty(SysCommon.VideoPath))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "保存路径没有设置，请去采集设置里设置保存路径", "注意", MessageBoxButton.OK);
			return;
		}
		if (File.Exists(SysCommon.VideoPath))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "保存路径不存在，请重新去采集设置里设置保存路径", "注意", MessageBoxButton.OK);
			return;
		}
		foreach (DYVideoInfo item in list)
		{
			DownWindow downWindow = new DownWindow(_mainWindow.Top + _mainWindow.Height / 2.0, _mainWindow.Left + _mainWindow.Width / 3.0, item);
			downWindow.ShowDialog();
		}
	}

	private void CaijiDyVideo()
	{
		Thread.Sleep(3000);
		string key_word = "";
		dyList = new List<DYVideoInfo>();
		int num = 0;
		while (true)
		{
			num++;
			if (dyWindow == null || !win_dy || !cj_dy)
			{
				cj_dy = false;
				SysCommon.isDyCj = false;
				txt_CJ.Text = "开始采集";
				return;
			}
			if (num > 20 && !isDyCj())
			{
				break;
			}
			if (SysCommon.DyVideoList != null && SysCommon.DyVideoList.Count > 0)
			{
				num = 0;
				SysCommon.DyVideoList.ForEach(delegate(DYVideoInfo item)
				{
					item.KeyWord = key_word;
				});
				int i = 0;
				SysCommon.DyVideoList.ForEach(delegate(DYVideoInfo item)
				{
					item.Id = (dyList.Count() + ++i).ToString() ?? "";
				});
				dyList.AddRange(SysCommon.DyVideoList);
				SysCommon.DyVideoList = new List<DYVideoInfo>();
				DyVideoList.Dispatcher.BeginInvoke((Action)delegate
				{
					DyVideoList.ItemsSource = null;
					DyVideoList.ItemsSource = dyList;
					DyVideoList.SelectedIndex = dyList.Count() - 1;
					object itemAt = DyVideoList.Items.GetItemAt(dyList.Count() - 1);
					DyVideoList.ScrollIntoView(itemAt);
				});
			}
			dy_browser.GetMainFrame().ExecuteJavaScriptAsync("try{window.scrollBy(0,100)}catch(err){}");
			Thread.Sleep(1000);
		}
		cj_dy = false;
		SysCommon.isDyCj = false;
		txt_CJ.Text = "开始采集";
	}

	private bool isDyCj()
	{
		try
		{
			string text = "";
			Task<JavascriptResponse> task = dy_browser.EvaluateScriptAsync("document.getElementsByTagName('html')[0].innerHTML;");
			task.Wait();
			if (task.Result.Success && task.Result.Result != null)
			{
				text = task.Result.Result.ToString();
			}
			if (text.Contains("登录后即可观看喜欢、收藏的视频"))
			{
				SysMessage.MessageShow(Window.GetWindow(this), "抖音还没登陆，请先登录", "注意", MessageBoxButton.OK);
				return false;
			}
			if (text.Contains("captcha_verify_container"))
			{
				SysCommon.isDyCj = false;
				cj_dy = false;
				SysMessage.MessageShow(Window.GetWindow(this), "有滑块验证，请手动验证，并重新点击开始采集", "注意", MessageBoxButton.OK);
				return false;
			}
			if (text.Contains("服务出现异常"))
			{
				SysCommon.isDyCj = false;
				cj_dy = false;
				SysMessage.MessageShow(Window.GetWindow(this), "服务出现异常，并重新点击开始采集", "注意", MessageBoxButton.OK);
				return false;
			}
			if (text.Contains("重新刷新"))
			{
				SysCommon.isDyCj = false;
				cj_dy = false;
				SysMessage.MessageShow(Window.GetWindow(this), "重新刷新 拉取数据警告，并重新点击开始采集", "注意", MessageBoxButton.OK);
				return false;
			}
			return true;
		}
		catch (Exception e)
		{
			LogHelper.Error("抖音浏览器网页数据获取异常", e);
			return false;
		}
	}

	private void ChooseBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		string text = ((TextBox)sender).Text;
		SysConfig.UpdateConfigSetting("videoPath", text);
		SysCommon.VideoPath = text;
	}



}
