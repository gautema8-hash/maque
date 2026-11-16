using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using MDD.Models;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class NoticeWindow : Window
{
	private string url = "";






	public double TopFrom { get; set; }

	public NoticeWindow(double width, double height)
	{
		InitializeComponent();
		base.Loaded += NoticeWindow_Loaded;
	}

	private void NoticeWindow_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is NoticeInfo noticeInfo)
		{
			tbDesc.Text = noticeInfo.ShortDesc;
			tbContent.Text = noticeInfo.Content;
			tbTitle.Text = noticeInfo.Title;
			if (!string.IsNullOrEmpty(noticeInfo.Url))
			{
				url = noticeInfo.Url;
				btn_show.Visibility = Visibility.Visible;
			}
		}
		NoticeWindow self = sender as NoticeWindow;
		if (self == null)
		{
			return;
		}
		self.UpdateLayout();
		double right = SystemParameters.WorkArea.Right;
		self.Top = self.TopFrom - self.ActualHeight;
		DoubleAnimation animation = new DoubleAnimation();
		animation.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));
		animation.From = right;
		animation.To = right - self.ActualWidth - 10.0;
		self.BeginAnimation(Window.LeftProperty, animation);
		Task.Factory.StartNew(delegate
		{
			int num = 20;
			Thread.Sleep(TimeSpan.FromSeconds(num));
			base.Dispatcher.Invoke(delegate
			{
				animation = new DoubleAnimation();
				animation.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));
				animation.Completed += delegate
				{
					self.Close();
				};
				animation.From = right - self.ActualWidth;
				animation.To = right;
				self.BeginAnimation(Window.LeftProperty, animation);
			});
		});
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		double right = SystemParameters.WorkArea.Right;
		DoubleAnimation doubleAnimation = new DoubleAnimation();
		doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));
		doubleAnimation.Completed += delegate
		{
			Close();
		};
		doubleAnimation.From = right - base.ActualWidth;
		doubleAnimation.To = right;
		BeginAnimation(Window.LeftProperty, doubleAnimation);
	}

	private void btn_show_Click(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrEmpty(url))
		{
			Process.Start(url);
		}
	}


}
