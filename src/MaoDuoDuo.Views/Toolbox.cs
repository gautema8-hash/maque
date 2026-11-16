using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MDD.Models;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class Toolbox : UserControl
{
	public MainWindow _mainWindow;


	public ObservableCollection<ToolboxInfo> ToolboxList { get; set; }

	public Toolbox()
	{
		InitializeComponent();
		initData();
	}

	private void initData()
	{
		ToolboxList = new ObservableCollection<ToolboxInfo>();
		base.DataContext = this;
		ToolboxInfo toolboxInfo = new ToolboxInfo();
		toolboxInfo.Id = 1;
		toolboxInfo.ImgUrl = "/Images/Toolbox/chatgpt.jpg";
		toolboxInfo.ImgShow = Visibility.Visible;
		toolboxInfo.BtnShow = Visibility.Visible;
		toolboxInfo.Text = "ChatGpt";
		ToolboxList.Add(toolboxInfo);
		ToolboxInfo toolboxInfo2 = new ToolboxInfo();
		toolboxInfo2.Id = 2;
		toolboxInfo2.ImgUrl = "";
		toolboxInfo2.ImgShow = Visibility.Collapsed;
		toolboxInfo2.BtnShow = Visibility.Visible;
		toolboxInfo2.Text = "监控管理";
		ToolboxList.Add(toolboxInfo2);
		for (int i = 0; i < 18; i++)
		{
			ToolboxInfo toolboxInfo3 = new ToolboxInfo();
			toolboxInfo3.ImgShow = Visibility.Collapsed;
			toolboxInfo3.BtnShow = Visibility.Collapsed;
			ToolboxList.Add(toolboxInfo3);
		}
	}

	private void btnView_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (button.DataContext is ToolboxInfo { Id: not 1, Id: 2 })
		{
			_mainWindow.views_good.SearchData();
		}
	}



}
