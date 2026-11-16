using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MDD.Models;
using MDD.Utility;

namespace MDD.DAL;

public class ShopSetDAL
{
	private static List<ShopInfo> ChangeInfo(DataTable dt)
	{
		int i = 1;
		return (from row in dt.AsEnumerable()
			select new ShopInfo
			{
				Id = Convert.ToString(row["Id"]),
				Num = i++,
				ShopName = Convert.ToString(row["shop_name"]),
				GoodId = Convert.ToString(row["good_id"]),
				GoodId2 = Convert.ToString(row["good_id2"]),
				Result = Convert.ToString(row["result"]),
				State = Convert.ToInt32(row["state"]),
				UploadTime = Convert.ToString(row["upload_time"])
			}).ToList();
	}

	public static List<ShopInfo> GetAllInfo()
	{
		List<ShopInfo> result = new List<ShopInfo>();
		DataTable dataTable = SqlHelper.Query("select * from upload_info where state = 1");
		if (dataTable != null && dataTable.Rows.Count > 0)
		{
			result = ChangeInfo(dataTable);
		}
		return result;
	}

	public static StoreInfo GetShopInfo(string shopId)
	{
		DataTable dt = SqlHelper.Query("select * from dd_shop where shop_id = '" + shopId + "'");
		if (dt.Rows.Count == 0)
		{
			return new StoreInfo();
		}
		DataRow row = dt.Rows[0];
		return new StoreInfo
		{
			Id = Convert.ToInt32(row["id"]),
			ShopId = Convert.ToString(row["shop_id"]),
			UUID = Convert.ToString(row["uuid"]),
			Name = Convert.ToString(row["name"]),
			Mobile = Convert.ToString(row["mobile"]),
			Staple = Convert.ToString(row["staple"]),
			ChooseState = Convert.ToInt32(row["choose_state"]),
			CreateTime = Convert.ToString(row["create_time"])
		};
	}

	public static List<StoreInfo> GetShopInfoByName(string type, string name)
	{
		List<StoreInfo> result = new List<StoreInfo>();
		string text = "select * from dd_shop where 1=1";
		if (!string.IsNullOrEmpty(name))
		{
			text = text + " and name like '%" + name + "%'";
		}
		if (type != "全部")
		{
			text = text + " and staple = '" + type + "'";
		}
		DataTable dt = SqlHelper.Query(text);
		foreach (DataRow row in dt.Rows)
		{
			result.Add(new StoreInfo
			{
				Id = Convert.ToInt32(row["id"]),
				Num = result.Count + 1,
				ShopId = Convert.ToString(row["shop_id"]),
				UUID = Convert.ToString(row["uuid"]),
				Name = Convert.ToString(row["name"]),
				Mobile = Convert.ToString(row["mobile"]),
				Staple = Convert.ToString(row["staple"]),
				ChooseState = row.Table.Columns.Contains("choose_state") ? Convert.ToInt32(row["choose_state"]) : 0,
				CreateTime = Convert.ToString(row["create_time"])
			});
		}
		return result;
	}

	public static string UpdateShop(ShopInfo info)
	{
		return SqlHelper.Update($"update upload_info set state = 0 where id='{info.Id}'");
	}

	public static string UpdateShopAll()
	{
		return SqlHelper.Update($"update upload_info set state = 0");
	}

	public static string AddShop(ShopInfo info)
	{
		return SqlHelper.Update($"insert into upload_info(shop_name,good_id,good_id2,upload_time,result,state) \r\n                    values('{info.ShopName}','{info.GoodId}','{info.GoodId2}','{info.UploadTime}','{info.Result}',1)", getId: true);
	}

	public static string UpdateShopState(StoreInfo info)
	{
		return SqlHelper.Update($"update dd_shop set choose_state = {info.ChooseState} where shop_id='{info.ShopId}'");
	}

	public static string ClearInfo()
	{
		return SqlHelper.Update("DELETE FROM dd_shop");
	}
}
