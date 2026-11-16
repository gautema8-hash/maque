using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MDD.Common;
using MDD.Models.Enum;
using ZdfFlatUI;

namespace DuoDuoPuHuo.Views;

public partial class Compute : Window
{













	public Compute()
	{
		InitializeComponent();
	}

	public void DragWindow(object sender, MouseButtonEventArgs args)
	{
		try
		{
			DragMove();
		}
		catch
		{
		}
	}

	private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void btnROI_Click(object sender, RoutedEventArgs e)
	{
		if (!float.TryParse(txt_xsjg.Text, out var result))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的销售价格", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_cpcb.Text, out var result2))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的产品成本", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_fwkd.Text, out var result3))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的服务扣点", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_pjyf.Text, out var result4))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的平均运费", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_zpcb.Text, out var result5))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的赠品成本", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_tkl.Text, out var result6))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的退款率", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_qita.Text, out var result7))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的其他成本", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		double num = 0.0;
		double value = (double)result / (((double)(result - result2 - result5 - result4 - result7) - num - (double)(result * result3)) * (double)(1f - result6));
		txt_roi.Text = "ROI：" + Math.Round(value, 2);
	}

	private void btnTgyk_Click(object sender, RoutedEventArgs e)
	{
		if (!float.TryParse(txt_sjhf.Text, out var result))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的实际花费", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_sjtc.Text, out var result2))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的实际投产ROI", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		if (!float.TryParse(txt_bbtc.Text, out var result3))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请输入正确的保本投产ROI", "数据异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Error);
			return;
		}
		float num = (result2 - result3) * result / result3;
		txt_tgyk.Text = "推广盈亏：" + Math.Round(num, 2);
	}


}
