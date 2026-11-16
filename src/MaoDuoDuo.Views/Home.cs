using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Navigation;
using LiveCharts;
using LiveCharts.Wpf;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using Newtonsoft.Json.Linq;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class Home : UserControl
{
	public MainWindow _mainWindow;










	public ObservableCollection<DirectEntryInfo> DirectEntryInfoList { get; set; }

	public ObservableCollection<string> HotSearchInfoList { get; set; }

	public Home()
	{
		InitializeComponent();
		Loaded += (s, e) => { if (_mainWindow == null) _mainWindow = Window.GetWindow(this) as MainWindow; };
		DirectEntryInfoList = new ObservableCollection<DirectEntryInfo>();
		HotSearchInfoList = new ObservableCollection<string>();
		base.DataContext = this;
		SearchHomeData();
	}

	private void SearchHomeData()
	{
		ResultInfo homeInfo = HomeBLL.GetHomeInfo();
		if (homeInfo.Code != 200)
		{
			SysMessage.MessageShow(Window.GetWindow(this), homeInfo.Msg, "获取数据失败", MessageBoxButton.OK);
			return;
		}
		DirectEntryInfoList.Clear();
		HotSearchInfoList.Clear();
		foreach (DirectEntryInfo item2 in SysCommon.ConfigInfo.DirectEntry)
		{
			DirectEntryInfoList.Add(item2);
		}
		string[] hotSearch = SysCommon.ConfigInfo.HotSearch;
		foreach (string item in hotSearch)
		{
			HotSearchInfoList.Add(item);
		}
		JToken marketData = SysCommon.ConfigInfo.marketData as JToken;
		ShowChart(marketData);
	}

	private void ShowChart(JToken marketData)
	{
		List<string> list = new List<string>();
		ChartValues<double> chartValues = new ChartValues<double>();
		ChartValues<double> chartValues2 = new ChartValues<double>();
		foreach (JToken item in marketData.Values())
		{
			list.Add(item.Path);
			chartValues.Add(Convert.ToDouble(item["diff"]));
			chartValues2.Add(Convert.ToDouble(item["today"]));
		}
		cartesianChart1.Series = new SeriesCollection
		{
			new ColumnSeries
			{
				Title = "差异",
				Values = chartValues,
				MaxColumnWidth = 25.0
			},
			new ColumnSeries
			{
				Title = "今日",
				Values = chartValues2,
				MaxColumnWidth = 25.0
			}
		};
		cartesianChart1.AxisX.Add(new Axis
		{
			Title = "日期",
			Labels = list
		});
		cartesianChart1.LegendLocation = LegendLocation.Top;
	}

	private void BtnGoodSearch_Click(object sender, RoutedEventArgs e)
	{
		string url = "https://mobile.yangkeduo.com/search_result.html?search_key=" + txtGoodsKey.Text + "&search_type=goods";
		_mainWindow.GotoUrl(url);
	}

	private void BtnStoreSearch_Click(object sender, RoutedEventArgs e)
	{
		string url = "https://mobile.yangkeduo.com/search_result.html?search_key=" + txtStoreKey.Text + "&search_type=mall";
		_mainWindow.GotoUrl(url);
	}

	private void BtnDirectEntry_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		_mainWindow.GotoUrl(button.Tag.ToString());
	}

	private void BtnHotSearch_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		TabSearch.SelectedIndex = 0;
		txtGoodsKey.Text = button.Content.ToString();
		BtnGoodSearch_Click(null, null);
	}

	private void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
	{
	}



}
