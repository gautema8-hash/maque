using Newtonsoft.Json;

namespace MDD.Models;

public class EnrollGoodsRestrictActivityPrice
{
	[JsonProperty("goods_id")]
	public long GoodsId { get; set; }
}
