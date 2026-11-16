using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using MDD.Common;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class ImageSet : Window
{
	private BoundObject bound;

	private Thread threadDownload;

	private ImageViewModel viewModel;






	public ImageSet()
	{
		InitializeComponent();
	}

	public ImageSet(BoundObject bound)
	{
		InitializeComponent();
		this.bound = bound;
		Init();
	}

	private void Init()
	{
		try
		{
			viewModel = new ImageViewModel();
			base.DataContext = viewModel;
			viewModel.ImagePath = "pack://application:,,,/Images/tu/tu1.jpg";
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			dictionary.Add(1, "图片1");
			dictionary.Add(2, "图片2");
			dictionary.Add(3, "图片3");
			dictionary.Add(4, "图片4");
			dictionary.Add(5, "图片5");
			dictionary.Add(6, "图片6");
			box_bottom.ItemsSource = dictionary;
			box_bottom.DisplayMemberPath = "Value";
			box_bottom.SelectedValuePath = "Key";
			box_bottom.SelectedIndex = bound.imageInfo.boImage - 1;
			check_image1.IsChecked = bound.imageInfo.topState == 1;
			check_image2.IsChecked = bound.imageInfo.skuState == 1;
			if (bound.settingInfo.boxType == 2)
			{
				check_image1.IsEnabled = false;
				check_image2.IsEnabled = false;
			}
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

	private void bottom_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		bound.imageInfo.boImage = int.Parse(box_bottom.SelectedValue.ToString());
		viewModel.ImagePath = "pack://application:,,,/Images/tu/tu" + box_bottom.SelectedValue.ToString() + ".jpg";
	}

	private void CheckBox_Type(object sender, RoutedEventArgs e)
	{
		bound.imageInfo.topState = (check_image1.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.imageInfo.skuState = (check_image2.IsChecked.GetValueOrDefault() ? 1 : 0);
	}


}
