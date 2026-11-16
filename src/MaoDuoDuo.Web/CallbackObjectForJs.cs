using System;

namespace MaoDuoDuo.Web;

public class CallbackObjectForJs
{
	public void showMessage(string msg)
	{
		Console.WriteLine("浏览器：" + msg + DateTime.Now.ToString());
	}
}
