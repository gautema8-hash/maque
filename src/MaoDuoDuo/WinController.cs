using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using MDD.Common;
using MDD.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace DuoDuoPuHuo;

public static class WinController
{
	private struct POINT
	{
		public int X;

		public int Y;
	}

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public struct INPUT
	{
		public uint type;

		public MOUSEINPUT mi;
	}

	public struct MOUSEINPUT
	{
		public int dx;

		public int dy;

		public uint mouseData;

		public uint dwFlags;

		public uint time;

		public IntPtr dwExtraInfo;
	}

	public struct KEYBDINPUT
	{
		public ushort wVk;

		public ushort wScan;

		public uint dwFlags;

		public uint time;

		public IntPtr dwExtraInfo;
	}

	private const uint INPUT_MOUSE = 0u;

	private const uint MOUSEEVENTF_LEFTDOWN = 2u;

	private const uint MOUSEEVENTF_LEFTUP = 4u;

	private const uint INPUT_KEYBOARD = 1u;

	private const uint KEYEVENTF_KEYUP = 2u;

	private const int SB_THUMBPOSITION = 4;

	private const uint WM_SETTEXT = 12u;

	private const uint WM_HSCROLL = 276u;

	private const uint WM_VSCROLL = 277u;

	private const int MOUSEEVENTF_WHEEL = 2048;

	private const uint WM_KEYDOWN = 256u;

	private const uint WM_KEYUP = 257u;

	private const int VK_RETURN = 13;

	private static string img_opencv;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr GetWindowDC(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

	[DllImport("user32.dll")]
	public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

	[DllImport("user32.dll")]
	public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern int SendMessageA(IntPtr hwnd, uint wMsg, IntPtr wParam, string lParam);

	[DllImport("user32.dll")]
	public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

	[DllImport("user32.dll")]
	public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	[DllImport("user32.dll")]
	private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForSystem();

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr hwnd);

	public static bool GetWinDPI()
	{
		uint dpiForSystem = GetDpiForSystem();
		Console.WriteLine($"系统 DPI: {dpiForSystem}");
		IntPtr zero = IntPtr.Zero;
		uint dpiForWindow = GetDpiForWindow(zero);
		Console.WriteLine($"窗口 DPI: {dpiForWindow}");
		if (dpiForSystem > 96 || dpiForWindow > 96)
		{
			return true;
		}
		return false;
	}

	public static IntPtr FindWindow(string className)
	{
		return FindWindow(className, null);
	}

	public static void SetWindowPos(IntPtr hWnd, int newWidth, int newHeight)
	{
		SetWindowPos(hWnd, IntPtr.Zero, 0, 0, newWidth, newHeight, 4u);
	}

	public static void WaitTime(int time)
	{
		Random random = new Random();
		int num = random.Next(100, 1000);
		Thread.Sleep(time + num);
	}

