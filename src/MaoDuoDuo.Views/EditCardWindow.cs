using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaoDuoDuo.Views;

public partial class EditCardWindow : Window
{
	public string CardNo { get; private set; }
	public int ValidityValue { get; private set; }
	public string ValidityUnit { get; private set; }
	public string Status { get; private set; }
	public string Note { get; private set; }

	public EditCardWindow(string cardNo, string validityText, string note, string statusText)
	{
		InitializeComponent();
		CardNo = cardNo;
		txtCardNo.Text = cardNo;
		txtNote.Text = note ?? "";

		// 解析 "1天" / "24小时" 等
		int value = 1;
		string unit = "day";
		if (!string.IsNullOrEmpty(validityText))
		{
			string v = validityText.Replace("小时", "").Replace("天", "").Replace("月", "").Replace("年", "").Trim();
			int.TryParse(v, out value);
			if (validityText.Contains("小时")) unit = "hour";
			else if (validityText.Contains("月")) unit = "month";
			else if (validityText.Contains("年")) unit = "year";
			else unit = "day";
		}
		txtValue.Text = value.ToString();
		for (int i = 0; i < comboUnit.Items.Count; i++)
		{
			if ((comboUnit.Items[i] as ComboBoxItem)?.Tag?.ToString() == unit) { comboUnit.SelectedIndex = i; break; }
		}
		for (int i = 0; i < comboStatus.Items.Count; i++)
		{
			string tag = (comboStatus.Items[i] as ComboBoxItem)?.Tag?.ToString();
			if ((statusText == "已失效" && tag == "disabled") ||
				(statusText == "已使用" && tag == "used") ||
				(statusText == "未使用" && tag == "active") ||
				(statusText == "过期" && tag == "active"))
			{ comboStatus.SelectedIndex = i; break; }
		}
	}

	private void DragWindow(object sender, MouseButtonEventArgs e) { DragMove(); }
	private void CloseWindow_Click(object sender, MouseButtonEventArgs e) { DialogResult = false; Close(); }
	private void btnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

	private void btnSave_Click(object sender, RoutedEventArgs e)
	{
		if (!int.TryParse(txtValue.Text.Trim(), out int value) || value <= 0)
		{
			MessageBox.Show("有效期数值必须为正整数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		ValidityValue = value;
		ValidityUnit = (comboUnit.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "day";
		Status = (comboStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "active";
		Note = (txtNote.Text ?? "").Trim();
		DialogResult = true;
		Close();
	}
}
