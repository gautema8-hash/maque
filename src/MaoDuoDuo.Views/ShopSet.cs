using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using CefSharp;
using CefSharp.Wpf;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using MDD.Models.Enum;
using MaoDuoDuo.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZdfFlatUI;

namespace MaoDuoDuo.Views;

public partial class ShopSet : UserControl
{
	private ChromiumWebBrowser pdd_browser;

	public Window pddWindow;

	private Timer stTimer;

	private Timer gqTimer;

	private int count = 0;

	public int update_time = 0;

	private bool login_check_flag = false;

	private string UUID = "";

	public List<StoreInfo> StoreList;

	public List<Window> AllWindow;

	public bool update_cookie = false;












	public ShopSet()
	{
		InitializeComponent();
		initData();
	}

	private void initData()
	{
		update_time = 10800;
		Timer timer = new Timer();
		timer = new Timer(1000.0);
		timer.Elapsed += UpdateTimer;
		timer.AutoReset = true;
		timer.Enabled = true;
		timer.Start();
		StoreList = ShopSetBLL.GetShopInfoByName("全部", "");
		if (!SelectShop())
		{
			PddShopList.ItemsSource = null;
			PddShopList.ItemsSource = StoreList;
		}
		IEnumerable<IGrouping<string, StoreInfo>> enumerable = from it in StoreList
			group it by it.Staple;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("全部", "全部");
		foreach (IGrouping<string, StoreInfo> item in enumerable)
		{
			dictionary.Add(item.Key, item.Key);
		}
		shopType.ItemsSource = dictionary;
		shopType.DisplayMemberPath = "Value";
		shopType.SelectedValuePath = "Key";
		shopType.SelectedIndex = 0;
		string text = AppDomain.CurrentDomain.BaseDirectory + "cache/pddshop";
		if (StoreList.Count == 0)
		{
			if (Directory.Exists(text))
			{
				try
				{
					Directory.Delete(text, recursive: true);
				}
				catch (IOException e)
				{
					LogHelper.Error("删除所有店铺缓存发生异常：" + text, e);
				}
			}
		}
		else
		{
			if (!Directory.Exists(text))
			{
				return;
			}
			string[] directories = Directory.GetDirectories(text);
			string[] array = directories;
			foreach (string text2 in array)
			{
				string fName = new DirectoryInfo(text2).Name;
				List<StoreInfo> list = StoreList.Where((StoreInfo it) => it.UUID == fName).ToList();
				if (list.Count == 0)
				{
					try
					{
						Directory.Delete(text2, recursive: true);
					}
					catch (IOException e2)
					{
						LogHelper.Error("删除店铺缓存发生异常：" + text2, e2);
					}
				}
			}
		}
	}

	private void UpdateTimer(object sender, ElapsedEventArgs e)
	{
		if (update_time > 0)
		{
			update_time--;
		}
		else
		{
			update_time = 10800;
			UpdateCookie();
		}
		int num = update_time;
		string h = "00";
		string i = "00";
		string s = "00";
		if (num / 3600 > 0)
		{
			h = (num / 3600).ToString().PadLeft(2, '0');
			num -= num / 3600 * 3600;
		}
		if (num / 60 > 0)
		{
			i = (num / 60).ToString().PadLeft(2, '0');
			num -= num / 60 * 60;
		}
		s = num.ToString().PadLeft(2, '0');
		base.Dispatcher.Invoke(delegate
		{
			txt_time.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace('-', '/');
			txt_updateTime.Text = h + "时" + i + "分" + s + "秒后自动登录";
		});
	}

	private void UpdateLog(string txt)
	{
		base.Dispatcher.Invoke(delegate
		{
			Run run = txt_log;
			run.Text = run.Text + txt + "\n";
			myRichTextBox.Focus();
			myRichTextBox.CaretPosition = myRichTextBox.Document.ContentEnd;
			myRichTextBox.ScrollToEnd();
		});
	}

	private void AddPddWindow(string text, string url)
	{
		RequestContextSettings settings = new RequestContextSettings
		{
			CachePath = AppDomain.CurrentDomain.BaseDirectory + "cache/pddshop/" + text,
			PersistUserPreferences = true,
			PersistSessionCookies = true
		};
		RequestContext requestContext = new RequestContext(settings);
		pdd_browser = new ChromiumWebBrowser();
		pdd_browser.RequestContext = requestContext;
		pdd_browser.JavascriptObjectRepository.Register("colynJsObj", new CallbackObjectForJs(), isAsync: false);
		pddWindow = new Window
		{
			Title = "My Browser",
			Content = pdd_browser,
			Width = 800.0,
			Height = 500.0
		};
		pdd_browser.Address = url;
		pddWindow.Show();
		pddWindow.Visibility = Visibility.Collapsed;
	}

