using Newtonsoft.Json;

namespace MDD.Models;

public class EnrollGoodsRuleVo
{
	[JsonProperty("goods_id")]
	public long GoodsId { get; set; }

	[JsonProperty("enroll_rule_param")]
	public EnrollRuleParam EnrollRuleParam { get; set; }
}
