using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace MDD.Common;

public class LogHelper
{
	private static readonly ILog log_fatal = LogManager.GetLogger("fatal");

	private static readonly ILog log_error = LogManager.GetLogger("error");

	private static readonly ILog log_warn = LogManager.GetLogger("warn");

	private static readonly ILog log_info = LogManager.GetLogger("info");

	private static readonly ILog log_debug = LogManager.GetLogger("debug");

	public static void Fatal(string fatal, Exception e)
	{
		log_info.Fatal(fatal, e);
	}

	public static void Error(string error_info, Exception e = null)
	{
		if (e == null)
		{
			log_error.Error(error_info);
			return;
		}
		string text = BeautyErrorMsg(e);
		log_error.ErrorFormat("{0} {1}", new object[2] { error_info, text });
	}

	private static string BeautyErrorMsg(Exception ex)
	{
		return string.Format("【异常类型】：{0}，【异常信息】：{1}，【堆栈调用】：{2}", new object[3]
		{
			ex.GetType().Name,
			ex.Message,
			ex.StackTrace
		});
	}

	public static void Warn(string warn_info, Exception e)
	{
		string text = BeautyErrorMsg(e);
		log_error.Warn(new object[2] { warn_info, text });
	}

	public static void Info(string info)
	{
		log_info.Info(info);
	}

	public static void Debug(string debug_info, Exception e)
	{
		string text = BeautyErrorMsg(e);
		log_debug.DebugFormat("{0} {1}", new object[2] { debug_info, text });
	}

	public static void Clear(int beforeDay = 7)
	{
		string path = AppDomain.CurrentDomain.BaseDirectory + "\\Log";
		try
		{
			if (!Directory.Exists(path))
			{
				return;
			}
			DateTime now = DateTime.Now;
			List<string> list = new List<string>();
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			directoryInfo.GetFiles();
			FileInfo[] files = directoryInfo.GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				try
				{
					string[] array = fileInfo.Name.Split('.')[0].Split('-');
					DateTime dateTime = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
					if ((now - dateTime).Days > beforeDay)
					{
						list.Add(fileInfo.FullName);
					}
				}
				catch
				{
					list.Add(fileInfo.FullName);
				}
			}
			foreach (string item in list)
			{
				File.Delete(item);
			}
		}
		catch
		{
		}
	}
}