	private void GetQRCode(object sender, ElapsedEventArgs e)
	{
		count++;
		if (count >= 10)
		{
			count = 0;
			stTimer.Enabled = false;
			base.Dispatcher.Invoke(delegate
			{
				txt_State.Content = "获取失败";
			});
			return;
		}
		try
		{
			string text = "";
			Task<JavascriptResponse> task = pdd_browser.EvaluateScriptAsync("document.getElementsByTagName('canvas')[0].toDataURL()");
			task.Wait();
			if (task.Result.Result != null)
			{
				text = task.Result.Result.ToString();
			}
			if (string.IsNullOrEmpty(text) || !text.Contains("data:image/png;base64"))
			{
				stTimer.Enabled = true;
				return;
			}
			count = 0;
			stTimer.Enabled = false;
			gqTimer = new Timer(120000.0);
			gqTimer.AutoReset = false;
			gqTimer.Elapsed += QRCodeExpire;
			gqTimer.Enabled = true;
			text = text.Replace("data:image/png;base64,", "");
			byte[] buffer = Convert.FromBase64String(text);
			MemoryStream ms = new MemoryStream(buffer);
			ImageSourceConverter imageSourceConverter = new ImageSourceConverter();
			base.Dispatcher.Invoke(delegate
			{
				txt_State.Content = "获取二维码成功";
				img_Login.Source = (ImageSource)imageSourceConverter.ConvertFrom(ms);
			});
			login_check_flag = true;
			stTimer = new Timer(1000.0);
			stTimer.AutoReset = false;
			stTimer.Elapsed += GetShopInfo;
			stTimer.Enabled = true;
		}
		catch (Exception ex)
		{
			stTimer.Enabled = true;
			string message = ex.Message;
		}
	}

	private void QRCodeExpire(object sender, ElapsedEventArgs e)
	{
		gqTimer.Enabled = false;
		base.Dispatcher.Invoke(delegate
		{
			login_check_flag = false;
			count = 0;
			txt_State.Content = "二维码失效";
		});
	}

	private void GetShopInfo(object sender, ElapsedEventArgs e)
	{
		if (!login_check_flag)
		{
			return;
		}
		try
		{
			string text = "";
			Task<JavascriptResponse> task = pdd_browser.EvaluateScriptAsync("document.getElementsByTagName('html')[0].innerHTML;");
			task.Wait();
			if (task.Result.Result != null)
			{
				text = task.Result.Result.ToString();
			}
			if (!text.Contains("还没有店铺？") && !text.Contains("立即0元开店"))
			{
				string text2 = "";
				Task<JavascriptResponse> task2 = pdd_browser.EvaluateScriptAsync("localStorage.getItem('new_userinfo');");
				task2.Wait();
				if (task2.Result.Success && task2.Result.Result != null)
				{
					text2 = task2.Result.Result.ToString();
					JObject jObject = (JObject)JsonConvert.DeserializeObject(text2);
					if (!string.IsNullOrEmpty(jObject["id"].ToString()))
					{
						StoreInfo info = new StoreInfo();
						info.ChooseState = 0;
						info.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						info.Num = StoreList.Count() + 1;
						info.UUID = UUID;
						info.Mobile = jObject["mobile"].ToString();
						JObject jObject2 = (JObject)JsonConvert.DeserializeObject(jObject["mall"].ToString());
						info.ShopId = jObject2["mall_id"].ToString();
						info.Name = jObject2["mall_name"].ToString();
						info.Status = jObject2["status"].ToString();
						info.Staple = jObject2["staple"][0].ToString();
						stTimer.Enabled = false;
						StoreInfo shopInfo = ShopSetBLL.GetShopInfo(info.ShopId);
						if (shopInfo.Id > 0)
						{
							string text3 = AppDomain.CurrentDomain.BaseDirectory + "cache/pddshop/" + shopInfo.UUID;
							if (Directory.Exists(text3))
							{
								try
								{
									Directory.Delete(text3, recursive: true);
								}
								catch (IOException e2)
								{
									LogHelper.Error("删除店铺缓存发生异常：" + text3, e2);
								}
							}
							UpdateLog(info.Name + "(" + info.ShopId + "):店铺已经登录");
						}
						else
						{
							UpdateLog(info.Name + "(" + info.ShopId + "):店铺登录成功");
						}
						StoreInfo storeInfo = StoreList.Where((StoreInfo it) => it.ShopId == info.ShopId).FirstOrDefault();
						if (storeInfo != null)
						{
							StoreList.Where((StoreInfo it) => it.ShopId == info.ShopId).ToDictionary((StoreInfo it) => it.UUID = UUID);
						}
						else
						{
							StoreList.Add(info);
						}
						base.Dispatcher.Invoke(delegate
						{
							PddShopList.ItemsSource = null;
							PddShopList.ItemsSource = StoreList;
							pddWindow.Close();
						});
						return;
					}
				}
			}
			stTimer.Enabled = true;
		}
		catch (Exception)
		{
			stTimer.Enabled = true;
		}
	}

