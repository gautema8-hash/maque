using System.Collections.Generic;
using Newtonsoft.Json;

namespace MDD.Models;

public class SkuInfo
{
	[JsonProperty("sku_id")]
	public long SkuId { get; set; }

	[JsonProperty("spec_info_list")]
	public List<SpecInfo> SpecInfoList { get; set; }
}
