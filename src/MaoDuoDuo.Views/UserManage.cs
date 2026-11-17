using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MDD.BLL;
using MDD.DAL;
using MDD.Models;

namespace MaoDuoDuo.Views;

/// <summary>
/// 用户管理（卡密分配）：仅管理员可分配/编辑/删除卡密；卡密号为 32 位 UUID；
/// 列表每 5 秒实时刷新，到期时间实时显示，过期显示「过期」。
/// </summary>
public partial class UserManage : UserControl
{
	private readonly DispatcherTimer _refreshTimer;
	private bool _isAdmin;
	private string _keyword = "";

	public UserManage()
	{
		InitializeComponent();
		_isAdmin = string.Equals(SysCommon.UserRole, "admin", StringComparison.OrdinalIgnoreCase);

		// 权限控制：仅管理员可分配/作废/编辑/删除
		genPanel.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
		noPermTip.Visibility = _isAdmin ? Visibility.Collapsed : Visibility.Visible;
		noPermText.Text = "当前账号（" + (SysCommon.Username ?? "") + "，角色：" + (SysCommon.UserRole ?? "user") + "）无管理权限，仅管理员可分配/编辑/删除卡密。";

		txtSearch.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) btnSearch_Click(s, null); };
		RefreshList();
		_refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
		_refreshTimer.Tick += (s, e) => RefreshList();
		_refreshTimer.Start();
		Unloaded += (s, e) => _refreshTimer.Stop();
	}

	private void btnSearch_Click(object sender, RoutedEventArgs e)
	{
		_keyword = (txtSearch.Text ?? "").Trim();
		RefreshList();
	}

	private void btnResetSearch_Click(object sender, RoutedEventArgs e)
	{
		txtSearch.Text = "";
		_keyword = "";
		RefreshList();
	}

	private void RefreshList()
	{
		try
		{
			DataTable dt = _keyword.Length == 0 ? CardBLL.ListCards() : CardBLL.SearchCards(_keyword);
			List<CardRow> rows = new List<CardRow>();
			foreach (DataRow row in dt.Rows)
			{
				string unit = Convert.ToString(row["validity_unit"]) ?? "day";
				string unitText = unit == "hour" ? "小时" : unit == "month" ? "月" : unit == "year" ? "年" : "天";
				string statusText, remainText;
				(statusText, remainText) = CardDAL.GetCardDisplay(row);
				rows.Add(new CardRow
				{
					card_no = Convert.ToString(row["card_no"]),
					validity_text = Convert.ToString(row["validity_value"]) + unitText,
					status_text = statusText,
					remain_text = remainText,
					created_at = Convert.ToString(row["created_at"]),
					expire_at = Convert.ToString(row["expire_at"]),
					bind_user = Convert.ToString(row["bind_user"]),
					note = Convert.ToString(row["note"])
				});
			}
			dgCards.ItemsSource = rows;
			txtLastRefresh.Text = "刷新于 " + DateTime.Now.ToString("HH:mm:ss");
		}
		catch (Exception ex)
		{
			txtLastRefresh.Text = "刷新失败：" + ex.Message;
		}
	}

	private void btnGenerate_Click(object sender, RoutedEventArgs e)
	{
		if (!_isAdmin) { MessageBox.Show("仅管理员可分配卡密", "无权限", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
		int value;
		if (!int.TryParse(txtValue.Text.Trim(), out value) || value <= 0)
		{
			MessageBox.Show("有效期数值必须为正整数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		string unit = GetSelectedUnit();
		string result = CardBLL.GenerateCard("", value, unit, txtNote.Text.Trim());
		if (result == "true")
		{
			RefreshList();
			MessageBox.Show("卡密已生成（32 位 UUID），有效期 " + value + " " + unit + "。请复制列表中卡密号。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
			txtNote.Text = "";
		}
		else MessageBox.Show("卡密生成失败：" + result, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
	}

	private string GetSelectedUnit()
	{
		if (comboUnit.SelectedItem is ComboBoxItem item && item.Tag != null) return item.Tag.ToString();
		return "day";
	}

	private void btnCopy_Click(object sender, RoutedEventArgs e)
	{
		string cardNo = (sender as Button)?.Tag as string;
		if (string.IsNullOrEmpty(cardNo)) return;
		try
		{
			Clipboard.SetText(cardNo);
			MessageBox.Show("已复制卡密号：\n" + cardNo, "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show("复制失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void btnEdit_Click(object sender, RoutedEventArgs e)
	{
		if (!_isAdmin) { MessageBox.Show("仅管理员可编辑卡密", "无权限", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
		string cardNo = (sender as Button)?.Tag as string;
		CardRow row = FindRow(cardNo);
		if (row == null) return;

		EditCardWindow win = new EditCardWindow(row.card_no, row.validity_text, row.note, row.status_text);
		win.Owner = Window.GetWindow(this);
		if (win.ShowDialog() == true)
		{
			string result = CardBLL.UpdateCard(win.CardNo, win.ValidityValue, win.ValidityUnit, win.Note, win.Status);
			if (result == "true") { RefreshList(); MessageBox.Show("卡密已更新", "成功", MessageBoxButton.OK, MessageBoxImage.Information); }
			else MessageBox.Show("更新失败：" + result, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void btnDelete_Click(object sender, RoutedEventArgs e)
	{
		if (!_isAdmin) { MessageBox.Show("仅管理员可删除卡密", "无权限", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
		string cardNo = (sender as Button)?.Tag as string;
		if (string.IsNullOrEmpty(cardNo)) return;
		if (MessageBox.Show("确认【删除】卡密 " + cardNo + " ？\n删除后不可恢复，该卡密将无法登录。", "确认删除",
			MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
		string result = CardBLL.DeleteCard(cardNo);
		if (result == "true") { RefreshList(); MessageBox.Show("已删除", "成功", MessageBoxButton.OK, MessageBoxImage.Information); }
		else MessageBox.Show("删除失败：" + result, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
	}

	private CardRow FindRow(string cardNo)
	{
		List<CardRow> rows = dgCards.ItemsSource as List<CardRow>;
		if (rows == null) return null;
		return rows.Find(r => r.card_no == cardNo);
	}

	private class CardRow
	{
		public string card_no { get; set; }
		public string validity_text { get; set; }
		public string status_text { get; set; }
		public string remain_text { get; set; }
		public string created_at { get; set; }
		public string expire_at { get; set; }
		public string bind_user { get; set; }
		public string note { get; set; }
	}
}