	private void btnGetQRCode_Click(object sender, RoutedEventArgs e)
	{
		if (count == 0)
		{
			UUID = Guid.NewGuid().ToString("N");
			AddPddWindow(UUID, "https://mms.pinduoduo.com/login/");
			txt_State.Content = "获取中";
			stTimer = new Timer(2000.0);
			stTimer.AutoReset = false;
			stTimer.Elapsed += GetQRCode;
			stTimer.Enabled = true;
		}
	}

	private void btnFind_Click(object sender, RoutedEventArgs e)
	{
		string text = txt_shopName.Text;
		if (!string.IsNullOrEmpty(text))
		{
			StoreList = ShopSetBLL.GetShopInfoByName(shopType.SelectedValue.ToString(), text);
			PddShopList.ItemsSource = null;
			PddShopList.ItemsSource = StoreList;
		}
	}

	private void btnChoose_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		int chooseState = 0;
		if (button.Content.ToString() == "全选")
		{
			button.Content = "取消全选";
			chooseState = 1;
		}
		else
		{
			button.Content = "全选";
		}
		foreach (StoreInfo store in StoreList)
		{
			store.ChooseState = chooseState;
			ShopSetBLL.UpdateShopState(store);
		}
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
	}

	private void boxShop_Checked(object sender, RoutedEventArgs e)
	{
		CheckBox checkBox = sender as CheckBox;
		StoreInfo info = checkBox.DataContext as StoreInfo;
		ShopSetBLL.UpdateShopState(info);
	}

	private void btnClear_Click(object sender, RoutedEventArgs e)
	{
		if (SysMessage.MessageShow(Window.GetWindow(this), "你确定要清空所有店铺吗？会删除所有的店铺登录信息！", "清空操作", MessageBoxButton.OKCancel) != MessageEnum.OK)
		{
			return;
		}
		StoreList = new List<StoreInfo>();
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
		ShopSetBLL.ClearInfo();
		string text = AppDomain.CurrentDomain.BaseDirectory + "cache/pddshop";
		if (!Directory.Exists(text))
		{
			return;
		}
		try
		{
			Directory.Delete(text, recursive: true);
		}
		catch (IOException e2)
		{
			LogHelper.Error("删除所有店铺缓存发生异常：" + text, e2);
		}
	}

	private void btnSelect_Click(object sender, RoutedEventArgs e)
	{
		if (!SelectShop())
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先选择店铺！", "提示", MessageBoxButton.OK);
		}
	}

	private bool SelectShop()
	{
		List<StoreInfo> list = StoreList.Where((StoreInfo it) => it.ChooseState == 1).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		bool isEditable = true;
		if (txt_select.Content.ToString() == "选中使用店铺")
		{
			txt_select.Content = "取消选中使用店铺";
			isEditable = false;
			SysCommon.SelectStore = list;
		}
		else
		{
			txt_select.Content = "选中使用店铺";
			SysCommon.SelectStore = new List<StoreInfo>();
		}
		foreach (StoreInfo store in StoreList)
		{
			store.IsEditable = isEditable;
		}
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
		return true;
	}

	private void btnUpdate_Click(object sender, RoutedEventArgs e)
	{
		UpdateCookie();
	}

	private void UpdateCookie()
	{
		if (update_cookie)
		{
			return;
		}
		update_cookie = true;
		AllWindow = new List<Window>();
		List<StoreInfo> list = new List<StoreInfo>();
		string url = "https://mms.pinduoduo.com/mallcenter/info/basic";
		foreach (StoreInfo item in list)
		{
			AddPddWindow(item.UUID, url);
			pddWindow.Visibility = Visibility.Visible;
			AllWindow.Add(pddWindow);
			UpdateLog("店铺:" + item.Name + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "刷新cookie成功");
		}
		UpdateLog(list.Count + "家店铺：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "刷新cookie成功");
		Timer timer = new Timer(5000.0);
		timer.AutoReset = false;
		timer.Elapsed += CloseWindow;
		timer.Enabled = true;
	}

	private void CloseWindow(object sender, ElapsedEventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			foreach (Window item in AllWindow)
			{
				try
				{
					item?.Close();
				}
				catch (Exception)
				{
				}
			}
		});
		update_cookie = false;
	}



}
