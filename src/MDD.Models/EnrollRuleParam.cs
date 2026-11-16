using Newtonsoft.Json;

namespace MDD.Models;

public class EnrollRuleParam
{
	[JsonProperty("enroll_mall_sign_manfan")]
	public EnrollMallSignManfan EnrollMallSignManfan { get; set; }

	[JsonProperty("enroll_goods_restrict_activity_price")]
	public EnrollGoodsRestrictActivityPrice EnrollGoodsRestrictActivityPrice { get; set; }

	[JsonProperty("enroll_goods_big_sale_price")]
	public EnrollGoodsBigSalePrice EnrollGoodsBigSalePrice { get; set; }
}
