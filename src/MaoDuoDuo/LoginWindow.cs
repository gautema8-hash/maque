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
		double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
		double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;
		base.Width = 820.0 * primaryScreenWidth / 1920.0;
		base.Height = 500.0 * primaryScreenHeight / 1080.0;
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
			ShowLoginError(resultInfo.Msg);
			return false;
		}
		ShowLoginError("");
		return true;
	}

	private void ShowLoginError(string msg)
	{
		if (string.IsNullOrEmpty(msg))
		{
			errTip.Visibility = Visibility.Collapsed;
			return;
		}
		errTipText.Text = "⚠ " + msg;
		errTip.Visibility = Visibility.Visible;
	}


	


}
