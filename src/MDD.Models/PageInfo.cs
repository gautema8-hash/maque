using Newtonsoft.Json;

namespace MDD.Models;

public class PageInfo
{
	[JsonProperty("num_rows")]
	public int NumRows { get; set; }

	public int Px { get; set; }

	public int Pz { get; set; }

	[JsonProperty("num_page")]
	public int NumPage { get; set; }
}
