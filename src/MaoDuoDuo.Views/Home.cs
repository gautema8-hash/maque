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

	public void SetSearchKeywords(string goods, string store)
	{
		try
		{
			txtGoodsKey.GetType().GetProperty("Text")?.SetValue(txtGoodsKey, goods);
			txtStoreKey.GetType().GetProperty("Text")?.SetValue(txtStoreKey, store);
		}
		catch
		{
		}
	}

	private bool EnsureMainWindow()
	{
		if (_mainWindow == null)
		{
			_mainWindow = Window.GetWindow(this) as MainWindow;
		}
		if (_mainWindow == null)
		{
			SysMessage.MessageShow(null, "无法定位主窗口，请重新打开功能页", "提示", MessageBoxButton.OK);
			return false;
		}
		return true;
	}

	/// <summary>读取 ZTextBox 实际文本：优先控件 Text 属性，兜底读模板内部 TextBox（兼容自动化赋值场景）</summary>
	private string GetBoxText(System.Windows.Controls.Control wrapper)
	{
		string direct = null;
		try
		{
			var prop = wrapper.GetType().GetProperty("Text");
			direct = prop?.GetValue(wrapper) as string;
		}
		catch
		{
		}
		if (!string.IsNullOrWhiteSpace(direct))
		{
			return direct;
		}
		return FindInnerTextBoxText(wrapper);
	}

	private void SetInnerTextBoxText(DependencyObject root, string value)
	{
		int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
			if (child is System.Windows.Controls.TextBox tb)
			{
				tb.Text = value;
				return;
			}
			SetInnerTextBoxText(child, value);
		}
	}

	private string FindInnerTextBoxText(DependencyObject root)
	{
		int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
			if (child is System.Windows.Controls.TextBox tb)
			{
				return tb.Text ?? "";
			}
			string result = FindInnerTextBoxText(child);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}

	public void SearchAndShow(string keyword, string searchType)
	{
		DoSearch(keyword, searchType);
	}

	private void DoSearch(string keyword, string searchType)
	{
		if (string.IsNullOrWhiteSpace(keyword))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先输入搜索关键词", "提示", MessageBoxButton.OK);
			return;
		}
		if (!EnsureMainWindow())
		{
			return;
		}
		string url = "https://mobile.yangkeduo.com/search_result.html?search_key=" + Uri.EscapeDataString(keyword.Trim()) + "&search_type=" + searchType;
		_mainWindow.GotoUrl(url);
		_mainWindow.ShowBuyerPage();
	}

	private void BtnGoodSearch_Click(object sender, RoutedEventArgs e)
	{
		DoSearch(GetBoxText(txtGoodsKey), "goods");
	}

	private void BtnStoreSearch_Click(object sender, RoutedEventArgs e)
	{
		DoSearch(GetBoxText(txtStoreKey), "mall");
	}

	private void GoodsKey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Enter)
		{
			BtnGoodSearch_Click(sender, e);
		}
	}

	private void StoreKey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Enter)
		{
			BtnStoreSearch_Click(sender, e);
		}
	}

	private void BtnDirectEntry_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (button?.Tag == null || !EnsureMainWindow())
		{
			return;
		}
		_mainWindow.GotoUrl(button.Tag.ToString());
		_mainWindow.ShowBuyerPage();
	}

	private void BtnHotSearch_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		TabSearch.SelectedIndex = 0;
		try
		{
			var prop = txtGoodsKey.GetType().GetProperty("Text");
			prop?.SetValue(txtGoodsKey, button.Content.ToString());
		}
		catch
		{
		}
		SetInnerTextBoxText(txtGoodsKey, button.Content.ToString());
		BtnGoodSearch_Click(null, null);
	}

	private void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
	{
	}



}
