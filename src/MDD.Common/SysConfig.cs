using System;
using System.Configuration;

namespace MDD.Common;

public class SysConfig
{
	public static void UpdateConfigSetting(string settingName, string valueName)
	{
		Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		if (ConfigurationManager.AppSettings[settingName] != null)
		{
			configuration.AppSettings.Settings.Remove(settingName);
		}
		configuration.AppSettings.Settings.Add(settingName, valueName);
		configuration.Save(ConfigurationSaveMode.Modified);
		ConfigurationManager.RefreshSection("appSettings");
	}

	public static string GetConfigString(string settingName)
	{
		try
		{
			return ConfigurationManager.AppSettings[settingName].ToString();
		}
		catch (Exception)
		{
			return null;
		}
	}
}
