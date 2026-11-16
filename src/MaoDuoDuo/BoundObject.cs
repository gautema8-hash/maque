using System.Collections.Generic;
using MDD.Common;
using MDD.Models;

namespace MaoDuoDuo;

public class BoundObject
{
	public string type { get; set; } = "";


	public int skuLen { get; set; } = 0;


	public MDD.Common.SettingInfo setInfo { get; set; }

	public string reason { get; set; }

	public List<string> reaList { get; set; } = new List<string>();


	public int nowId { get; set; } = 0;


	public string lastId { get; set; }

	public bool isStart { get; set; } = false;


	public bool isEnd { get; set; } = false;


	public bool isSuccess { get; set; } = false;


	public int sensuNum { get; set; } = 0;


	public List<string> ReasonList { get; set; }

	public string ssReason { get; set; } = "买家描述的问题本就为恶意捏造和夸大，本店已经同意给买家退货退款或者换货，并且运费和损失由本店承担，但贵平台不经本店同意就擅自同意买家的退款申请，自作主张给买家退款不退货，并且违反了《消费者权益保护法》第49条的规定，现请平台尽快处理退回我的货款，否则我将继续进行投诉";


	public bool isStartLogin { get; set; } = false;


	public bool isStartLogin1 { get; set; } = false;


	public bool isLogin { get; set; } = true;


	public bool isLogin2 { get; set; } = true;


	public int step { get; set; }

	public string url { get; set; }

	public string newGoodId { get; set; }

	public string goodId { get; set; }

	public int waitTime { get; set; }

	public int jiangeTime { get; set; }

	public int isRun { get; set; } = 1;


	public string dPrice { get; set; }

	public double minPrice { get; set; }

	public double minPrice1 { get; set; }

	public double minPrice2 { get; set; }

	public string msg { get; set; } = "";


	public string endMsg { get; set; } = "";


	public string jsonString { get; set; } = "";


	public string rawData { get; set; } = "";


	public bool isSkip { get; set; } = false;


	public bool isPublish { get; set; } = true;


	public string goodsName { get; set; }

	public string goodsName2 { get; set; }

	public string imgPath { get; set; }

	public List<string> topImage { get; set; }

	public List<string> detailImage { get; set; }

	public string goodsProperty { get; set; }

	public List<ProductGoodsProperty> goodsPropertyList { get; set; }

	public string cateInfo { get; set; }

	public List<SkuInfo> SkuList { get; set; }

	public string skuInfos { get; set; }

	public int skuCount { get; set; } = 0;


	public List<SkuPrice> SkuPriceList { get; set; }

	public string skuPrice { get; set; }

	public int imgId { get; set; }

	public SettingInfo settingInfo { get; set; } = new SettingInfo();


	public ImageInfo imageInfo { get; set; } = new ImageInfo();

}
