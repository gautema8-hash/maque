using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using AutoUpdaterDotNET;
using DuoDuoPuHuo;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using Newtonsoft.Json.Linq;
using ZdfFlatUI;
using log4net.Config;

namespace MaoDuoDuo;

public partial class LoginWindow : Window
{
	private const uint WM_SETTEXTWM_SETTEXT = 12u;

	private const uint WM_COMMAND = 273u;

	private const int BN_CLICKED = 0;

	private const uint WM_CHAR = 258u;

	private int update_state = 0;









	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lpszClass, string lpszWindow);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool SetWindowText(IntPtr hWnd, string lpString);

	[DllImport("user32.dll")]
	private static extern int SendMessageA(IntPtr hwnd, int wMsg, IntPtr wParam, string lParam);

	public LoginWindow()
	{
		base.Visibility = Visibility.Hidden;
		Task.Run((Func<Task>)CheckForUpdatesAsync);
		DateTime now = DateTime.Now;
		if (now > DateTime.MaxValue)
		{
			return;
		}
		while (update_state != 1)
		{
			if (update_state == 2)
			{
				return;
			}
			Thread.Sleep(1000);
		}
		string path = AppDomain.CurrentDomain.BaseDirectory + "\\setting.json";
		if (File.Exists(path))
		{
			string json = File.ReadAllText(path);
			JObject jObject = JObject.Parse(json);
			SysCommon.SysUrl.SetSource = jObject["source"].ToString();
			if (SysCommon.SysUrl.SetSource == "1")
			{
				SysCommon.SysUrl.source = "1";
				SysCommon.SysUrl.ss = "1";
			}
		}
		bool flag = false;
		XmlConfigurator.Configure();
		LogHelper.Clear();
		try
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string fullName = Directory.GetParent(baseDirectory).FullName;
			string fullName2 = Directory.GetParent(fullName).FullName;
			string fullName3 = Directory.GetParent(fullName2).FullName;
			string path2 = "user.json";
			string path3 = Path.Combine(fullName3, path2);
			if (File.Exists(path3))
			{
				Application.Current.Dispatcher.Invoke(delegate
				{
					Hide();
				});
				string json2 = File.ReadAllText(path3);
				JObject jObject2 = JObject.Parse(json2);
				string username = jObject2["username"].ToString();
				string password = jObject2["password"].ToString();
				if (Login(new LoginInfo
				{
					Username = username,
					Password = password
				}, isShow: true))
				{
					flag = true;
				}
				else
				{
					Close();
					Environment.Exit(0);
				}
			}
		}
		catch (Exception)
		{
		}
		InitializeComponent();
		if (SysCommon.SysUrl.SetSource == "1")
		{
			btn_fangwen.Visibility = Visibility.Hidden;
		}
		double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
		double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;
		base.Width = 820.0 * primaryScreenWidth / 1920.0;
		base.Height = 500.0 * primaryScreenHeight / 1080.0;
		string uriString = AppDomain.CurrentDomain.BaseDirectory + "/Images/logo.png";
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.UriSource = new Uri(uriString, UriKind.Absolute);
		bitmapImage.EndInit();
		img_logo.ImageSource = bitmapImage;
		initData();
		// 自测开关：命令行携带 --autotest 时自动填入账号密码并登录（用于自动化冒烟测试）
		if (Environment.GetCommandLineArgs().Any((string a) => a.Contains("autotest")))
		{
			textAccount.Text = "autotest";
			textPwd.Password = "autotest123";
			base.Dispatcher.BeginInvoke(new Action(delegate
			{
				btnLogin_Click(null, null);
			}), System.Windows.Threading.DispatcherPriority.Background);
		}
		if (flag)
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
		}
		SysCommon.isSuofang = WinController.GetWinDPI();
	}

	private void ceshi()
	{
	}

	private void initData()
	{
		string configString = SysConfig.GetConfigString("isRemember");
		if (configString == "true")
		{
			btnCheck.IsChecked = true;
			textAccount.Text = SysConfig.GetConfigString("username");
			textPwd.Password = SysConfig.GetConfigString("password");
		}
	}

	private async Task CheckForUpdatesAsync()
	{
		// 本地重建版：跳过在线更新检查
		update_state = 1;
		await Task.CompletedTask;
		base.Dispatcher.Invoke(delegate
		{
			base.Visibility = Visibility.Visible;
		});
	}

	private static int CompareVersions(string version1, string version2)
	{
		string[] array = version1.Split('.');
		string[] array2 = version2.Split('.');
		int num = Math.Max(array.Length, array2.Length);
		for (int i = 0; i < num; i++)
		{
			int num2 = ((i < array.Length) ? int.Parse(array[i]) : 0);
			int num3 = ((i < array2.Length) ? int.Parse(array2[i]) : 0);
			if (num2 > num3)
			{
				return 1;
			}
			if (num2 < num3)
			{
				return -1;
			}
		}
		return 0;
	}

	public void DragWindow(object sender, MouseButtonEventArgs args)
	{
		DragMove();
	}

	public void CloseWindow(object sender, RoutedEventArgs args)
	{
		Close();
		Environment.Exit(0);
	}

	private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	public void MinWindow(object sender, RoutedEventArgs args)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void btnLogin_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(textAccount.Text))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "账号不能为空", "登录失败", MessageBoxButton.OK);
			return;
		}
		if (string.IsNullOrEmpty(textPwd.Password))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "密码不能为空", "登录失败", MessageBoxButton.OK);
			return;
		}
		LoginInfo loginInfo = new LoginInfo();
		loginInfo.Username = textAccount.Text;
		loginInfo.Password = textPwd.Password;
		if (Login(loginInfo))
		{
			if (Convert.ToBoolean(btnCheck.IsChecked))
			{
				SysConfig.UpdateConfigSetting("username", textAccount.Text);
				SysConfig.UpdateConfigSetting("password", textPwd.Password);
				SysConfig.UpdateConfigSetting("isRemember", "true");
			}
			else
			{
				SysConfig.UpdateConfigSetting("username", "");
				SysConfig.UpdateConfigSetting("password", "");
				SysConfig.UpdateConfigSetting("isRemember", "false");
			}
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			Close();
		}
	}

	private bool Login(LoginInfo info, bool isShow = false)
	{
		var (resultInfo, userInfo) = LoginBLL.Login(info);
		if (resultInfo.Status != 200)
		{
			if (isShow)
			{
				base.WindowState = WindowState.Minimized;
			}
			SysMessage.MessageShow(Window.GetWindow(this), resultInfo.Msg, "登录失败", MessageBoxButton.OK);
			return false;
		}
		if (!userInfo.Vip)
		{
			if (isShow)
			{
				base.WindowState = WindowState.Minimized;
			}
			SysMessage.MessageShow(Window.GetWindow(this), "会员已到期，请重新付费1", "会员已到期", MessageBoxButton.OK);
			return false;
		}
		DateTime dateTime = DateTime.Parse(userInfo.ExpirationDate);
		if (dateTime < DateTime.Now)
		{
			if (isShow)
			{
				base.WindowState = WindowState.Minimized;
			}
			SysMessage.MessageShow(Window.GetWindow(this), "会员已到期，请重新付费2", "会员已到期", MessageBoxButton.OK);
			return false;
		}
		return true;
	}

	private void btnPay_Click(object sender, RoutedEventArgs e)
	{
		Process.Start(SysCommon.SysUrl.WebsiteHost);
	}

	private void btnPass_Click(object sender, RoutedEventArgs e)
	{
		switch (SysCommon.SysUrl.SetSource)
		{
		case "4":
			SysCommon.SysUrl.UserUrl = "https://yinbao.catduoduo.com/#/forgetPass";
			break;
		case "5":
			SysCommon.SysUrl.UserUrl = "https://mi.catduoduo.com/#/forgetPass";
			break;
		case "6":
			SysCommon.SysUrl.UserUrl = "https://long.catduoduo.com/#/forgetPass";
			break;
		case "7":
			SysCommon.SysUrl.UserUrl = "https://huang.catduoduo.com/#/forgetPass";
			break;
		case "8":
			SysCommon.SysUrl.UserUrl = "https://leida.catduoduo.com/#/forgetPass";
			break;
		case "9":
			SysCommon.SysUrl.UserUrl = "https://jingling.catduoduo.com/#/forgetPass";
			break;
		case "10":
			SysCommon.SysUrl.UserUrl = "https://qianduoduo.catduoduo.com/#/forgetPass";
			break;
		case "11":
			SysCommon.SysUrl.UserUrl = "https://juxiang.catduoduo.com/#/forgetPass";
			break;
		case "12":
			SysCommon.SysUrl.UserUrl = "https://qiang.catduoduo.com/#/forgetPass";
			break;
		case "13":
			SysCommon.SysUrl.UserUrl = "https://duozhentan.catduoduo.com/#/forgetPass";
			break;
		case "14":
			SysCommon.SysUrl.UserUrl = "https://sanguowei.catduoduo.com/#/forgetPass";
			break;
		case "15":
			SysCommon.SysUrl.UserUrl = "https://yizhou.catduoduo.com/#/forgetPass";
			break;
		case "16":
			SysCommon.SysUrl.UserUrl = "https://zhiduoduo.catduoduo.com/#/forgetPass";
			break;
		case "17":
			SysCommon.SysUrl.UserUrl = "https://wanli.catduoduo.com/#/forgetPass";
			break;
		case "18":
			SysCommon.SysUrl.UserUrl = "https://duoduobao.catduoduo.com/#/forgetPass";
			break;
		case "19":
			SysCommon.SysUrl.UserUrl = "https://gongjuxiang.catduoduo.com/#/forgetPass";
			break;
		case "20":
			SysCommon.SysUrl.UserUrl = "https://maodouduo.catduoduo.com/#/forgetPass";
			break;
		case "21":
			SysCommon.SysUrl.UserUrl = "https://mingzhentan.catduoduo.com/#/forgetPass";
			break;
		case "22":
			SysCommon.SysUrl.UserUrl = "https://songshi.catduoduo.com/#/forgetPass";
			break;
		case "23":
			SysCommon.SysUrl.UserUrl = "https://yiduoduo.catduoduo.com/#/forgetPass";
			break;
		case "24":
			SysCommon.SysUrl.UserUrl = "https://xiaowu.catduoduo.com/#/forgetPass";
			break;
		case "25":
			SysCommon.SysUrl.UserUrl = "https://xiduoduo.catduoduo.com/#/forgetPass";
			break;
		case "26":
			SysCommon.SysUrl.UserUrl = "https://zhonggetong.catduoduo.com/#/forgetPass";
			break;
		case "27":
			SysCommon.SysUrl.UserUrl = "https://xiaoer.catduoduo.com/#/forgetPass";
			break;
		case "28":
			SysCommon.SysUrl.UserUrl = "https://jingpin.catduoduo.com/#/forgetPass";
			break;
		case "29":
			SysCommon.SysUrl.UserUrl = "https://hangqing.catduoduo.com/#/forgetPass";
			break;
		case "30":
			SysCommon.SysUrl.UserUrl = "https://longyi.catduoduo.com/#/forgetPass";
			break;
		}
		Process.Start(SysCommon.SysUrl.UserUrl);
	}

	private void btnCallon_Click(object sender, RoutedEventArgs e)
	{
		switch (SysCommon.SysUrl.SetSource)
		{
		case "4":
			SysCommon.SysUrl.WebsiteHost = "https://yinbao.catduoduo.com";
			break;
		case "5":
			SysCommon.SysUrl.WebsiteHost = "https://mi.catduoduo.com";
			break;
		case "6":
			SysCommon.SysUrl.WebsiteHost = "https://long.catduoduo.com";
			break;
		case "7":
			SysCommon.SysUrl.WebsiteHost = "https://huang.catduoduo.com";
			break;
		case "8":
			SysCommon.SysUrl.WebsiteHost = "https://leida.catduoduo.com";
			break;
		case "9":
			SysCommon.SysUrl.WebsiteHost = "https://jingling.catduoduo.com";
			break;
		case "10":
			SysCommon.SysUrl.WebsiteHost = "https://qianduoduo.catduoduo.com";
			break;
		case "11":
			SysCommon.SysUrl.WebsiteHost = "https://juxiang.catduoduo.com";
			break;
		case "12":
			SysCommon.SysUrl.WebsiteHost = "https://qiang.catduoduo.com";
			break;
		case "13":
			SysCommon.SysUrl.WebsiteHost = "https://duozhentan.catduoduo.com";
			break;
		case "14":
			SysCommon.SysUrl.WebsiteHost = "https://sanguowei.catduoduo.com";
			break;
		case "15":
			SysCommon.SysUrl.WebsiteHost = "https://yizhou.catduoduo.com";
			break;
		case "16":
			SysCommon.SysUrl.WebsiteHost = "https://zhiduoduo.catduoduo.com";
			break;
		case "17":
			SysCommon.SysUrl.WebsiteHost = "https://wanli.catduoduo.com";
			break;
		case "18":
			SysCommon.SysUrl.WebsiteHost = "https://duoduobao.catduoduo.com";
			break;
		case "19":
			SysCommon.SysUrl.WebsiteHost = "https://gongjuxiang.catduoduo.com";
			break;
		case "20":
			SysCommon.SysUrl.WebsiteHost = "https://maodouduo.catduoduo.com";
			break;
		case "21":
			SysCommon.SysUrl.WebsiteHost = "https://mingzhentan.catduoduo.com";
			break;
		case "22":
			SysCommon.SysUrl.WebsiteHost = "https://songshi.catduoduo.com";
			break;
		case "23":
			SysCommon.SysUrl.WebsiteHost = "https://yiduoduo.catduoduo.com";
			break;
		case "24":
			SysCommon.SysUrl.WebsiteHost = "https://xiaowu.catduoduo.com";
			break;
		case "25":
			SysCommon.SysUrl.WebsiteHost = "https://xiduoduo.catduoduo.com";
			break;
		case "26":
			SysCommon.SysUrl.WebsiteHost = "https://zhonggetong.catduoduo.com";
			break;
		case "27":
			SysCommon.SysUrl.WebsiteHost = "https://xiaoer.catduoduo.com";
			break;
		case "28":
			SysCommon.SysUrl.WebsiteHost = "https://jingpin.catduoduo.com";
			break;
		case "29":
			SysCommon.SysUrl.WebsiteHost = "https://hangqing.catduoduo.com";
			break;
		case "30":
			SysCommon.SysUrl.WebsiteHost = "https://longyi.catduoduo.com";
			break;
		}
		Process.Start(SysCommon.SysUrl.WebsiteHost);
	}


}
