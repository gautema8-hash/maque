using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MDD.Common;
using MDD.Models;
using MDD.Models.Enum;
using Newtonsoft.Json;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class ProductShow : Window
{
	private ProductInfo product;














	public ProductShow()
	{
		InitializeComponent();
	}

	public ProductShow(ProductInfo productInfo)
	{
		InitializeComponent();
		product = productInfo;
		Init();
	}

	private void Init()
	{
		try
		{
			if (product == null)
			{
				return;
			}
			txtTitle.Text = product.goodsName;
			txtID.Text = product.goodsID;
			string path = AppDomain.CurrentDomain.BaseDirectory + "cate.json";
			using (FileStream fileStream = new FileStream(path, FileMode.Open))
			{
				int num = (int)fileStream.Length;
				byte[] array = new byte[num];
				int num2 = fileStream.Read(array, 0, num);
				fileStream.Close();
				string @string = Encoding.UTF8.GetString(array, 0, num);
				Dictionary<string, ProductCatInfo> source = JsonConvert.DeserializeObject<Dictionary<string, ProductCatInfo>>(@string);
				List<string> list = new List<string>();
				if (!string.IsNullOrEmpty(product.catID1) && product.catID1 != "0")
				{
					list.Add(source.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == product.catID1).Value.cat_name);
				}
				if (!string.IsNullOrEmpty(product.catID2) && product.catID2 != "0")
				{
					ProductCatInfo value = source.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == product.catID2).Value;
					if (value != null)
					{
						list.Add(value.cat_name);
					}
				}
				if (!string.IsNullOrEmpty(product.catID3) && product.catID3 != "0")
				{
					ProductCatInfo value2 = source.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == product.catID3).Value;
					if (value2 != null)
					{
						list.Add(value2.cat_name);
					}
				}
				if (!string.IsNullOrEmpty(product.catID4) && product.catID4 != "0")
				{
					ProductCatInfo value3 = source.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == product.catID4).Value;
					if (value3 != null)
					{
						list.Add(value3.cat_name);
					}
				}
				sbCat.ItemsSource = list;
			}
			string text = "";
			text += product.sideSalesTip;
			text = text + " \r\n " + product.reviewNumText;
			text = text + " \r\n【单买价】：" + product.minNormalPrice;
			text = text + " \r\n【卷前价】：" + product.minGroupPrice;
			text = text + " \r\n【团购价】：" + product.sectionPrice;
			txtSaledInfo.Text = text;
			txtMallName.Text = product.mallName;
			txtMallDSR.Text = "描述：" + product.descScore + "  物流：" + product.logisticsScore + "  服务：" + product.serviceScore + "  ";
			txtMallSaledInfo.Text = "销售：" + product.salesTip + " \r\n商品数量：" + product.goodsNum;
		}
		catch (Exception)
		{
		}
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

	private void btnCopyTitle_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(product.goodsName);
			SysMessage.MessageShow(Window.GetWindow(this), "复制成功！", "提示：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
		}
		catch (Exception)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "复制失败！", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
		}
	}

	private void btnCopyID_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(product.goodsID);
			SysMessage.MessageShow(Window.GetWindow(this), "复制成功！", "提示：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
		}
		catch (Exception)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "复制失败！", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
		}
	}

	private void btnCopyMallName_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(product.mallName);
			SysMessage.MessageShow(Window.GetWindow(this), "复制成功！", "提示：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
		}
		catch (Exception)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "复制失败！", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
		}
	}

	private void btnCopyMallID_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(product.mallID);
			SysMessage.MessageShow(Window.GetWindow(this), "复制成功！", "提示：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
		}
		catch (Exception)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "复制失败！", "警告：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
		}
	}


}
