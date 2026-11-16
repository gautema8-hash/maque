using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class Hot : UserControl
{
	public MainWindow _mainWindow;
















	public ObservableCollection<HotShopInfo> HotShopInfoList { get; set; }

	public PageInfo PageInfo { get; set; }

	public Hot()
	{
		InitializeComponent();
		HotShopInfoList = new ObservableCollection<HotShopInfo>();
		PageInfo = new PageInfo();
		PageInfo.Px = 1;
		PageInfo.Pz = 15;
		Dictionary<string, string> itemsSource = new Dictionary<string, string>
		{
			{
				DateTime.Now.AddDays(-1.0).ToString("yyyy-MM-dd"),
				DateTime.Now.AddDays(-1.0).ToString("MM月dd日")
			},
			{
				DateTime.Now.AddDays(-2.0).ToString("yyyy-MM-dd"),
				DateTime.Now.AddDays(-2.0).ToString("MM月dd日")
			},
			{
				DateTime.Now.AddDays(-3.0).ToString("yyyy-MM-dd"),
				DateTime.Now.AddDays(-3.0).ToString("MM月dd日")
			}
		};
		cbDate.ItemsSource = itemsSource;
		cbDate.DisplayMemberPath = "Value";
		cbDate.SelectedValuePath = "Key";
		cbDate.SelectedIndex = 0;
		Dictionary<int, string> itemsSource2 = new Dictionary<int, string>
		{
			{ 1, "7天" },
			{ 2, "30天" }
		};
		cbSort.ItemsSource = itemsSource2;
		cbSort.DisplayMemberPath = "Value";
		cbSort.SelectedValuePath = "Key";
		cbSort.SelectedIndex = 0;
		base.DataContext = this;
		SearchHotProducts();
	}

	private void SearchHotProducts()
	{
		HotSearch hotSearch = new HotSearch();
		if (rbNormal.IsChecked.GetValueOrDefault())
		{
			hotSearch.OpenTime = cbDate.SelectedValue.ToString() + " 00:00:00";
			hotSearch.EndTime = cbDate.SelectedValue.ToString() + " 23:59:59";
		}
		else if (rbSort.IsChecked.GetValueOrDefault())
		{
			hotSearch.Sort = Convert.ToInt32(cbSort.SelectedValue);
		}
		hotSearch.Px = PageInfo.Px;
		hotSearch.Pz = PageInfo.Pz;
		hotSearch.TemplateGoodsName = txtKeyword.Text;
		var (resultInfo, list) = HotBLL.GetHotInfo(hotSearch);
		if (resultInfo.Code != 200)
		{
			SysMessage.MessageShow(Window.GetWindow(this), resultInfo.Msg, "获取数据失败", MessageBoxButton.OK);
			return;
		}
		HotShopInfoList.Clear();
		foreach (HotShopInfo item in list)
		{
			if (rbNormal.IsChecked.GetValueOrDefault())
			{
				item.SelectCondition = cbDate.Text;
				item.SalesNum = item.MinDaySalesNum + "-" + item.MaxDaySalesNum;
			}
			else
			{
				item.SelectCondition = cbSort.Text;
				if (cbSort.Text == "7天")
				{
					item.SalesNum = item.MinSevenSalesNum + "-" + item.MaxSevenSalesNum;
				}
				else
				{
					item.SalesNum = item.MinThirtySalesNum + "-" + item.MaxThirtySalesNum;
				}
			}
			item.ShopUrl = "https://mobile.pinduoduo.com/goods.html?goods_id=" + item.TemplateGoodsId + "&force_use_web_bundle=1/";
			HotShopInfoList.Add(item);
		}
		txtTotalCount.Text = PageInfo.NumRows.ToString();
		txtTotalPage.Text = PageInfo.NumPage.ToString();
		txtCurrentPage.Text = PageInfo.Px.ToString();
	}

	private void BtnSearch_Click(object sender, RoutedEventArgs e)
	{
		PageInfo.Px = 1;
		txtJumpPage.Text = "";
		SearchHotProducts();
	}

	private void BtnView_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (button.DataContext is HotShopInfo hotShopInfo)
		{
			_mainWindow.GotoUrl(hotShopInfo.ShopUrl);
		}
	}

	private void rbNormal_Checked(object sender, RoutedEventArgs e)
	{
		if (cbSort != null)
		{
			cbSort.IsEnabled = false;
		}
		if (cbDate != null)
		{
			cbDate.IsEnabled = true;
		}
	}

	private void rbSort_Checked(object sender, RoutedEventArgs e)
	{
		if (cbSort != null)
		{
			cbSort.IsEnabled = true;
		}
		if (cbDate != null)
		{
			cbDate.IsEnabled = false;
		}
	}

	private void btnJumpPage_Click(object sender, RoutedEventArgs e)
	{
		if (!int.TryParse(txtJumpPage.Text, out var result) || result < 1 || result > PageInfo.NumPage)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "页码输入错误", "提示", MessageBoxButton.OK);
			return;
		}
		PageInfo.Px = Convert.ToInt32(txtJumpPage.Text);
		SearchHotProducts();
	}

	private void btnPrePage_Click(object sender, RoutedEventArgs e)
	{
		if (PageInfo.Px <= 1)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "没有上一页", "提示", MessageBoxButton.OK);
			return;
		}
		PageInfo.Px--;
		SearchHotProducts();
	}

	private void btnNextPage_Click(object sender, RoutedEventArgs e)
	{
		if (PageInfo.Px >= PageInfo.NumPage)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "没有下一页", "提示", MessageBoxButton.OK);
			return;
		}
		PageInfo.Px++;
		SearchHotProducts();
	}



}
