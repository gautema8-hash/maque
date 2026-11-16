using Newtonsoft.Json;

namespace MDD.Models;

public class GoodsTrafficProtectInfo
{
	[JsonProperty("high_sensitive_goods")]
	public bool HighSensitiveGoods { get; set; }

	[JsonProperty("fill_max_cut_price_amount")]
	public int FillMaxCutPriceAmount { get; set; }
}
