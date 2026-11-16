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
using MDD.DAL;
using System.Globalization;
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
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("全部", "全部");
		foreach (string preset in new string[7] { "食品饮料", "服饰鞋包", "数码家电", "美妆个护", "家居日用", "母婴玩具", "其他" })
		{
			dictionary[preset] = preset;
		}
		foreach (StoreInfo st in StoreList)
		{
			if (!string.IsNullOrEmpty(st.Staple) && !dictionary.ContainsKey(st.Staple))
			{
				dictionary[st.Staple] = st.Staple;
			}
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
						info.ChooseState = 1;
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
							// 重复登录：更新 Cookie 缓存目录 UUID 与手机号，保证持久化数据与缓存一致
							ShopSetDAL.UpdateStoreUUID(info.ShopId, info.UUID, info.Mobile);
							UpdateLog(info.Name + "(" + info.ShopId + "):店铺已经登录(登录信息已更新)");
						}
						else
						{
							// 首次登录：店铺持久化到数据库（否则点"查找店铺"会丢失）
							ShopSetDAL.AddStore(info);
							UpdateLog(info.Name + "(" + info.ShopId + "):店铺登录成功(已保存到数据库)");
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
		if (count != 0 || stTimer != null && stTimer.Enabled)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "正在获取二维码，请稍候（约20秒超时）", "提示", MessageBoxButton.OK);
			return;
		}
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
		string type = shopType.SelectedValue?.ToString() ?? "全部";
		StoreList = ShopSetBLL.GetShopInfoByName(type, text);
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
		UpdateLog("查找店铺[" + type + "]关键词[" + text + "]：共 " + StoreList.Count + " 家 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
	}

	private void btnChoose_Click(object sender, RoutedEventArgs e)
	{
		Button button = sender as Button;
		if (StoreList == null || StoreList.Count == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "当前没有店铺可操作，请先扫码添加店铺", "提示", MessageBoxButton.OK);
			return;
		}
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
		RefreshSelectStore();
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
	}

	private void boxShop_Checked(object sender, RoutedEventArgs e)
	{
		CheckBox checkBox = sender as CheckBox;
		if (checkBox?.DataContext is StoreInfo info)
		{
			info.ChooseState = checkBox.IsChecked.GetValueOrDefault() ? 1 : 0;
			ShopSetBLL.UpdateShopState(info);
			RefreshSelectStore();
		}
	}

	private void btnClear_Click(object sender, RoutedEventArgs e)
	{
		List<StoreInfo> chosen = (StoreList ?? new List<StoreInfo>())
			.Where((StoreInfo it) => it.ChooseState == 1).ToList();
		if (chosen.Count == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先勾选要删除的店铺", "提示", MessageBoxButton.OK);
			return;
		}
		if (SysMessage.MessageShow(Window.GetWindow(this), string.Format("确定删除勾选的 {0} 家店铺吗？将同时删除其登录缓存！", chosen.Count), "删除店铺", MessageBoxButton.OKCancel) != MessageEnum.OK)
		{
			return;
		}
		ShopSetDAL.DeleteStores(chosen.ConvertAll((StoreInfo it) => it.ShopId));
		foreach (StoreInfo item in chosen)
		{
			string text2 = AppDomain.CurrentDomain.BaseDirectory + "cache/pddshop/" + item.UUID;
			if (Directory.Exists(text2))
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
		foreach (StoreInfo item in chosen)
		{
			StoreList.Remove(item);
		}
		RefreshSelectStore();
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
		UpdateLog("已删除勾选店铺 " + chosen.Count + " 家 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
	}


	/// <summary>刷新使用中的店铺集合（勾选状态即选用状态）</summary>
	private void RefreshSelectStore()
	{
		SysCommon.SelectStore = (StoreList ?? new List<StoreInfo>())
			.Where((StoreInfo it) => it.ChooseState == 1).ToList();
	}

	private bool SelectShop()
	{
		if (StoreList == null || StoreList.Count == 0)
		{
			return false;
		}
		RefreshSelectStore();
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = StoreList;
		return SysCommon.SelectStore.Count > 0;
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
		List<StoreInfo> list = (SysCommon.SelectStore != null && SysCommon.SelectStore.Count > 0)
			? SysCommon.SelectStore
			: StoreList.Where((StoreInfo it) => it.ChooseState == 1).ToList();
		if (list.Count == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先勾选需要更新Cookie的店铺", "提示", MessageBoxButton.OK);
			return;
		}
		update_cookie = true;
		AllWindow = new List<Window>();
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

	/// <summary>int(0/1) 与 bool 转换（店铺勾选列绑定）</summary>
	public class IntToBoolConverter : System.Windows.Data.IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is int i && i == 1;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is bool b && b) ? 1 : 0;
		}
	}
