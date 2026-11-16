using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using MySql.Data.MySqlClient;
using MDD.Common;

namespace MDD.Utility;

public class SqlHelper
{
	// 数据库后端：配置 appSettings["DbType"] = Sqlite(默认) | MySql
	private static readonly string DbType = ConfigurationManager.AppSettings["DbType"] ?? "Sqlite";
	private static readonly string MySqlConn = ConfigurationManager.AppSettings["MySqlConn"] ?? "";
	private static readonly string SqLiteConnStr = "Data Source=mddph.sqlite;";

	private static IDbConnection _conn;

	public SqlHelper()
	{
		if (_conn == null)
		{
			_conn = CreateConn();
			_conn.Open();
		}
		EnsureSchema();
	}

	private static IDbConnection CreateConn()
	{
		if (string.Equals(DbType, "MySql", StringComparison.OrdinalIgnoreCase))
		{
			return new MySqlConnection(MySqlConn);
		}
		return new SQLiteConnection(SqLiteConnStr);
	}

	private static IDbConnection GetConn()
	{
		if (_conn == null)
		{
			_conn = CreateConn();
			_conn.Open();
		}
		return _conn;
	}

	private static void Fill(DataTable dataTable, IDbCommand cmd)
	{
		if (cmd is MySqlCommand myCmd)
		{
			new MySqlDataAdapter(myCmd).Fill(dataTable);
		}
		else
		{
			new SQLiteDataAdapter((SQLiteCommand)cmd).Fill(dataTable);
		}
	}

	// 数据库结构自动补建（幂等）
	private static void EnsureSchema()
	{
		bool isMySql = string.Equals(DbType, "MySql", StringComparison.OrdinalIgnoreCase);
		if (isMySql)
		{
			Query("CREATE TABLE IF NOT EXISTS upload_info (id INT AUTO_INCREMENT PRIMARY KEY, num INT NULL, shop_name VARCHAR(255) NULL, good_id VARCHAR(64) NULL, good_id2 VARCHAR(64) NULL, upload_time VARCHAR(32) NULL, result VARCHAR(255) NULL, state INT DEFAULT 1) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS upload_log (id INT AUTO_INCREMENT PRIMARY KEY, num INT NULL, shop_name VARCHAR(255) NULL, good_id VARCHAR(64) NULL, good_id2 VARCHAR(64) NULL, upload_time VARCHAR(32) NULL, result VARCHAR(255) NULL, state INT DEFAULT 1) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS submit_log (id INT AUTO_INCREMENT PRIMARY KEY, good_id VARCHAR(64) NULL, num INT NULL, create_time VARCHAR(32) NULL) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS dd_shop (id INT AUTO_INCREMENT PRIMARY KEY, shop_id VARCHAR(64) NULL, uuid VARCHAR(64) NULL, name VARCHAR(255) NULL, mobile VARCHAR(32) NULL, staple VARCHAR(255) NULL, choose_state INT DEFAULT 0, create_time VARCHAR(32) NULL) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS dd_good_monitor (id INT AUTO_INCREMENT PRIMARY KEY, good_id VARCHAR(64) NULL, good_name VARCHAR(255) NULL, good_num INT DEFAULT 0, good_state INT DEFAULT 1, update_time VARCHAR(32) NULL, create_time VARCHAR(32) NULL) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS dd_good_monitor_info (id INT AUTO_INCREMENT PRIMARY KEY, good_id VARCHAR(64) NULL, good_name VARCHAR(255) NULL, good_num INT DEFAULT 0, update_time VARCHAR(32) NULL, create_time VARCHAR(32) NULL) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS dy_login (id INT PRIMARY KEY, name VARCHAR(255) NULL, `value` LONGTEXT NULL) ENGINE=InnoDB");
			Query("CREATE TABLE IF NOT EXISTS users (id INT AUTO_INCREMENT PRIMARY KEY, username VARCHAR(64) NOT NULL UNIQUE, password_hash VARCHAR(128) NOT NULL, role VARCHAR(16) DEFAULT 'user', created_at VARCHAR(32) NULL) ENGINE=InnoDB");
			Query("INSERT IGNORE INTO users (username, password_hash, role) SELECT 'admin', '43d437bffab2951bfef9c0497826069daa4a16cbd5f9093dc379878ec070965f', 'admin' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM users)");
		}
		else
		{
			Query("CREATE TABLE IF NOT EXISTS dd_good_monitor (id INTEGER PRIMARY KEY AUTOINCREMENT, good_id TEXT, good_name TEXT, good_num INTEGER DEFAULT 0, good_state INTEGER DEFAULT 1, update_time TEXT, create_time TEXT)");
			Query("CREATE TABLE IF NOT EXISTS dd_good_monitor_info (id INTEGER PRIMARY KEY AUTOINCREMENT, good_id TEXT, good_name TEXT, good_num INTEGER DEFAULT 0, update_time TEXT, create_time TEXT)");
			Query("CREATE TABLE IF NOT EXISTS dd_shop (id INTEGER PRIMARY KEY AUTOINCREMENT, shop_id TEXT, uuid TEXT, name TEXT, mobile TEXT, staple TEXT, choose_state INTEGER DEFAULT 0, create_time TEXT)");
			Query("CREATE TABLE IF NOT EXISTS dy_login (id INTEGER PRIMARY KEY, name TEXT, value TEXT)");
			Query("CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT UNIQUE, password_hash TEXT, role TEXT DEFAULT 'user', created_at TEXT)");
			Query("INSERT INTO users (username, password_hash, role) SELECT 'admin', '43d437bffab2951bfef9c0497826069daa4a16cbd5f9093dc379878ec070965f', 'admin' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM users)");
		}
	}

	public static DataTable Query(string sql)
	{
		DataTable dataTable = new DataTable();
		try
		{
			using IDbCommand cmd = GetConn().CreateCommand();
			cmd.CommandText = sql;
			Fill(dataTable, cmd);
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
			using (IDbCommand cmd = GetConn().CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();
			}
			if (getId)
			{
				string lastIdSql = string.Equals(DbType, "MySql", StringComparison.OrdinalIgnoreCase)
					? "SELECT LAST_INSERT_ID()"
					: "SELECT last_insert_rowid()";
				using IDbCommand sQLiteCommand = GetConn().CreateCommand();
				sQLiteCommand.CommandText = lastIdSql;
				return Convert.ToString(sQLiteCommand.ExecuteScalar());
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
