using System;

namespace MDD.Models;

[Serializable]
public class item
{
	public string version { get; set; }

	public string url { get; set; }

	public string changelog { get; set; }

	public string mandatory { get; set; }
}
