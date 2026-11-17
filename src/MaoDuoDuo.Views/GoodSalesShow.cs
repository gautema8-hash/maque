using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using LiveCharts;
using LiveCharts.Wpf;
using MDD.BLL;
using MDD.Models.Good;

namespace MaoDuoDuo.Views;

public partial class GoodSalesShow : Window
{


	public GoodSalesShow(string goodId)
	{
		InitializeComponent();
		SerachData(goodId);
	}

	private void SerachData(string goodId)
	{
		List<GoodMonitorInfo> allGoodMonitorInfo = GoodMonitorBLL.GetAllGoodMonitorInfo(goodId);
		allGoodMonitorInfo = allGoodMonitorInfo.Where((GoodMonitorInfo it) => DateTime.Parse(it.CreateTime) > DateTime.Now.AddDays(-8.0)).ToList();
		List<string> list = new List<string>();
		ChartValues<int> chartValues = new ChartValues<int>();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < 8; i++)
		{
			string time = DateTime.Now.AddDays(-8 + i).ToString("yyyy-MM-dd");
			GoodMonitorInfo goodMonitorInfo = allGoodMonitorInfo.Where((GoodMonitorInfo it) => it.CreateTime == time).FirstOrDefault();
			if (goodMonitorInfo != null)
			{
				if (i == 0)
				{
					num = goodMonitorInfo.GoodNum;
					break;
				}
				if (num2 > 0)
				{
					int item = (goodMonitorInfo.GoodNum - num) / num2;
					for (int j = 0; j < num2 + 1; j++)
					{
						chartValues.Add(item);
					}
				}
				else
				{
					chartValues.Add(goodMonitorInfo.GoodNum - num);
				}
				num = goodMonitorInfo.GoodNum;
				num2 = 0;
			}
			else if (i > 0)
			{
				num2++;
			}
			if (i > 0)
			{
				list.Add(time);
			}
		}
		if (num2 > 0)
		{
			for (int k = 0; k < num2; k++)
			{
				if (k == num2 - 1 && num > 0)
				{
					chartValues.Add(num);
				}
				else
				{
					chartValues.Add(0);
				}
			}
		}
		cartesianChart1.Series = new SeriesCollection
		{
			new ColumnSeries
			{
				Title = "销量",
				Values = chartValues,
				MaxColumnWidth = 25.0
			}
		};
		cartesianChart1.AxisX.Add(new Axis
		{
			Title = "日期",
			Labels = list
		});
		cartesianChart1.LegendLocation = LegendLocation.None;
	}

	private void CloseWindow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		Close();
	}

	private void DragWindow(object sender, RoutedEventArgs e)
	{
		try
		{
			DragMove();
		}
		catch
		{
		}
	}


}
