using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using MDD.Models.Video;

namespace MaoDuoDuo.Views;

public partial class DownWindow : Window
{



	public DownWindow(double x, double y, DYVideoInfo info)
	{
		InitializeComponent();
		base.WindowStartupLocation = WindowStartupLocation.Manual;
		base.Top = x;
		base.Left = y;
		downFile(info);
	}

	private void downFile(DYVideoInfo info)
	{
		DownloadClass downloadClass = new DownloadClass();
		downloadClass.downCallback += delegate(long maxNum, long readLen)
		{
			Application.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				int num = int.Parse((readLen * 100 / maxNum).ToString());
				lb_down.Content = num + "%";
				pro_down.Value = num;
				if (maxNum <= readLen)
				{
					Close();
				}
			});
		};
		string text = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
		CaptureVideoBLL.DownloadFile(info.VideoUrl, SysCommon.VideoPath + "\\" + info.AwemeId + "_" + text + ".mp4", downloadClass);
	}


}
