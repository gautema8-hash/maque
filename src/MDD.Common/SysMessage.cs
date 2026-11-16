using System;
using System.Windows;
using MDD.Models.Enum;
using ZdfFlatUI;

namespace MDD.Common;

public class SysMessage
{
	public static MessageEnum MessageShow(Window owner, string messageBoxText, string caption, MessageBoxButton button, MDD.Models.Enum.EnumPromptType type = MDD.Models.Enum.EnumPromptType.None)
	{
		return (MessageEnum)Enum.Parse(typeof(MessageEnum), ZMessageBox.Show(owner, messageBoxText, caption, button).ToString());
	}
}
