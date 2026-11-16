using System.Collections.Generic;

namespace MDD.Models;

public class ProductInfo
{
	public string thumbUrl { get; set; }

	public string minGroupPrice { get; set; }

	public string minNormalPrice { get; set; }

	public string goodsName { get; set; }

	public string goodsID { get; set; }

	public List<ProductDetailGallery> detailGallery { get; set; }

	public List<ProductTopGallery> topGallery { get; set; }

	public List<ProductVideoGallery> topVideoGallery { get; set; }

	public string topGalleryUrl { get; set; }

	public List<ProductVideoGallery> descVideoGallery { get; set; }

	public List<ProductSkus> skus { get; set; }

	public List<ProductGoodsProperty> goodsProperty { get; set; }

	public string sectionPrice { get; set; }

	public string sideSalesTip { get; set; }

	public string brandId { get; set; }

	public string shareDesc { get; set; }

	public string shareLink { get; set; }

	public string catID4 { get; set; }

	public string catID3 { get; set; }

	public string catID2 { get; set; }

	public string catID1 { get; set; }

	public string catID { get; set; }

	public string mallName { get; set; }

	public string mallLink { get; set; }

	public string mallID { get; set; }

	public string reviewNum { get; set; }

	public string reviewNumText { get; set; }

	public string descScore { get; set; }

	public string logisticsScore { get; set; }

	public string serviceScore { get; set; }

	public string mallStar { get; set; }

	public string goodsNum { get; set; }

	public string goodsNumDesc { get; set; }

	public string salesTip { get; set; }

	public string salesTipV2 { get; set; }

	public ProductCookieInfo cookieInfo { get; set; }
}
