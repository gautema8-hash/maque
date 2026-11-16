using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MDD.Common;
using MDD.Models.Enum;
using MaoDuoDuo;
using Newtonsoft.Json;
using ZdfFlatUI;

namespace DuoDuoPuHuo.Views;

public partial class LYSet : Window
{
	private BoundObject bound;



	public LYSet()
	{
		InitializeComponent();
	}

	public LYSet(BoundObject bound)
	{
		InitializeComponent();
		this.bound = bound;
		txt_reason.Text = this.bound.setInfo.reason;
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

	private void btnSave_Click(object sender, RoutedEventArgs e)
	{
		bound.setInfo.reason = txt_reason.Text;
		string contents = JsonConvert.SerializeObject(bound.setInfo);
		string path = AppDomain.CurrentDomain.BaseDirectory + "\\setting.json";
		File.Exists(path);
		File.WriteAllText(path, contents);
		bound.reaList = bound.setInfo.reason.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
		SysMessage.MessageShow(Window.GetWindow(this), "保存已完成", "保存成功：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
		Close();
	}


}
