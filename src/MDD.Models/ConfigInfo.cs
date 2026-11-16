using System.Collections.Generic;

namespace MDD.Models;

public class ConfigInfo
{
	public List<DirectEntryInfo> DirectEntry { get; set; }

	public string[] HotSearch { get; set; }

	public object marketData { get; set; }
}
