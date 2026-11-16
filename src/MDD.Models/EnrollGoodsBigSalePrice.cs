using System.Collections.Generic;
using Newtonsoft.Json;

namespace MDD.Models;

public class EnrollGoodsBigSalePrice
{
	[JsonProperty("goods_traffic_protect_info")]
	public GoodsTrafficProtectInfo GoodsTrafficProtectInfo { get; set; }

	[JsonProperty("sku_info_list")]
	public List<SkuInfoList> SkuInfoList { get; set; }
}
