using System.Collections.Generic;

namespace MaoDuoDuo;

public class SkuPrice
{
	public double groupPrice { get; set; }

	public double price { get; set; }

	public string num { get; set; }

	public string goodsId { get; set; }

	public List<string> values => new List<string>
	{
		num,
		groupPrice.ToString(),
		price.ToString(),
		goodsId
	};
}
