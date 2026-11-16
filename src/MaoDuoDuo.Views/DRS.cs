using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MDD.Common;
using MDD.Models;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class DRS : UserControl
{









	public ObservableCollection<ProductionInfo> ProductionInfoList { get; set; }

	public DRS()
	{
		InitializeComponent();
		initData();
	}

	private void initData()
	{
		base.DataContext = this;
		ProductionInfoList = new ObservableCollection<ProductionInfo>();
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		dictionary.Add(1, "单SKU");
		dictionary.Add(2, "多SKU");
		box_type.ItemsSource = dictionary;
		box_type.DisplayMemberPath = "Value";
		box_type.SelectedValuePath = "Key";
		box_type.SelectedIndex = 0;
	}

	private void btnClean_Click(object sender, RoutedEventArgs e)
	{
		ProductionInfoList.Clear();
	}

	private void btAdd_Click(object sender, RoutedEventArgs e)
	{
		double result = 0.0;
		double result2 = 0.0;
		double.TryParse(txt_cost.Text, out result);
		double.TryParse(txt_price.Text, out result2);
		if (result <= 0.0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "产品成本不能为0", "注意", MessageBoxButton.OK);
			return;
		}
		if (result2 <= 0.0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "产品售价不能为0", "注意", MessageBoxButton.OK);
			return;
		}
		if (result2 <= result)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "产品售价不能小于等于成本", "注意", MessageBoxButton.OK);
			return;
		}
		int num = Convert.ToInt32(box_type.SelectedValue);
		ProductionInfo productionInfo = new ProductionInfo();
		if (ProductionInfoList.Count > 0)
		{
			productionInfo.Id = ProductionInfoList[ProductionInfoList.Count - 1].Id + 1;
		}
		else
		{
			productionInfo.Id = 1;
		}
		productionInfo.ProductType = box_type.Text;
		productionInfo.ProductCost = result.ToString() ?? "";
		productionInfo.ProductPrice = result2.ToString() ?? "";
		double num2 = result2 - result;
		double num3 = num2 / result2 * 100.0;
		double num4 = result2 / num2;
		productionInfo.ProductProfit = num2.ToString("0.00");
		productionInfo.ProductGPM = num3.ToString("0.00") + "%";
		productionInfo.ProductNum = num4.ToString("0.00");
		if (num == 1 && ProductionInfoList.Count > 0)
		{
			ProductionInfoList[0] = productionInfo;
		}
		else
		{
			ProductionInfoList.Add(productionInfo);
		}
	}

	private void btnDelete_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		ProductionInfo productionInfo = button.DataContext as ProductionInfo;
		for (int i = 0; i < ProductionInfoList.Count; i++)
		{
			if (ProductionInfoList[i].Id == productionInfo.Id)
			{
				ProductionInfoList.RemoveAt(i);
				break;
			}
		}
	}

	private void text_wxChanged(object sender, TextChangedEventArgs e)
	{
		try
		{
			double result = 0.0;
			int result2 = 0;
			double result3 = 0.0;
			if (txt_dsr1 != null)
			{
				double.TryParse(txt_dsr1.Text, out result);
			}
			if (txt_comment != null)
			{
				int.TryParse(txt_comment.Text, out result2);
			}
			if (txt_dsr2 != null)
			{
				double.TryParse(txt_dsr2.Text, out result3);
			}
			double num = (result3 - result) * (double)result2;
			num /= 5.0 - result3;
			if (num > 0.0 && txt_wxNum != null)
			{
				txt_wxNum.Text = (Math.Floor(num) + 1.0).ToString() ?? "";
			}
		}
		catch
		{
		}
	}



}