	public static Mat KeepCapture(IntPtr hWnd, RECT pos, string savePath = "", bool isShow = false)
	{
		SetForegroundWindow(hWnd);
		Thread.Sleep(200);
		GetClientRect(hWnd, out var lpRect);
		POINT pOINT = default(POINT);
		pOINT.X = lpRect.Left;
		pOINT.Y = lpRect.Top;
		POINT lpPoint = pOINT;
		ClientToScreen(hWnd, ref lpPoint);
		GetClientRect(hWnd, out var lpRect2);
		if (pos.Left < 0)
		{
			pos = lpRect2;
		}
		int width = pos.Right - pos.Left;
		int height = pos.Bottom - pos.Top;
		using Bitmap bitmap = new Bitmap(width, height);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.CopyFromScreen(new System.Drawing.Point(pos.Left, pos.Top), System.Drawing.Point.Empty, new System.Drawing.Size(width, height));
		}
		if (savePath.Length > 0)
		{
			bitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
		}
		Mat mat = bitmap.ToMat();
		if (isShow)
		{
			Cv2.ImShow("Base64 Image", mat);
			Cv2.WaitKey();
			Cv2.DestroyAllWindows();
		}
		return mat;
	}

	private static Bitmap ResizeBitmap(Bitmap original, float scaleFactor)
	{
		int width = (int)((float)original.Width * scaleFactor);
		int height = (int)((float)original.Height * scaleFactor);
		Bitmap bitmap = new Bitmap(width, height);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(original, 0, 0, width, height);
		}
		return bitmap;
	}

	public static void ForegroundWindow(IntPtr hWnd)
	{
		SetForegroundWindow(hWnd);
	}

	public static void MoveWin(Tuple<int, int> pos, bool isRandom = true)
	{
		int num = 0;
		int num2 = 0;
		if (isRandom)
		{
			Random random = new Random();
			num = random.Next(0, 15);
			num2 = random.Next(0, 15);
		}
		Cursor.Position = new System.Drawing.Point(pos.Item1 + num, pos.Item2 + num2);
	}

	public static void ClickWin(Tuple<int, int> pos, bool isRandom = true, bool isDouble = false)
	{
		Random random = new Random();
		int num = 0;
		int num2 = 0;
		if (isRandom)
		{
			num = random.Next(0, 10);
			num2 = random.Next(0, 10);
		}
		Cursor.Position = new System.Drawing.Point(pos.Item1 + num, pos.Item2 + num2);
		int millisecondsTimeout = random.Next(10, 100);
		Thread.Sleep(millisecondsTimeout);
		INPUT[] array = new INPUT[2];
		array[0].type = 0u;
		array[0].mi.dx = 0;
		array[0].mi.dy = 0;
		array[0].mi.mouseData = 0u;
		array[0].mi.dwFlags = 2u;
		array[0].mi.time = 0u;
		array[0].mi.dwExtraInfo = IntPtr.Zero;
		array[1].type = 0u;
		array[1].mi.dx = 0;
		array[1].mi.dy = 0;
		array[1].mi.mouseData = 0u;
		array[1].mi.dwFlags = 4u;
		array[1].mi.time = 0u;
		array[1].mi.dwExtraInfo = IntPtr.Zero;
		SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT)));
		if (isDouble)
		{
			Thread.Sleep(millisecondsTimeout);
			SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT)));
		}
	}

	public static void KeyWinInput(IntPtr hWnd)
	{
		SendMessage(hWnd, 256u, (IntPtr)13, IntPtr.Zero);
		SendMessage(hWnd, 257u, (IntPtr)13, IntPtr.Zero);
		Console.WriteLine("回车键已发送.");
	}

	public static void WriteWin(string value)
	{
		Thread.Sleep(500);
		SendKeys.SendWait(value);
	}

	public static void Huadong(IntPtr hwnd)
	{
		int num = 100;
		SendMessage(hwnd, 276u, (IntPtr)(4 | (num << 16)), IntPtr.Zero);
		Console.WriteLine("滑动命令已发送.");
		Console.WriteLine("命令已发送.");
	}

	public static void ShowImg()
	{
		string imgBase = "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";
		Mat mat = BaseConvert(imgBase);
		Cv2.ImShow("Base64 Image", mat);
		Cv2.WaitKey();
		Cv2.DestroyAllWindows();
	}

	public static (int, int) FindPicWinEx(IntPtr hwnd, Tuple<int, int, int, int> pos, string templateImageString, bool isShow = false, double threshold = 0.8)
	{
		RECT rECT = default(RECT);
		rECT.Left = pos.Item1;
		rECT.Top = pos.Item2;
		rECT.Right = pos.Item3;
		rECT.Bottom = pos.Item4;
		RECT pos2 = rECT;
		Mat mainImage = KeepCapture(hwnd, pos2, "", isShow);
		var (num, num2) = MatchTemplate(mainImage, templateImageString, isShow, threshold);
		if (num < 0)
		{
			return (num, num2);
		}
		return (num + pos.Item1, num2 + pos.Item2);
	}

	public static (int, int) MatchTemplate(Mat mainImage, string templateImageString, bool isShow = false, double threshold = 0.8)
	{
		try
		{
			if (!SysCommon.isSuofang && threshold == 0.8)
			{
				threshold = 0.6;
			}
			Cv2.CvtColor(mainImage, mainImage, ColorConversionCodes.BGR2GRAY);
			Mat mat = BaseConvert(templateImageString);
			if (mat.Channels() == 3)
			{
				Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
			}
			int width = mainImage.Width - mat.Width + 1;
			int height = mainImage.Height - mat.Height + 1;
			Mat mat2 = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_32FC1);
			Cv2.MatchTemplate(mainImage, mat, mat2, TemplateMatchModes.CCoeffNormed);
			OpenCvSharp.Point point = new OpenCvSharp.Point(-1, -1);
			bool flag = false;
			for (int i = 0; i < mat2.Rows; i++)
			{
				for (int j = 0; j < mat2.Cols; j++)
				{
					float num = mat2.At<float>(i, j);
					if ((double)num >= threshold && (!flag || j < point.X || (j == point.X && i < point.Y)))
					{
						point = new OpenCvSharp.Point(j, i);
						flag = true;
					}
				}
			}
			if (flag)
			{
				if (isShow)
				{
					OpenCvSharp.Point[] array = new OpenCvSharp.Point[4]
					{
						new OpenCvSharp.Point(point.X, point.Y),
						new OpenCvSharp.Point(point.X + mat.Width, point.Y),
						new OpenCvSharp.Point(point.X + mat.Width, point.Y + mat.Height),
						new OpenCvSharp.Point(point.X, point.Y + mat.Height)
					};
					Cv2.Polylines(mainImage, new OpenCvSharp.Point[1][] { array }, isClosed: true, Scalar.Red, 2);
					Cv2.ImShow("Match Result", mainImage);
					Cv2.WaitKey();
				}
				return (point.X + mat.Width / 2, point.Y + mat.Height / 2);
			}
			return (-1, -1);
		}
		catch (Exception)
		{
			return (-2, -2);
		}
	}

	public static Mat BaseConvert(string imgBase)
	{
		byte[] buf = Convert.FromBase64String(imgBase);
		Mat mat = Cv2.ImDecode(buf, ImreadModes.Grayscale);
		Mat mat2 = new Mat();
		Cv2.CvtColor(mat, mat2, ColorConversionCodes.GRAY2BGR);
		if (!SysCommon.isSuofang)
		{
			Mat mat3 = new Mat();
			Cv2.Resize(mat2, mat3, new OpenCvSharp.Size(0, 0), 0.8, 0.8);
			mat2 = mat3;
		}
		return mat2;
	}

	private static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
	{
		using MemoryStream stream = new MemoryStream();
		BitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
		bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));
		bitmapEncoder.Save(stream);
		return new Bitmap(stream);
	}

	private static Mat BitmapToMat(Bitmap bitmap)
	{
		using MemoryStream memoryStream = new MemoryStream();
		bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
		return Mat.FromImageData(memoryStream.ToArray());
	}

	public static void ScrollMouseWheelDown(int scrollAmount)
	{
		mouse_event(2048, 0, 0, scrollAmount, 0);
	}

	public static void UploadImg(string filePath)
	{
		IntPtr intPtr = FindWindow(null, "打开");
		if (intPtr == IntPtr.Zero)
		{
			LogHelper.Info("JTK：102");
			return;
		}
		LogHelper.Info("JTK：103");
		IntPtr hwnd = FindWindowEx(intPtr, IntPtr.Zero, "ComboBoxEx32", null);
		SendMessageA(hwnd, 12u, IntPtr.Zero, filePath);
		Thread.Sleep(500);
		LogHelper.Info("JTK：105");
		IntPtr hwnd2 = FindWindowEx(intPtr, IntPtr.Zero, "Button", null);
		SendMessageA(hwnd2, 245u, IntPtr.Zero, "");
	}

	public static string OcrText(IntPtr hwnd, Tuple<int, int, int, int> pos, bool isShow = false)
	{
		string result = "";
		try
		{
			LogHelper.Error("11111111");
			RECT rECT = default(RECT);
			rECT.Left = pos.Item1;
			rECT.Top = pos.Item2;
			rECT.Right = pos.Item3;
			rECT.Bottom = pos.Item4;
			RECT pos2 = rECT;
			Pix image = KeepCapture2(hwnd, pos2);
			string text = AppDomain.CurrentDomain.BaseDirectory + "Resources\\tessdata";
			LogHelper.Error("tessDataPath: " + text);
			using TesseractEngine tesseractEngine = new TesseractEngine(text, "chi_sim", EngineMode.Default);
			using Page page = tesseractEngine.Process(image);
			result = page.GetText();
		}
		catch (Exception arg)
		{
			LogHelper.Error($"ocr error: {arg}");
		}
		return result;
	}

	private static Bitmap ScaleBitmap(Bitmap original, float scaleFactor)
	{
		int width = (int)((float)original.Width * scaleFactor);
		int height = (int)((float)original.Height * scaleFactor);
		Bitmap bitmap = new Bitmap(width, height);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(original, 0, 0, width, height);
		}
		return bitmap;
	}

	public static Pix KeepCapture2(IntPtr hWnd, RECT pos, string savePath = "")
	{
		SetForegroundWindow(hWnd);
		Thread.Sleep(200);
		GetClientRect(hWnd, out var lpRect);
		POINT pOINT = default(POINT);
		pOINT.X = lpRect.Left;
		pOINT.Y = lpRect.Top;
		POINT lpPoint = pOINT;
		ClientToScreen(hWnd, ref lpPoint);
		GetClientRect(hWnd, out var lpRect2);
		if (pos.Left < 0)
		{
			pos = lpRect2;
		}
		int width = pos.Right - pos.Left;
		int height = pos.Bottom - pos.Top;
		using MemoryStream memoryStream = new MemoryStream();
		using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
		{
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.CopyFromScreen(new System.Drawing.Point(pos.Left, pos.Top), System.Drawing.Point.Empty, new System.Drawing.Size(width, height));
			}
			int num = 2;
			if (!SysCommon.isSuofang)
			{
				num = 3;
			}
			Bitmap bitmap2 = ScaleBitmap(bitmap, num);
			bitmap2.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
		}
		memoryStream.Seek(0L, SeekOrigin.Begin);
		return Pix.LoadFromMemory(memoryStream.ToArray());
	}

	private static void HighlightImage(Bitmap bitmap)
	{
		for (int i = 0; i < bitmap.Height; i++)
		{
			for (int j = 0; j < bitmap.Width; j++)
			{
				Color pixel = bitmap.GetPixel(j, i);
				int red = Math.Min(pixel.R + 50, 255);
				int green = Math.Min(pixel.G + 50, 255);
				int blue = Math.Min(pixel.B + 50, 255);
				bitmap.SetPixel(j, i, Color.FromArgb(red, green, blue));
			}
		}
	}
}
