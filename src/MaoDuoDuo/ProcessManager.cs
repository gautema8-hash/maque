using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;

namespace MaoDuoDuo;

internal static class ProcessManager
{
	private static Mutex ProcessLock;

	private static bool HasLock;

	public static void GetProcessLock()
	{
		ProcessLock = new Mutex(initiallyOwned: false, "Global\\MDD.Toolkit[" + GetUid() + "]", out HasLock);
		if (!HasLock)
		{
			ActiveWindow();
			Environment.Exit(0);
		}
	}

	private static string GetUid()
	{
		byte[] array = Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().Location);
		using (MD5 mD = MD5.Create())
		{
			array = mD.ComputeHash(array);
		}
		return BitConverter.ToString(array);
	}

	public static void ActiveWindow()
	{
		Process p = Process.GetCurrentProcess();
		try
		{
			string processName = p.ProcessName;
			Process[] array = (from it in Process.GetProcessesByName(processName)
				where it.Id != p.Id
				select it).ToArray();
			Process[] array2 = array;
			foreach (Process process in array2)
			{
				if (process.MainModule.FileName == p.MainModule.FileName)
				{
					IntPtr mainWindowHandle = process.MainWindowHandle;
					if (!IsWindowVisible(mainWindowHandle))
					{
						MessageBox.Show("请结束进程，重新打开软件！");
						process.Kill();
					}
					else
					{
						SwitchToThisWindow(mainWindowHandle, fAltTab: true);
					}
					break;
				}
			}
		}
		finally
		{
			if (p != null)
			{
				((IDisposable)p).Dispose();
			}
		}
	}

	public static void ReleaseLock()
	{
		if (ProcessLock != null && HasLock)
		{
			ProcessLock.Dispose();
			HasLock = false;
		}
	}

	[DllImport("user32.dll")]
	public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindowVisible(IntPtr hWnd);
}
