using System.Collections.Generic;

namespace MDD.Models;

public class ProductSkus
{
	public string attribute { get; set; }

	public string defaultQuantity { get; set; }

	public string goodsId { get; set; }

	public string groupPrice { get; set; }

	public string groupTip { get; set; }

	public string groupTipColor { get; set; }

	public string groupTipFont { get; set; }

	public string groupTipType { get; set; }

	public string initQuantity { get; set; }

	public string isOnsale { get; set; }

	public string limitQuantity { get; set; }

	public string marketPrice { get; set; }

	public string normalPrice { get; set; }

	public string normalSavePrice { get; set; }

	public string oldGroupPrice { get; set; }

	public string previewPriority { get; set; }

	public string price { get; set; }

	public ProductSkusPriceDisplay priceDisplay { get; set; }

	public string quantity { get; set; }

	public string skuExpansionPrice { get; set; }

	public string skuID { get; set; }

	public string skuId { get; set; }

	public string soldQuantity { get; set; }

	public string spec { get; set; }

	public List<ProductSkusSpecs> specs { get; set; }

	public string staticLimitQuantity { get; set; }

	public string thumbUrl { get; set; }

	public List<ProductSkusYellowLabelList> yellowLabelList { get; set; }
}
