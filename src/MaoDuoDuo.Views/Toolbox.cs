using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaoDuoDuo.Views;

public partial class Toolbox : UserControl
{
	public Toolbox()
	{
		InitializeComponent();
	}

	private MainWindow Main => Window.GetWindow(this) as MainWindow;

	private void Run(string tool)
	{
		MainWindow mainWindow = Main;
		if (mainWindow == null)
		{
			return;
		}
		try
		{
			mainWindow.RunTool(tool);
		}
		catch
		{
		}
	}

	private void Kdc_Click(object sender, MouseButtonEventArgs e) => Run("kdc");

	private void Fan_Click(object sender, MouseButtonEventArgs e) => Run("fan");

	private void Off_Click(object sender, MouseButtonEventArgs e) => Run("off");

	private void Tk_Click(object sender, MouseButtonEventArgs e) => Run("tk");

	private void Tk2_Click(object sender, MouseButtonEventArgs e) => Run("tk2");

	private void Reason_Click(object sender, MouseButtonEventArgs e) => Run("reason");

	private void Imgset_Click(object sender, MouseButtonEventArgs e) => Run("imgset");

	private void Compute_Click(object sender, MouseButtonEventArgs e) => Run("compute");

	private void Monitor_Click(object sender, MouseButtonEventArgs e) => Run("monitor");
}
