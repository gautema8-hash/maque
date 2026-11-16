using System.Collections.Generic;
using MDD.DAL;
using MDD.Models;

namespace MDD.BLL;

public class ShopSetBLL
{
	public static string ClearInfo()
	{
		return ShopSetDAL.ClearInfo();
	}

	public static List<ShopInfo> GetAllInfo()
	{
		return ShopSetDAL.GetAllInfo();
	}

	public static StoreInfo GetShopInfo(string shopId)
	{
		return ShopSetDAL.GetShopInfo(shopId);
	}

	public static List<StoreInfo> GetShopInfoByName(string type, string name)
	{
		return ShopSetDAL.GetShopInfoByName(type, name);
	}

	public static string UpdateShop(ShopInfo info)
	{
		return ShopSetDAL.UpdateShop(info);
	}

	public static string UpdateShopAll()
	{
		return ShopSetDAL.UpdateShopAll();
	}

	public static string AddShop(ShopInfo info)
	{
		return ShopSetDAL.AddShop(info);
	}

	public static string UpdateShopState(StoreInfo info)
	{
		return ShopSetDAL.UpdateShopState(info);
	}
}
