using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MDD.BLL;

namespace MaoDuoDuo.Views;

public partial class CardManageWindow : Window
{
	public CardManageWindow()
	{
		InitializeComponent();
		RefreshList();
	}

	private void DragWindow(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void btnClose_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void btnRefresh_Click(object sender, RoutedEventArgs e)
	{
		RefreshList();
	}

	private void RefreshList()
	{
		try
		{
			DataTable dt = CardBLL.ListCards();
			List<CardRow> rows = new List<CardRow>();
			foreach (DataRow row in dt.Rows)
			{
				string unit = Convert.ToString(row["validity_unit"]) ?? "day";
				string unitText = unit == "hour" ? "小时" : unit == "month" ? "月" : unit == "year" ? "年" : "天";
				string status = Convert.ToString(row["status"]) ?? "active";
				string statusText = status == "used" ? "已使用" : status == "disabled" ? "已失效" : "未使用";
				rows.Add(new CardRow
				{
					card_no = Convert.ToString(row["card_no"]),
					validity_text = Convert.ToString(row["validity_value"]) + unitText,
					status_text = statusText,
					created_at = Convert.ToString(row["created_at"]),
					expire_at = Convert.ToString(row["expire_at"]),
					bind_user = Convert.ToString(row["bind_user"]),
					note = Convert.ToString(row["note"])
				});
			}
			dgCards.ItemsSource = rows;
		}
		catch (Exception ex)
		{
			MessageBox.Show("加载卡密列表失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void btnGenerate_Click(object sender, RoutedEventArgs e)
	{
		int value;
		if (!int.TryParse(txtValue.Text.Trim(), out value) || value <= 0)
		{
			MessageBox.Show("有效期数值必须为正整数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		string unit = "day";
		if (comboUnit.SelectedItem is ComboBoxItem item && item.Tag != null)
		{
			unit = item.Tag.ToString();
		}
		string result = CardBLL.GenerateCard(txtCardNo.Text.Trim(), value, unit, txtNote.Text.Trim());
		if (result == "true")
		{
			MessageBox.Show("卡密生成成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
			txtCardNo.Text = "";
			txtNote.Text = "";
			RefreshList();
		}
		else
		{
			MessageBox.Show("卡密生成失败：" + result, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void btnDisable_Click(object sender, RoutedEventArgs e)
	{
		CardRow selected = dgCards.SelectedItem as CardRow;
		if (selected == null)
		{
			MessageBox.Show("请先选中一行卡密", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		if (MessageBox.Show("确认作废卡密 " + selected.card_no + " ？作废后不可登录。", "确认",
			MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
		{
			return;
		}
		string result = CardBLL.DisableCard(selected.card_no);
		if (result == "true")
		{
			MessageBox.Show("已作废", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
			RefreshList();
		}
		else
		{
			MessageBox.Show("作废失败：" + result, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private class CardRow
	{
		public string card_no { get; set; }
		public string validity_text { get; set; }
		public string status_text { get; set; }
		public string created_at { get; set; }
		public string expire_at { get; set; }
		public string bind_user { get; set; }
		public string note { get; set; }
	}
}
