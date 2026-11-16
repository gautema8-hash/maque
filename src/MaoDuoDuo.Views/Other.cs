using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MDD.Models;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class Other : UserControl
{
	public List<OtherInfo> OtherList = new List<OtherInfo>();

	public List<OtherInfo> TabList = new List<OtherInfo>();



	public Other()
	{
		InitializeComponent();
		OtherList.Add(new OtherInfo
		{
			Title = "多多课堂",
			Url = SysCommon.SysUrl.DDUrl
		});
		OtherList.Add(new OtherInfo
		{
			Title = "社群服务",
			Url = SysCommon.SysUrl.SQUrl
		});
		OtherList.Add(new OtherInfo
		{
			Title = "个人中心",
			Url = SysCommon.SysUrl.GRUrl
		});
		OtherList.Add(new OtherInfo
		{
			Title = "ChartGpt",
			Url = SysCommon.SysUrl.GPTUrl
		});
	}

	public void AddTab(int index)
	{
		OtherInfo info = OtherList[index];
		List<OtherInfo> list = TabList.Where((OtherInfo it) => it.Title == info.Title).ToList();
		if (list.Count > 0)
		{
			tabControl.SelectedIndex = list[0].Index;
			return;
		}
		if (index > 0)
		{
			info.Url += SysCommon.UserInfo.Token;
		}
		info.Index = TabList.Count;
		TabList.Add(info);
		tabControl.ItemsSource = null;
		tabControl.ItemsSource = TabList;
		tabControl.SelectedIndex = info.Index;
	}


}
