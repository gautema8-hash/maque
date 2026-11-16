using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MDD.BLL;
using MDD.Common;
using MDD.Models.Enum;
using MDD.Models.Good;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class GoodManager : UserControl
{
	public MainWindow _mainWindow;

	private System.Timers.Timer timer;




	public ObservableCollection<GoodMonitor> GoodMonitorList { get; set; }

	public GoodManager()
	{
		InitializeComponent();
		initData();
	}

	private void initData()
	{
		GoodMonitorList = new ObservableCollection<GoodMonitor>();
		base.DataContext = this;
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		for (int i = 1; i < 7; i++)
		{
			dictionary.Add(i * 10, (i * 10).ToString());
		}
		box_time.ItemsSource = dictionary;
		box_time.DisplayMemberPath = "Value";
		box_time.SelectedValuePath = "Key";
		box_time.SelectedIndex = 2;
		SearchData();
	}

	private void btnSearch_Click(object sender, RoutedEventArgs e)
	{
		SearchData();
	}

	private void btnMonitor_Click_placeholder() { }

	public void SearchData()
	{
		GoodMonitorList.Clear();
		List<GoodMonitor> allGoodMonitor = GoodMonitorBLL.GetAllGoodMonitor("1");
		for (int i = 0; i < allGoodMonitor.Count; i++)
		{
			allGoodMonitor[i].Num = i + 1;
			GoodMonitorList.Add(allGoodMonitor[i]);
		}
	}

	private void btnMonitor_Click(object sender, RoutedEventArgs e)
	{
		int num = int.Parse(box_time.SelectedValue.ToString());
		timer = new System.Timers.Timer(60000 * num);
		timer.Elapsed += MonitorTimer;
		timer.AutoReset = true;
		timer.Enabled = true;
		timer.Start();
		if (SysMessage.MessageShow(Window.GetWindow(this), "正在进行商品监控，请不要进行其他操作！", "警告", MessageBoxButton.OKCancel) != MessageEnum.Error)
		{
			timer.Stop();
		}
	}

	private void MonitorTimer(object sender, ElapsedEventArgs e)
	{
		List<GoodMonitor> list = GoodMonitorBLL.GetAllGoodMonitor("1");
		base.Dispatcher.Invoke(delegate
		{
			foreach (GoodMonitor item in list)
			{
				_mainWindow.GotoUrl("https://mobile.pinduoduo.com/goods.html?goods_id=" + item.GoodId + "&force_use_web_bundle=1/");
				Thread.Sleep(10000);
			}
			SearchData();
		});
	}

	private void btnShow_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (button.DataContext is GoodMonitor goodMonitor)
		{
			GoodSalesShow goodSalesShow = new GoodSalesShow(goodMonitor.GoodId);
			goodSalesShow.ShowDialog();
		}
	}

	private void btnDelete_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (button.DataContext is GoodMonitor goodMonitor)
		{
			goodMonitor.GoodState = 2;
			GoodMonitorBLL.UpdateGoodMonitorState(goodMonitor);
			SearchData();
		}
	}

	private void btnOpen_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (button.DataContext is GoodMonitor goodMonitor)
		{
			_mainWindow.GotoUrl("https://mobile.pinduoduo.com/goods.html?goods_id=" + goodMonitor.GoodId + "&force_use_web_bundle=1/");
		}
	}



}
