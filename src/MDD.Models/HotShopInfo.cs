using Newtonsoft.Json;

namespace MDD.Models;

public class HotShopInfo
{
	public string Id { get; set; }

	[JsonProperty("template_goods_id")]
	public string TemplateGoodsId { get; set; }

	[JsonProperty("template_goods_name")]
	public string TemplateGoodsName { get; set; }

	[JsonProperty("image_url")]
	public string ImageUrl { get; set; }

	[JsonProperty("reference_price")]
	public string ReferencePrice { get; set; }

	public string Sales { get; set; }

	[JsonProperty("yesterday_reference_sales_min")]
	public string MinDaySalesNum { get; set; }

	[JsonProperty("yesterday_reference_sales_max")]
	public string MaxDaySalesNum { get; set; }

	[JsonProperty("yesterday_reference_sales_difference_count_min_7")]
	public string MinSevenSalesNum { get; set; }

	[JsonProperty("yesterday_reference_sales_difference_count_max_7")]
	public string MaxSevenSalesNum { get; set; }

	[JsonProperty("yesterday_reference_sales_difference_count_min_30")]
	public string MinThirtySalesNum { get; set; }

	[JsonProperty("yesterday_reference_sales_difference_count_max_30")]
	public string MaxThirtySalesNum { get; set; }

	public string SelectCondition { get; set; }

	public string SalesNum { get; set; }

	public string ShopUrl { get; set; }
}
