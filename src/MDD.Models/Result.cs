using System.Collections.Generic;
using Newtonsoft.Json;

namespace MDD.Models;

public class Result
{
	[JsonProperty("activity_id")]
	public int ActivityId { get; set; }

	[JsonProperty("goods_id_list")]
	public List<long> GoodsIdList { get; set; }

	[JsonProperty("enroll_goods_rule_vos")]
	public List<EnrollGoodsRuleVo> EnrollGoodsRuleVos { get; set; }
}
