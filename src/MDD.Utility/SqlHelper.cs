using System;
using System.Data;
using System.Data.SQLite;
using MDD.Common;

namespace MDD.Utility;

public class SqlHelper
{
	private static readonly string connStr = "mddph.sqlite";

	private static SQLiteConnection sqliteConn;

	public SqlHelper()
	{
		sqliteConn = new SQLiteConnection("Data Source=" + connStr + ";");
		sqliteConn.Open();
		Query($"select * from submit_log");
		EnsureSchema();
	}

	// 重建版：补建历史版本缺失的数据表，避免首次运行时功能异常
	private static void EnsureSchema()
	{
		Query("CREATE TABLE IF NOT EXISTS dd_good_monitor (id INTEGER PRIMARY KEY AUTOINCREMENT, good_id TEXT, good_name TEXT, good_num INTEGER DEFAULT 0, good_state INTEGER DEFAULT 1, update_time TEXT, create_time TEXT)");
		Query("CREATE TABLE IF NOT EXISTS dd_good_monitor_info (id INTEGER PRIMARY KEY AUTOINCREMENT, good_id TEXT, good_name TEXT, good_num INTEGER DEFAULT 0, update_time TEXT, create_time TEXT)");
		Query("CREATE TABLE IF NOT EXISTS dd_shop (id INTEGER PRIMARY KEY AUTOINCREMENT, shop_id TEXT, uuid TEXT, name TEXT, mobile TEXT, staple TEXT, choose_state INTEGER DEFAULT 0, create_time TEXT)");
		Query("CREATE TABLE IF NOT EXISTS dy_login (id INTEGER PRIMARY KEY, name TEXT, value TEXT)");
		Query("INSERT INTO dy_login (id, name, value) SELECT 1, '', '' WHERE NOT EXISTS (SELECT 1 FROM dy_login WHERE id = 1)");
	}

	public static DataTable Query(string sql)
	{
		DataTable dataTable = new DataTable();
		try
		{
			new SQLiteDataAdapter(new SQLiteCommand(sql, sqliteConn)).Fill(dataTable);
		}
		catch (Exception e)
		{
			LogHelper.Error("Query：" + sql, e);
		}
		return dataTable;
	}

	public static string Update(string sql, bool getId = false)
	{
		try
		{
			new SQLiteCommand(sql, sqliteConn).ExecuteNonQuery();
			if (getId)
			{
				using (SQLiteCommand sQLiteCommand = new SQLiteCommand("SELECT last_insert_rowid()", sqliteConn))
				{
					return ((long)sQLiteCommand.ExecuteScalar()).ToString();
				}
			}
			return "true";
		}
		catch (Exception ex)
		{
			LogHelper.Error("Update：" + sql, ex);
			return ex.Message;
		}
	}
}
