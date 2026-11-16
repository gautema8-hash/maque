using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MDD.Common;

public class SysTool
{
	private static string[] ks_dic = new string[36]
	{
		"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
		"a", "b", "c", "d", "e", "f", "g", "h", "i", "j",
		"k", "l", "m", "n", "o", "p", "q", "r", "s", "t",
		"u", "v", "w", "x", "y", "z"
	};

	public static string GetNullOrString(object obj)
	{
		if (obj == null)
		{
			return string.Empty;
		}
		return obj.ToString();
	}

	public static List<string> ExtractNumbers(string input)
	{
		string pattern = "\\d+(\\.\\d+)?";
		MatchCollection matchCollection = Regex.Matches(input, pattern);
		List<string> list = new List<string>();
		foreach (Match item in matchCollection)
		{
			list.Add(item.Value);
		}
		return list;
	}

	public static string changeNumber(double number)
	{
		string text = "";
		if (number < 10000.0)
		{
			return number.ToString() ?? "";
		}
		if (number < 100000000.0)
		{
			return Math.Round(number / 10000.0, 2) + "万";
		}
		return Math.Round(number / 100000000.0, 2) + "亿";
	}

	public static string createDid()
	{
		string text = "web_";
		Random random = new Random();
		for (int i = 0; i < 15; i++)
		{
			text += ks_dic[random.Next(0, 36)];
		}
		for (int j = 0; j < 4; j++)
		{
			text += random.Next(0, 10);
		}
		for (int k = 0; k < 13; k++)
		{
			text += ks_dic[random.Next(0, 36)];
		}
		return text;
	}
}
