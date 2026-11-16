using Newtonsoft.Json;

namespace MDD.Models;

public class NoticeInfo
{
	public string Id { get; set; }

	public string Url { get; set; }

	public string Title { get; set; }

	[JsonProperty("short_desc")]
	public string ShortDesc { get; set; }

	public string Content { get; set; }
}
