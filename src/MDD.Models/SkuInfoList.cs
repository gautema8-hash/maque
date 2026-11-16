using Newtonsoft.Json;

namespace MDD.Models;

public class SkuInfoList
{
	[JsonProperty("require_price")]
	public int RequirePrice { get; set; }

	[JsonProperty("filling_sku_price")]
	public int FillingSkuPrice { get; set; }
}
