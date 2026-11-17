using System;
using System.Windows;
using System.Windows.Input;
using MDD.Common;
using MDD.Models;

namespace MaoDuoDuo.Views;

public partial class PersonalInfoWindow : Window
{
	public PersonalInfoWindow()
	{
		InitializeComponent();
		LoadInfo();
	}

	private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void LoadInfo()
	{
		UserInfo info = SysCommon.UserInfo ?? new UserInfo();
		string account = string.IsNullOrEmpty(SysCommon.Username) ? info.Username : SysCommon.Username;
		if (string.IsNullOrEmpty(account)) account = "local";
		txtAccount.Text = account;
		txtInitial.Text = account.Substring(0, 1).ToUpperInvariant();

		string role = SysCommon.UserRole ?? "user";
		string roleText = role == "admin" ? "超级管理员" : role == "card" ? "卡密用户" : "普通用户";
		txtRole.Text = roleText;

		string method = SysCommon.LoginMethod ?? "password";
		txtLoginMethod.Text = method == "card" ? "卡密号登录" : "账号密码登录";
		txtLoginTime.Text = string.IsNullOrEmpty(info.LocalTime) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : info.LocalTime;
		txtExpire.Text = string.IsNullOrEmpty(info.ExpirationDate) ? "长期有效" : info.ExpirationDate;
		txtCardNo.Text = string.IsNullOrEmpty(SysCommon.CardNo) ? "—" : SysCommon.CardNo;
		txtDesc.Text = string.IsNullOrEmpty(info.MemberDescription) ? "麻雀一键铺货 本地授权版" : info.MemberDescription;
	}

	private void btnSwitch_Click(object sender, RoutedEventArgs e)
	{
		SwitchAccount();
	}

	private void btnLogout_Click(object sender, RoutedEventArgs e)
	{
		Logout();
	}

	private void ClearSavedCredentials()
	{
		SysConfig.UpdateConfigSetting("isRemember", "false");
		SysConfig.UpdateConfigSetting("username", "");
		SysConfig.UpdateConfigSetting("password", "");
	}

	private void SwitchAccount()
	{
		// 切换账号：保留"记住密码"偏好，但清除已保存凭据以便重新输入
		SysConfig.UpdateConfigSetting("username", "");
		SysConfig.UpdateConfigSetting("password", "");
		SysConfig.UpdateConfigSetting("isRemember", "false");
		SysCommon.UserInfo = null;
		SysCommon.Username = "";
		SysCommon.Password = "";
		SysCommon.LoginMethod = "password";
		SysCommon.CardNo = "";
		Close();
		ReturnToLogin();
	}

	private void Logout()
	{
		ClearSavedCredentials();
		SysCommon.UserInfo = null;
		SysCommon.Username = "";
		SysCommon.Password = "";
		SysCommon.LoginMethod = "password";
		SysCommon.CardNo = "";
		Close();
		ReturnToLogin();
	}

	private void ReturnToLogin()
	{
		// 先显示登录窗，再关闭其它窗口，避免最后一个窗口关闭触发应用退出
		LoginWindow login = new LoginWindow();
		login.Show();
		Window[] windows = new Window[System.Windows.Application.Current.Windows.Count];
		System.Windows.Application.Current.Windows.CopyTo(windows, 0);
		foreach (Window window in windows)
		{
			if (window != login && (window is MainWindow || window is PersonalInfoWindow || window is CardManageWindow))
			{
				try { window.Close(); } catch { }
			}
		}
	}
}
