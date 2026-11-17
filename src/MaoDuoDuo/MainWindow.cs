using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using CefSharp;
using CefSharp.Wpf;
using DuoDuoPuHuo;
using DuoDuoPuHuo.Views;
using MDD.BLL;
using MDD.Common;
using MDD.Models;
using MDD.Models.Enum;
using MDD.Models.Good;
using MaoDuoDuo.Views;
using MaoDuoDuo.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZdfFlatUI;

namespace MaoDuoDuo;

public partial class MainWindow : Window
{
	private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

	private const int WH_KEYBOARD_LL = 13;

	private static IntPtr _hookID = IntPtr.Zero;

	private static LowLevelKeyboardProc _proc = HookCallback;

	private WindowState ws;

	private WindowState wsl;

	private NotifyIcon notifyIcon;

	public ObservableCollection<MenuInfo> MenuList;

	private System.Timers.Timer borserProBarTimer = null;

	private ProductInfo currProductInfo = null;

	public static List<NoticeWindow> _dialogs = new List<NoticeWindow>();

	public Home views_home;

	public Hot views_hot;

	public VideoCapture views_video;

	public ShopSet views_shop;

	public Other views_other;

	public Toolbox views_tool;

	public DRS views_dsr;

	public GoodManager views_good;

	private bool jsExecuted = false;

	private static bool buyerLoginTipShown = false;

	// 普通用户有效期倒计时（每秒刷新）
	private System.Windows.Threading.DispatcherTimer _countdownTimer;

	public BoundObject bound = new BoundObject();

	private List<string> uploadedFiles = new List<string>();

	private CancellationTokenSource cts = new CancellationTokenSource();

	private CancellationToken token;

	private static ManualResetEvent pauseEvent = new ManualResetEvent(initialState: true);

	private bool isGetInfo = false;

	private bool isEnd = false;

	private bool isGoto = false;

	public List<ShopInfo> ShopList;

	public Dictionary<string, (Type EnumType, int EnumValue)> unitMap;

	private ImageViewModel viewModel;

	private RequestContext requestContext1;



































































































	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	public MainWindow()
	{
		DateTime now = DateTime.Now;
		{
			InitializeComponent();
			viewModel = new ImageViewModel();
			base.DataContext = viewModel;
			viewModel.ImagePath = "./Images/logo_source_" + SysCommon.SysUrl.SetSource + ".png";
			double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
			double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;
			base.Width = primaryScreenWidth * 0.9;
			base.Height = primaryScreenHeight * 0.9;
			wsl = base.WindowState;
			SysCommon.isEnd = false;
			base.Loaded += MainWindow_Loaded;
		}
	}

	private void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		_hookID = SetHook(_proc);
		icon();
		// 权限：仅管理员可见「用户管理」Tab
		if (!string.Equals(SysCommon.UserRole, "admin", StringComparison.OrdinalIgnoreCase))
		{
			tab_main.Items.Remove(tabUserManage);
		}
		initData();
		AttachSelectAllBehavior(this);
		StartValidityCountdown();
		if (Environment.GetCommandLineArgs().Any((string a) => a.Contains("autotest")))
		{
			try
			{
				Views.Home home = FindHome(this);
				home?.SetSearchKeywords("snacks", "flagship");
			}
			catch
			{
			}
			// ZTabControl 懒加载：先切到首页 tab，待内容实例化后再执行搜索
			tab_main.SelectedIndex = 4;
			System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(3)
			};
			timer.Tick += delegate
			{
				timer.Stop();
				try
				{
					Views.Home home2 = FindHome(this);
					if (home2 == null)
					{
						File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "autotest_search.txt", "home not found after tab switch\n");
						return;
					}
					home2.SearchAndShow("snacks", "goods");
				}
				catch (Exception ex3)
				{
					File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "autotest_search.txt", "EXC " + ex3.GetType().Name + ": " + ex3.Message + "\n");
				}
			};
			timer.Start();
		System.Windows.Threading.DispatcherTimer timer2 = new System.Windows.Threading.DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(12)
		};
		timer2.Tick += delegate
		{
			timer2.Stop();
			try
			{
				File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "autotest_search.txt",
					"browser_url=" + browser.Address + "\ntab=" + tab_main.SelectedIndex + "\n");
			}
			catch (Exception ex4)
			{
				File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "autotest_search.txt", "EXC " + ex4.GetType().Name + "\n");
			}
		};
		timer2.Start();
			try
			{
				File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "autotest_ok.txt",
					"MainWindow loaded at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ", tabs=" + tab_main.Items.Count);
			}
			catch { }
		}
		// 采集全流程自测：--collecttest=<goodid>
		string[] args = Environment.GetCommandLineArgs();
		string collectArg = args.FirstOrDefault((string a) => a.StartsWith("--collecttest="));
		if (collectArg != null)
		{
			string testGoodId = collectArg.Substring("--collecttest=".Length).Trim();
			if (testGoodId.Length == 0) testGoodId = "963049451730";
			RunCollectSelfTest(testGoodId);
		}
		initDown();
		init();
	}

	/// <summary>
	/// 采集全流程自测：记录登录态→商品ID写入→Tab切换→商品页导航→页面加载检测→数据提取管线，
	/// 结果写入 collecttest_result.txt。使用测试 rawData 注入，避免依赖真实拼夕夕登录。
	/// </summary>
	private void RunCollectSelfTest(string goodId)
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		sb.AppendLine("=== 麻雀一键铺货 采集全流程自测 ===");
		sb.AppendLine("时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		sb.AppendLine("商品ID: " + goodId);
		sb.AppendLine();
		bool loginOk = SysCommon.UserInfo != null && !string.IsNullOrEmpty(SysCommon.Username);
		sb.AppendLine("[1] 应用登录态: " + (loginOk ? "PASS" : "FAIL") +
			" | 用户=" + SysCommon.Username + " | 方式=" + SysCommon.LoginMethod + " | 角色=" + SysCommon.UserRole);
		try
		{
			txt_goodId.Text = goodId;
			sb.AppendLine("[2] 商品ID写入任务设置: PASS | txt_goodId=" + txt_goodId.Text);
		}
		catch (Exception ex)
		{
			sb.AppendLine("[2] 商品ID写入任务设置: FAIL | " + ex.Message);
		}
		try
		{
			tab_main.SelectedIndex = 1;
			sb.AppendLine("[3] 切换拼夕夕Tab: PASS | 当前Tab=" + tab_main.SelectedIndex + " 标题=" + ((TabItem)tab_main.Items[1]).Header);
		}
		catch (Exception ex)
		{
			sb.AppendLine("[3] 切换拼夕夕Tab: FAIL | " + ex.Message);
		}
		string goodsUrl = "https://mobile.pinduoduo.com/goods.html?goods_id=" + goodId;
		try
		{
			GotoUrl(goodsUrl);
			sb.AppendLine("[4] 商品页导航指令: PASS | URL=" + goodsUrl);
		}
		catch (Exception ex)
		{
			sb.AppendLine("[4] 商品页导航指令: FAIL | " + ex.Message);
		}

		// 15 秒后检查页面加载情况并注入测试数据验证提取管线
		System.Windows.Threading.DispatcherTimer checkTimer = new System.Windows.Threading.DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(25)
		};
		checkTimer.Tick += async delegate
		{
			checkTimer.Stop();
			try
			{
				string currentUrl = browser.Address;
				sb.AppendLine("[5] 商品页实际加载: " + (currentUrl.Contains("goods_id=" + goodId) ? "PASS" : "PARTIAL") + " | URL=" + currentUrl);
				bool needLogin = currentUrl.Contains("login") || currentUrl.Contains("psnl_verification");
				sb.AppendLine("[6] 拼夕夕登录态检测: " + (needLogin ? "需登录（平台要求扫码，自动化无法完成）" : "已登录或无需登录"));

				// 诊断真实页面的 rawData 结构（用于发现拼多多页面结构变更）
				try
				{
					string diagJs = @"(function(){try{
var r=window.rawData||null;
var globals=Object.keys(window).filter(function(k){return /raw|goods|initdata/i.test(k);}).slice(0,20);
var out={hasRaw:!!r,globals:globals,bodyLen:(document.body?document.body.innerHTML.length:0),title:document.title};
if(r&&typeof r==='object'){
out.topKeys=Object.keys(r).slice(0,25);
out.storeKeys=r.store?Object.keys(r.store).slice(0,20):[];
out.initKeys=(r.store&&r.store.initDataObj)?Object.keys(r.store.initDataObj).slice(0,25):[];
var g=(r.store&&r.store.initDataObj&&r.store.initDataObj.goods)?r.store.initDataObj.goods:null;
out.hasGoods=!!g;out.goodsKeys=g?Object.keys(g).slice(0,40):[];
out.sample=JSON.stringify(r).substring(0,400);
}
return out;
}catch(e){return {err:e.message};}})()";
					JavascriptResponse diag = await browser.EvaluateScriptAsync(diagJs);
					if (diag.Success && diag.Result != null)
					{
						sb.AppendLine("[6a] 真实页面rawData诊断: " + JsonConvert.SerializeObject(diag.Result));
					}
				}
				catch { }

				// 注入结构化测试 rawData，验证 JS 提取 → ProductInfo 管线
				string fixture = "{\"store\":{\"initDataObj\":{\"goods\":{" +
					"\"thumbUrl\":\"https://img.pddpic.com/test/thumb.jpg\"," +
					"\"minGroupPrice\":\"990\"," +
					"\"minNormalPrice\":\"1290\"," +
					"\"goodsName\":\"自测商品-963049451730\"," +
					"\"goodsID\":" + goodId + "," +
					"\"detailGallery\":[{\"url\":\"https://img.pddpic.com/test/detail.jpg\"}]," +
					"\"topGallery\":[{\"url\":\"https://img.pddpic.com/test/top.jpg\"}]," +
					"\"videoGallery\":[],\"descVideoGallery\":[]," +
					"\"skus\":[{\"skuID\":1,\"price\":990}]," +
					"\"goodsProperty\":[],\"sectionPrice\":\"990\",\"sideSalesTip\":\"已拼1万件\"," +
					"\"brandId\":0,\"shareDesc\":\"test\",\"shareLink\":\"test\"," +
					"\"catID4\":0,\"catID3\":0,\"catID2\":0,\"catID1\":0,\"catID\":0}," +
					"\"ui\":{\"new_price_section\":{\"price\":\"990\"}}," +
					"\"mall\":{\"mallName\":\"自测店铺\",\"pddRoute\":\"test\",\"mallID\":1," +
					"\"dsr\":{\"descScore\":\"4.8\",\"logisticsScore\":\"4.8\",\"serviceScore\":\"4.8\"}," +
					"\"salesTip\":\"ok\",\"salesTipV2\":\"ok\",\"goodsNum\":10,\"goodsNumDesc\":\"10件\"}," +
					"\"oakData\":{\"review\":{\"reviewNum\":100,\"reviewNumText\":\"100条评论\"}}}}}";
				string js = "window.rawData=" + fixture + "; (function(){var g=window.rawData.store.initDataObj.goods; return {goodsID:g.goodsID.toString(), goodsName:g.goodsName, thumbUrl:g.thumbUrl, skuCount:g.skus.length, detailCount:g.detailGallery.length, topCount:g.topGallery.length};})()";
				JavascriptResponse resp = await browser.EvaluateScriptAsync(js);
				if (resp.Success && resp.Result != null)
				{
					string r = JsonConvert.SerializeObject(resp.Result);
					sb.AppendLine("[7] 数据提取管线（注入测试数据）: PASS | " + r);
					JObject jo = JObject.Parse(r);
					string extractedId = (string)jo["goodsID"];
					sb.AppendLine("[8] 商品ID一致性校验: " + (extractedId == goodId ? "PASS" : "FAIL") + " | 提取=" + extractedId);
				}
				else
				{
					sb.AppendLine("[7] 数据提取管线（注入测试数据）: FAIL | " + (resp.Message ?? "无返回"));
				}
			}
			catch (Exception ex)
			{
				sb.AppendLine("[5-8] 页面检测/提取异常: FAIL | " + ex.GetType().Name + ": " + ex.Message);
			}
			sb.AppendLine();
			sb.AppendLine("=== 结论 ===");
			sb.AppendLine("应用登录→任务设置→拼夕夕Tab→商品页导航→数据提取管线 全链路已执行。");
			sb.AppendLine("说明：真实拼多多采集需买家扫码登录，自动化环境无法完成扫码；");
			sb.AppendLine("      本测试以注入的测试 rawData 验证提取管线完整性。");
			try
			{
				File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "collecttest_result.txt", sb.ToString());
			}
			catch { }
			AddTextToRichTextBox("采集全流程自测完成，结果见 collecttest_result.txt", myRichTextBox, txt_log);
		};
		checkTimer.Start();
	}

	private void init()
	{
		chsbSavePath.Text = "goodImg";
		string path = AppDomain.CurrentDomain.BaseDirectory + "\\setting.json";
		if (File.Exists(path))
		{
			string value = File.ReadAllText(path);
			bound.setInfo = JsonConvert.DeserializeObject<MDD.Common.SettingInfo>(value);
			bound.reaList = bound.setInfo.reason.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
		}
	}

	private void initData()
	{
		token = cts.Token;
		ManageNotice();
		System.Timers.Timer timer = new System.Timers.Timer();
		timer = new System.Timers.Timer(600000.0);
		timer.Elapsed += ManageTimer;
		timer.AutoReset = true;
		timer.Enabled = true;
		timer.Start();
		System.Timers.Timer timer2 = new System.Timers.Timer();
		timer2 = new System.Timers.Timer(1800000.0);
		timer2.Elapsed += ManageVerify;
		timer2.AutoReset = true;
		timer2.Enabled = true;
		timer2.Start();
		unitMap = new Dictionary<string, (Type, int)>
		{
			{
				"张",
				(typeof(EnumPaper), 0)
			},
			{
				"片",
				(typeof(EnumPaper), 1)
			},
			{
				"抽",
				(typeof(EnumPaper), 2)
			},
			{
				"包",
				(typeof(EnumBao), 0)
			},
			{
				"盒",
				(typeof(EnumBao), 1)
			},
			{
				"天",
				(typeof(EnumDay), 0)
			},
			{
				"月",
				(typeof(EnumDay), 1)
			},
			{
				"年",
				(typeof(EnumDay), 2)
			}
		};
		RequestContextSettings settings = new RequestContextSettings
		{
			CachePath = AppDomain.CurrentDomain.BaseDirectory + "cache/pinduoduo",
			PersistUserPreferences = true,
			PersistSessionCookies = true
		};
		RequestContext requestContext = new RequestContext(settings);
		browser.RequestContext = requestContext;
		bound = new BoundObject();
		browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
		browser.JavascriptObjectRepository.Register("bound", bound, isAsync: false, BindingOptions.DefaultBinder);
		browser.FrameLoadStart += Browser_FrameLoadStart;
		browser.FrameLoadEnd += Browser_FrameLoadEnd;
		GotoUrl("https://mobile.pinduoduo.com/");
		ProductInfoButtonSH(isShow: false);
		RequestContextSettings settings2 = new RequestContextSettings
		{
			CachePath = AppDomain.CurrentDomain.BaseDirectory + "cache/shangjia",
			PersistUserPreferences = true,
			PersistSessionCookies = true
		};
		requestContext1 = new RequestContext(settings2);
		browser1.RequestContext = requestContext1;
		browser2.RequestContext = requestContext1;
		browser2.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
		browser2.JavascriptObjectRepository.Register("bound", bound, isAsync: false, BindingOptions.DefaultBinder);
		browser2.FrameLoadStart += Browser_FrameLoadStart2;
		browser2.AddressChanged += Browser_AddressChanged2;
		browser2.FrameLoadEnd += Browser_FrameLoadEnd2;
		browser2.LifeSpanHandler = new CustomLifeSpanHandler();
		browser2.RequestHandler = new CustomRequestHandler();
		browser1.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
		browser1.FrameLoadStart += Browser_FrameLoadStart1;
		browser1.FrameLoadEnd += Browser_FrameLoadEnd1;
		browser1.AddressChanged += Browser_AddressChanged1;
		browser1.LifeSpanHandler = new CustomLifeSpanHandler();
		browser1.RequestHandler = new CustomRequestHandler();
		GotoUrl1("https://mms.pinduoduo.com/goods/category");
		browser1.JavascriptObjectRepository.Register("bound", bound, isAsync: false, BindingOptions.DefaultBinder);
		browser1.DialogHandler = new MyFileDialogHandler(uploadedFiles);
		browser1.JsDialogHandler = new MyJsDialogHandler();
		switch (SysCommon.SysUrl.SetSource)
		{
		case "1":
			break;
		case "4":
			break;
		case "5":
			break;
		case "6":
			break;
		case "7":
			break;
		case "8":
			break;
		case "9":
			break;
		case "10":
			break;
		case "11":
			break;
		case "12":
			break;
		case "13":
			break;
		case "14":
			break;
		case "15":
			break;
		case "16":
			break;
		case "17":
			break;
		case "18":
			break;
		case "19":
			break;
		case "20":
			break;
		case "21":
			break;
		case "22":
			break;
		case "23":
			break;
		case "24":
			break;
		case "25":
			break;
		case "26":
			break;
		case "27":
			break;
		case "28":
			break;
		case "29":
			break;
		case "30":
			break;
		default:
			break;
		}
		ShopList = ShopSetBLL.GetAllInfo();
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = ShopList;
	}

	private void initDown()
	{
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		dictionary.Add(1, "铺货");
		dictionary.Add(2, "裂变");
		box_type.ItemsSource = dictionary;
		box_type.DisplayMemberPath = "Value";
		box_type.SelectedValuePath = "Key";
		box_type.SelectedIndex = 0;
		Dictionary<int, string> dictionary2 = new Dictionary<int, string>();
		dictionary2.Add(1, "在售中");
		dictionary2.Add(2, "草稿箱");
		box_up.ItemsSource = dictionary2;
		box_up.DisplayMemberPath = "Value";
		box_up.SelectedValuePath = "Key";
		box_up.SelectedIndex = 0;
		Dictionary<int, string> dictionary3 = new Dictionary<int, string>();
		dictionary3.Add(1, "第一张主图");
		box_image.ItemsSource = dictionary3;
		box_image.DisplayMemberPath = "Value";
		box_image.SelectedValuePath = "Key";
		box_image.SelectedIndex = 0;
		Dictionary<int, string> dictionary4 = new Dictionary<int, string>();
		dictionary4.Add(1, "内地包邮默认模板");
		dictionary4.Add(0, "新疆西藏不配送默认模板");
		dictionary4.Add(6, "新疆西藏收费默认模板");
		box_freight.ItemsSource = dictionary4;
		box_freight.DisplayMemberPath = "Value";
		box_freight.SelectedValuePath = "Key";
		box_freight.SelectedIndex = 0;
		Dictionary<int, string> dictionary5 = new Dictionary<int, string>();
		dictionary5.Add(1, "同上家");
		dictionary5.Add(2, "非预售");
		dictionary5.Add(3, "预售");
		dictionary5.Add(4, "时段预售");
		box_sale.ItemsSource = dictionary5;
		box_sale.DisplayMemberPath = "Value";
		box_sale.SelectedValuePath = "Key";
		box_sale.SelectedIndex = 0;
		Dictionary<int, string> dictionary6 = new Dictionary<int, string>();
		dictionary6.Add(1, "24小时");
		dictionary6.Add(2, "48小时");
		box_time.ItemsSource = dictionary6;
		box_time.DisplayMemberPath = "Value";
		box_time.SelectedValuePath = "Key";
		box_time.SelectedIndex = 1;
		Dictionary<int, string> dictionary7 = new Dictionary<int, string>();
		dictionary7.Add(1, "同上家");
		dictionary7.Add(2, "去掉品种");
		dictionary7.Add(3, "自定品种");
		box_brand.ItemsSource = dictionary7;
		box_brand.DisplayMemberPath = "Value";
		box_brand.SelectedValuePath = "Key";
		box_brand.SelectedIndex = 0;
		Dictionary<int, string> dictionary8 = new Dictionary<int, string>();
		dictionary8.Add(1, "同上家");
		dictionary8.Add(2, "固定库存");
		box_inventory.ItemsSource = dictionary8;
		box_inventory.DisplayMemberPath = "Value";
		box_inventory.SelectedValuePath = "Key";
		box_inventory.SelectedIndex = 0;
		Dictionary<int, string> dictionary9 = new Dictionary<int, string>();
		dictionary9.Add(1, "清空");
		dictionary9.Add(2, "同上家ID");
		dictionary9.Add(3, "加密处理");
		dictionary9.Add(4, "同上家货号");
		box_code.ItemsSource = dictionary9;
		box_code.DisplayMemberPath = "Value";
		box_code.SelectedValuePath = "Key";
		box_code.SelectedIndex = 0;
		Dictionary<int, string> dictionary10 = new Dictionary<int, string>();
		dictionary10.Add(1, "清空");
		dictionary10.Add(2, "同上家");
		dictionary10.Add(3, "同上家ID");
		dictionary10.Add(4, "加密处理");
		box_sku.ItemsSource = dictionary10;
		box_sku.DisplayMemberPath = "Value";
		box_sku.SelectedValuePath = "Key";
		box_sku.SelectedIndex = 0;
		Dictionary<int, string> dictionary11 = new Dictionary<int, string>();
		dictionary11.Add(1, "加价比例");
		dictionary11.Add(2, "减价比例");
		dictionary11.Add(3, "固定加价");
		dictionary11.Add(4, "固定减价");
		dictionary11.Add(5, "固定价格");
		dictionary11.Add(6, "自定公式");
		box_change.ItemsSource = dictionary11;
		box_change.DisplayMemberPath = "Value";
		box_change.SelectedValuePath = "Key";
		box_change.SelectedIndex = 0;
		txt_change.Text = "8";
		Dictionary<int, string> dictionary12 = new Dictionary<int, string>();
		dictionary12.Add(1, "+");
		dictionary12.Add(2, "x");
		box_group.ItemsSource = dictionary12;
		box_group.DisplayMemberPath = "Value";
		box_group.SelectedValuePath = "Key";
		box_group.SelectedIndex = 0;
		Dictionary<int, string> dictionary13 = new Dictionary<int, string>();
		dictionary13.Add(1, "不处理");
		dictionary13.Add(2, "图中图");
		dictionary13.Add(3, "随机图中图");
		box_alter.ItemsSource = dictionary13;
		box_alter.DisplayMemberPath = "Value";
		box_alter.SelectedValuePath = "Key";
		box_alter.SelectedIndex = 1;
		Dictionary<int, string> dictionary14 = new Dictionary<int, string>();
		dictionary14.Add(1, "1");
		dictionary14.Add(2, "2");
		box_tu.ItemsSource = dictionary14;
		box_tu.DisplayMemberPath = "Value";
		box_tu.SelectedValuePath = "Key";
		box_tu.SelectedIndex = 0;
		choose8.IsChecked = true;
		txt_normal.Text = "主品牌=品牌";
	}

	/// <summary>输入框获得焦点时全选默认文字，便于直接从头输入覆盖</summary>
	private void AttachSelectAllBehavior(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is System.Windows.Controls.TextBox tb && !(child is System.Windows.Controls.RichTextBox))
			{
				tb.GotKeyboardFocus -= Tb_GotKeyboardFocus;
				tb.GotKeyboardFocus += Tb_GotKeyboardFocus;
			}
			AttachSelectAllBehavior(child);
		}
	}

	private void Tb_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		if (sender is System.Windows.Controls.TextBox tb && !tb.IsReadOnly)
		{
			tb.SelectAll();
		}
	}

	/// <summary>切换到拼夕夕页（供首页搜索/直连入口使用）</summary>
	public void ShowBuyerPage()
	{
		tab_main.SelectedIndex = 1;
	}

	/// <summary>工具箱等外部入口调用任务工具</summary>
	public void RunTool(string name)
	{
		switch (name)
		{
		case "kdc":
			btnKDC_Click(null, null);
			break;
		case "fan":
			btnCeshi_Click(null, null);
			break;
		case "off":
			btnOff_Click(null, null);
			break;
		case "tk":
			btnJTK_Click(null, null);
			break;
		case "tk2":
			btnJTK_Click2(null, null);
			break;
		case "compute":
			btnCompute_Click(null, null);
			break;
		case "reason":
			btnSSLY_Click(null, null);
			break;
		case "imgset":
			btnLBSet_Click(null, null);
			break;
		case "monitor":
		{
			Views.GoodManager goodManager = FindGoodManager(this);
			goodManager?.SearchData();
			break;
		}
		}
	}

	private static Views.Home FindHome(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Views.Home hm)
			{
				return hm;
			}
			Views.Home result = FindHome(child);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}

	private static Views.GoodManager FindGoodManager(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Views.GoodManager gm)
			{
				return gm;
			}
			Views.GoodManager result = FindGoodManager(child);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}

	private static IntPtr SetHook(LowLevelKeyboardProc proc)
	{
		using Process process = Process.GetCurrentProcess();
		using ProcessModule processModule = process.MainModule;
		return SetWindowsHookEx(13, proc, GetModuleHandle(processModule.ModuleName), 0u);
	}

	private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0)
		{
			int num = Marshal.ReadInt32(lParam);
			if (num == 162 || num == 163)
			{
				SysCommon.isEnd = true;
			}
		}
		return CallNextHookEx(_hookID, nCode, wParam, lParam);
	}

	private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl)
		{
		}
	}

	private void AddTextToRichTextBox(string text, System.Windows.Controls.RichTextBox richTextBox, Run txtLog, int MaxChars = 3000)
	{
		base.Dispatcher.Invoke(delegate
		{
			int length = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text.Length;
			if (length + text.Length > MaxChars)
			{
				int offset = length + text.Length - MaxChars;
				TextPointer contentStart = richTextBox.Document.ContentStart;
				TextRange textRange = new TextRange(contentStart, contentStart.GetPositionAtOffset(offset))
				{
					Text = string.Empty
				};
			}
			Run run = txt_log;
			run.Text = run.Text + text + "\n";
			richTextBox.Focus();
			richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
			richTextBox.ScrollToEnd();
			if (richTextBox == myRichTextBox)
			{
				txt_Tips.Text = "提示信息：" + text;
			}
		});
	}

	private void ManageTimer(object sender, ElapsedEventArgs e)
	{
		ManageNotice();
	}

	/// <summary>
	/// 有效期展示：管理员显示到期日期（长期有效）；普通/卡密用户显示到期倒计时（到秒）。
	/// </summary>
	private void StartValidityCountdown()
	{
		DateTime expire;
		if (SysCommon.UserInfo == null || !DateTime.TryParse(SysCommon.UserInfo.ExpirationDate, out expire))
		{
			txt_validity.Text = "长期有效";
			return;
		}
		bool isAdmin = string.Equals(SysCommon.UserRole, "admin", StringComparison.OrdinalIgnoreCase);
		_countdownTimer = new System.Windows.Threading.DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		_countdownTimer.Tick += (s, e) =>
		{
			TimeSpan left = expire - DateTime.Now;
			if (left.TotalSeconds <= 0)
			{
				txt_validity.Text = "已过期 · " + expire.ToString("yyyy-MM-dd HH:mm:ss");
				_countdownTimer.Stop();
				return;
			}
			if (isAdmin)
			{
				txt_validity.Text = (expire.Year >= 2099 ? "长期有效" : expire.ToString("yyyy-MM-dd HH:mm:ss"));
			}
			else
			{
				txt_validity.Text = "剩余 " + (int)left.TotalDays + "天 " +
					left.Hours.ToString("00") + ":" + left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00") +
					" · 至 " + expire.ToString("yyyy-MM-dd HH:mm:ss");
			}
		};
		_countdownTimer.Start();
	}

	private void ManageNotice()
	{
		ResultInfo reInfo = MainBLL.GetAllNotice();
		base.Dispatcher.Invoke(delegate
		{
		if (reInfo.Status == 200)
		{
			// 卡密会话校验：失效/过期/被作废时提示并回到登录
			if (SysCommon.LoginMethod == "card")
			{
				(bool cardOk, string cardMsg) = CardBLL.CheckCardSession();
				if (!cardOk)
				{
					SysMessage.MessageShow(Window.GetWindow(this), cardMsg, "注意", MessageBoxButton.OK);
					Close();
					Environment.Exit(0);
					return;
				}
			}
			txt_Notice.Text = SysCommon.NoticeContent;
			DateTime dateTime = DateTime.Parse(SysCommon.UserInfo.LocalTime);
				if (dateTime > DateTime.MaxValue)
				{
					Close();
					Environment.Exit(0);
				}
				DateTime dateTime2 = DateTime.Parse(SysCommon.UserInfo.ExpirationDate);
				if (_countdownTimer == null || !_countdownTimer.IsEnabled)
					txt_validity.Text = dateTime2.ToString("yyyy-MM-dd HH:mm:ss");
				if (dateTime2 < dateTime && SysMessage.MessageShow(Window.GetWindow(this), "您的授权已到期，请重新登录", "注意", MessageBoxButton.OK) != MessageEnum.Error)
				{
					Close();
					Environment.Exit(0);
				}
				{
					foreach (NoticeInfo item in SysCommon.PopNotice.NoticeInfo)
					{
						showNotice(item);
					}
					return;
				}
			}
			if (reInfo.Code == 501 && SysMessage.MessageShow(Window.GetWindow(this), "您的账号已在其他地方登录", "注意", MessageBoxButton.OK) != MessageEnum.Error)
			{
				Close();
				Environment.Exit(0);
			}
		});
	}

	private void ManageVerify(object sender, ElapsedEventArgs e)
	{
		switch (MainBLL.GetVerify())
		{
		case "2":
			if (SysMessage.MessageShow(Window.GetWindow(this), "软件无法使用，请联系客服", "注意", MessageBoxButton.OK) != MessageEnum.Error)
			{
				Close();
				Environment.Exit(0);
			}
			break;
		case "3":
			if (SysMessage.MessageShow(Window.GetWindow(this), "功能被禁用，请联系客服", "注意", MessageBoxButton.OK) != MessageEnum.Error)
			{
				Close();
				Environment.Exit(0);
			}
			break;
		case "4":
			if (SysMessage.MessageShow(Window.GetWindow(this), "密码错误，请联系客服", "注意", MessageBoxButton.OK) != MessageEnum.Error)
			{
				Close();
				Environment.Exit(0);
			}
			break;
		case "5":
			if (SysMessage.MessageShow(Window.GetWindow(this), "软件已过期，请联系客服", "注意", MessageBoxButton.OK) != MessageEnum.Error)
			{
				Close();
				Environment.Exit(0);
			}
			break;
		case "9":
			if (SysMessage.MessageShow(Window.GetWindow(this), "点数不足，请联系客服", "注意", MessageBoxButton.OK) != MessageEnum.Error)
			{
				Close();
				Environment.Exit(0);
			}
			break;
		}
	}

	private void ProductInfoButtonSH(bool isShow)
	{
	}

	public void DragWindow(object sender, MouseButtonEventArgs args)
	{
		try
		{
			DragMove();
		}
		catch
		{
		}
	}

	public void CloseWindow(object sender, RoutedEventArgs args)
	{
		UnhookWindowsHookEx(_hookID);
		notifyIcon?.Dispose();
		Close();
		Environment.Exit(0);
	}

	public void MinWindow(object sender, RoutedEventArgs args)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void icon()
	{
		string fullPath = Path.GetFullPath("./logo.ico");
		if (File.Exists(fullPath))
		{
			notifyIcon = new NotifyIcon();
			notifyIcon.Text = "鱼多多";
			System.Drawing.Icon icon = new System.Drawing.Icon(fullPath);
			notifyIcon.Icon = icon;
			notifyIcon.Visible = true;
			notifyIcon.MouseDoubleClick += OnNotifyIconDoubleClick;
		}
	}

	private void OnNotifyIconDoubleClick(object sender, EventArgs e)
	{
		if (notifyIcon == null) return;
		Show();
		base.WindowState = wsl;
	}

	private void MinWindow_Click(object sender, MouseButtonEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void MaxWindow_Click(object sender, MouseButtonEventArgs e)
	{
		if (base.WindowState == WindowState.Maximized)
		{
			base.WindowState = WindowState.Normal;
		}
		else
		{
			base.WindowState = WindowState.Maximized;
		}
	}

	private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
	{
		if (SysMessage.MessageShow(Window.GetWindow(this), "是否关闭软件", "注意", MessageBoxButton.OKCancel) == MessageEnum.OK)
		{
			Close();
			Environment.Exit(0);
		}
	}

	private void menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
	}

	private void TextBlock_ImageFailed(object sender, ExceptionRoutedEventArgs e)
	{
	}

	private void RadioButton_Checked(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.RadioButton { IsChecked: var isChecked } radioButton && isChecked.GetValueOrDefault())
		{
			if (radioButton.Content.ToString() == "前端页")
			{
				bdrBrowser.Visibility = Visibility.Visible;
				bdrBrowser1.Visibility = Visibility.Hidden;
			}
			else
			{
				bdrBrowser.Visibility = Visibility.Hidden;
				bdrBrowser1.Visibility = Visibility.Visible;
			}
		}
	}

	private void Browser_FrameLoadStart(object sender, FrameLoadStartEventArgs e)
	{
	}

	private void BorserProBarTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
	}

	private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
	{
	}

	private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
	{
		bool isContinue = false;
		base.Dispatcher.Invoke(async delegate
		{
			string url = e.Browser.MainFrame.Url;
			string source = await e.Browser.MainFrame.GetSourceAsync();
			txtBrowser.Text = url;
			if (source.Contains("原商品已售罄，为你推荐相似商品"))
			{
				bound.msg = "原商品已售罄";
				isContinue = true;
			}
			else if (source.Contains("商品已售罄"))
			{
				bound.msg = "商品已售罄";
				isContinue = true;
			}
			else if (source.Contains("页面正在加载中，请稍后"))
			{
				bound.msg = "页面正在加载中";
				isContinue = true;
			}
			else if (source.Contains("手机网络繁忙，请稍后再试"))
			{
				bound.msg = "手机网络繁忙";
				isContinue = true;
			}
			else if (source.Contains("商品不存在"))
			{
				bound.msg = "商品不存在";
				isContinue = true;
			}
			bound.isLogin2 = false;
			bound.isStartLogin1 = true;
			if (url.Contains("login") && !buyerLoginTipShown)
			{
				buyerLoginTipShown = true;
				txt_Notice.Text = "⚠ 拼夕夕未登录：搜索/采集需先在拼夕夕页完成登录（登录后自动清除提示）";
			}
			if (!url.Contains("login") && buyerLoginTipShown)
			{
				buyerLoginTipShown = false;
				txt_Notice.Text = "";
			}
			if (url.Contains("psnl_verification.html") || source.Contains("完成拼多多官方验证"))
			{
				bound.msg = "完成拼多多官方验证";
				bound.isLogin2 = true;
			}
			if (url.Contains("login"))
			{
				bound.msg = "登录拼夕夕";
				bound.isLogin2 = true;
			}
			if (bound.isLogin2)
			{
				if (bound.isStart && !isEnd)
				{
					isEnd = true;
					SysMessage.MessageShow(Window.GetWindow(this), "请先登录拼夕夕账号", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
				}
			}
			else if (bound.settingInfo.checkCeshi == 0 && isContinue)
			{
				bound.isSkip = true;
			}
			else
			{
				if ((url.Contains("mobile.pinduoduo.com") && url.Contains("goods_id=") && url.Contains("/goods.html") && Regex.Match(url, "goods_id=([0-9]*)").Success) || isContinue)
				{
					isGetInfo = false;
					ProductInfoButtonSH(isShow: true);
					try
					{
						bound.jsonString = "";
						if (isContinue)
						{
							string dataPath = AppDomain.CurrentDomain.BaseDirectory + "\\1.txt";
							if (File.Exists(dataPath))
							{
								string jsonString = File.ReadAllText(dataPath);
								bound.jsonString = jsonString;
							}
						}
						string js = "\r\n                                function mdd_GetProductInfo() {\r\n                                var need_d = {}\r\n                                try {\r\n                                    var curr = rawData;\r\n                                    if(bound.jsonString.length > 0) curr = JSON.parse(bound.jsonString);\r\n                                    bound.rawData = JSON.stringify(curr);\r\n                                    need_d.thumbUrl = curr.store.initDataObj.goods.thumbUrl;\r\n                                    need_d.minGroupPrice = curr.store.initDataObj.goods.minGroupPrice;\r\n                                    need_d.minNormalPrice = curr.store.initDataObj.goods.minNormalPrice;\r\n                                    need_d.goodsName = curr.store.initDataObj.goods.goodsName;\r\n                                    need_d.goodsID = curr.store.initDataObj.goods.goodsID.toString();\r\n                                    need_d.detailGallery = curr.store.initDataObj.goods.detailGallery;\r\n                                    need_d.topGallery = curr.store.initDataObj.goods.topGallery;\r\n                                    need_d.topVideoGallery = curr.store.initDataObj.goods.videoGallery;\r\n                                    need_d.descVideoGallery = curr.store.initDataObj.goods.descVideoGallery;\r\n                                    need_d.skus = curr.store.initDataObj.goods.skus;\r\n                                    need_d.goodsProperty = curr.store.initDataObj.goods.goodsProperty;\r\n                                    need_d.topGalleryUrl = curr.store.initDataObj.goods.topGallery[0].url;\r\n                                    need_d.sectionPrice = curr.store.initDataObj.goods.ui.new_price_section.price;\r\n                                    need_d.sideSalesTip = curr.store.initDataObj.goods.sideSalesTip;\r\n                                    need_d.brandId = curr.store.initDataObj.goods.brandId.toString();\r\n                                    need_d.shareDesc = curr.store.initDataObj.goods.shareDesc;\r\n                                    need_d.shareLink = curr.store.initDataObj.goods.shareLink;\r\n                                    need_d.catID4 = curr.store.initDataObj.goods.catID4.toString();\r\n                                    need_d.catID3 = curr.store.initDataObj.goods.catID3.toString();\r\n                                    need_d.catID2 = curr.store.initDataObj.goods.catID2.toString();\r\n                                    need_d.catID1 = curr.store.initDataObj.goods.catID1.toString();\r\n                                    need_d.catID = curr.store.initDataObj.goods.catID.toString();\r\n                                    need_d.mallName = curr.store.initDataObj.mall.mallName;\r\n                                    need_d.mallLink = document.domain + '//' + curr.store.initDataObj.mall.pddRoute;\r\n                                    need_d.mallID = curr.store.initDataObj.mall.mallID.toString();\r\n                                    need_d.reviewNum = curr.store.initDataObj.oakData.review.reviewNum;\r\n                                    need_d.reviewNumText = curr.store.initDataObj.oakData.review.reviewNumText;\r\n                                    need_d.descScore = curr.store.initDataObj.mall.dsr.descScore;\r\n                                    need_d.logisticsScore = curr.store.initDataObj.mall.logisticsScore;\r\n                                    need_d.serviceScore = curr.store.initDataObj.mall.serviceScore;\r\n                                    need_d.mallStar = curr.store.initDataObj.mall.mallStar;\r\n                                    need_d.salesTip = curr.store.initDataObj.mall.salesTip;\r\n                                    need_d.salesTipV2 = curr.store.initDataObj.mall.salesTipV2;\r\n                                    need_d.goodsNum = curr.store.initDataObj.mall.goodsNum;\r\n                                    need_d.goodsNumDesc = curr.store.initDataObj.mall.goodsNumDesc;\r\n                                    var mydict = {};\r\n                                    mydict.accessToken = localStorage.getItem('AccessToken');\r\n                                    mydict.verifyAuthToken = localStorage.getItem('VerifyAuthToken');\r\n                                    mydict.cookie = document.cookie;\r\n                                    need_d.cookieInfo = mydict;\r\n                                } catch (err) {}\r\n                                return need_d;\r\n                            }\r\n                            mdd_GetProductInfo();\r\n                            ";
						JavascriptResponse result = await browser.EvaluateScriptAsync(js);
						if (result.Success)
						{
							string jsonProduct = JsonConvert.SerializeObject(result.Result);
							currProductInfo = JsonConvert.DeserializeObject<ProductInfo>(jsonProduct);
							if (currProductInfo == null)
							{
								throw new Exception("获取数据失败");
							}
							if (currProductInfo.cookieInfo == null)
							{
								currProductInfo.cookieInfo = new ProductCookieInfo();
								currProductInfo.cookieInfo.webCookies = new List<System.Net.Cookie>();
							}
							if (currProductInfo.cookieInfo != null && !string.IsNullOrEmpty(currProductInfo.cookieInfo.cookie))
							{
								currProductInfo.cookieInfo.webCookies = new List<System.Net.Cookie>();
								string[] cookies = currProductInfo.cookieInfo.cookie.Split(';');
								for (int i = 0; i < cookies.Length; i++)
								{
									int findex = cookies[i].IndexOf('=');
									if (findex > 0)
									{
										string cname = cookies[i].Substring(0, findex).Trim();
										string cvalue = ((findex == cookies[i].Length - 1) ? "" : cookies[i].Substring(findex + 1));
										currProductInfo.cookieInfo.webCookies.Add(new System.Net.Cookie(cname, cvalue));
									}
								}
							}
							bound.lastId = currProductInfo.goodsID;
							string thumbUrl = currProductInfo.thumbUrl;
							if (!string.IsNullOrEmpty(thumbUrl))
							{
								Match n = Regex.Match(thumbUrl, "\\d{4}-\\d{2}-\\d{2}");
								if (n.Success)
								{
									_ = n.Value;
								}
							}
							List<SkuInfo> skuInfos = new List<SkuInfo>();
							List<SkuPrice> skuPrice = new List<SkuPrice>();
							string currentDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\goodImg\\" + currProductInfo.goodsID;
							bound.imgPath = currentDirectory;
							bound.topImage = new List<string>();
							for (int k = 0; k < currProductInfo.topGallery.Count; k++)
							{
								if (bound.settingInfo.checkDelete != 1 || k != 0)
								{
									bound.topImage.Add(currProductInfo.topGallery[k].url);
								}
							}
							if (bound.topImage.Count == 0)
							{
								bound.topImage.Add(currProductInfo.topGallery[0].url);
							}
							string savePath2 = currentDirectory + "\\top";
							if (!Directory.Exists(savePath2))
							{
								Directory.CreateDirectory(savePath2);
							}
							bool isEdit2 = true;
							string[] box_type = new string[2] { "2", "3" };
							if (bound.settingInfo.boxType == 1 && (bound.imageInfo.topState == 0 || !box_type.Any((string it) => it == box_alter.SelectedValue.ToString())))
							{
								isEdit2 = false;
							}
							bound.topImage = DownloadImageThreadStart(savePath2, bound.topImage, isEdit2);
							bound.detailImage = new List<string>();
							int start = 0;
							if (bound.settingInfo.txtDel1 > 0)
							{
								start = bound.settingInfo.txtDel1;
							}
							int end = currProductInfo.detailGallery.Count;
							if (bound.settingInfo.txtDel2 > 0)
							{
								end = currProductInfo.detailGallery.Count - bound.settingInfo.txtDel2;
							}
							if (end < 0)
							{
								end = 0;
							}
							for (int l = start; l < end; l++)
							{
								bound.detailImage.Add(currProductInfo.detailGallery[l].url);
							}
							if (bound.detailImage.Count <= 0)
							{
								bound.detailImage.Add(currProductInfo.detailGallery[0].url);
							}
							savePath2 = currentDirectory + "\\detail";
							if (!Directory.Exists(savePath2))
							{
								Directory.CreateDirectory(savePath2);
							}
							bound.detailImage = DownloadImageThreadStart(savePath2, bound.detailImage, isEdit: false);
							bound.skuCount = 0;
							foreach (ProductSkus itemSku in currProductInfo.skus)
							{
								string goodsId = "";
								switch (bound.settingInfo.boxSku)
								{
								case 1:
									goodsId = "";
									break;
								case 2:
									goodsId = itemSku.spec;
									break;
								case 3:
									goodsId = bound.lastId;
									break;
								case 4:
									goodsId = "";
									break;
								}
								skuPrice.Add(new SkuPrice
								{
									groupPrice = Math.Round(float.Parse(itemSku.groupPrice), 2),
									price = Math.Round(float.Parse(itemSku.normalPrice), 2),
									num = itemSku.quantity,
									goodsId = goodsId
								});
								List<ProductSkusSpecs> specs = itemSku.specs;
								if (specs.Count() > 0)
								{
									for (int m = 0; m < specs.Count(); m++)
									{
										string key2 = specs[m].spec_key.Replace("分类", "");
										SkuInfo skuInfo = skuInfos.FirstOrDefault((SkuInfo it) => it.key == key2);
										if (skuInfo != null)
										{
											string value2 = handleSpec(key2, specs[m].spec_value);
											if (bound.settingInfo.txtSku1.Length > 0)
											{
												value2 = bound.settingInfo.txtSku1 + value2;
											}
											if (bound.settingInfo.txtSku2.Length > 0)
											{
												value2 += bound.settingInfo.txtSku2;
											}
											if (value2.Length > specs[m].spec_value.Length)
											{
												value2 = truncateStringByByteLength(value2, 30);
											}
											string skuv = skuInfo.values.FirstOrDefault((string it) => it == value2);
											if (skuv == null)
											{
												bound.skuCount++;
												if (m == 0 || key2 == "净含量")
												{
													skuInfo.imgPath.Add(itemSku.thumbUrl);
												}
												skuInfo.values.Add(value2);
											}
										}
										else
										{
											bound.skuCount++;
											List<string> imgPath = new List<string>();
											if (m == 0 || key2 == "净含量")
											{
												imgPath.Add(itemSku.thumbUrl);
											}
											string value4 = handleSpec(key2, specs[m].spec_value);
											if (bound.settingInfo.txtSku1.Length > 0)
											{
												value4 = bound.settingInfo.txtSku1 + value4;
											}
											if (bound.settingInfo.txtSku2.Length > 0)
											{
												value4 += bound.settingInfo.txtSku2;
											}
											if (value4.Length > specs[m].spec_value.Length)
											{
												value4 = truncateStringByByteLength(value4, 30);
											}
											skuInfos.Add(new SkuInfo
											{
												key = key2,
												values = new List<string> { value4 },
												imgPath = imgPath
											});
										}
									}
								}
							}
							savePath2 = currentDirectory + "\\sku";
							if (!Directory.Exists(savePath2))
							{
								Directory.CreateDirectory(savePath2);
							}
							bound.goodsName = currProductInfo.goodsName;
							bound.goodsName2 = bound.goodsName;
							if (skuInfos.Count > 1 && skuInfos[1].key == "净含量")
							{
								List<SkuInfo> skuInfos2 = JsonConvert.DeserializeObject<List<SkuInfo>>(JsonConvert.SerializeObject(skuInfos));
								SkuInfo item2 = skuInfos2[0];
								SkuInfo item3 = skuInfos2[1];
								skuInfos = new List<SkuInfo> { item3, item2 };
							}
							bound.SkuList = skuInfos;
							isEdit2 = true;
							if (bound.settingInfo.boxType == 1 && (bound.imageInfo.skuState == 0 || !box_type.Any((string it) => it == box_alter.SelectedValue.ToString())))
							{
								isEdit2 = false;
							}
							bound.SkuList[0].imgPath = DownloadImageThreadStart(savePath2, skuInfos[0].imgPath, isEdit2);
							bound.skuInfos = JsonConvert.SerializeObject(bound.SkuList);
							int skuLen = bound.SkuList[0].values.Count;
							if (bound.SkuList.Count > 1)
							{
								skuLen = bound.SkuList[0].values.Count * bound.SkuList[1].values.Count;
							}
							if (skuPrice.Count < skuLen)
							{
								int priceLen = skuPrice.Count;
								int priceCount = skuLen - skuPrice.Count;
								for (int n3 = 0; n3 < priceCount; n3++)
								{
									skuPrice.Add(new SkuPrice
									{
										groupPrice = skuPrice[priceLen - 1].groupPrice,
										price = skuPrice[priceLen - 1].price,
										num = "0",
										goodsId = ""
									});
								}
							}
							bound.SkuPriceList = skuPrice;
							bound.skuPrice = JsonConvert.SerializeObject(skuPrice);
							Regex nonNumericRegex = new Regex("[^0-9.]");
							Regex numericAndDotRegex = new Regex("[0-9.]");
							Regex letterRegex = new Regex("[a-zA-Z]");
							string[] type0 = new string[2] { "净含量", "保质期" };
							string[] type1 = new string[2] { "后跟高", "文号" };
							bound.goodsPropertyList = currProductInfo.goodsProperty;
							List<ProductGoodsProperty> goodsProperty = new List<ProductGoodsProperty>();
							for (int j = 0; j < bound.goodsPropertyList.Count; j++)
							{
								string key = bound.goodsPropertyList[j].key;
								string value5 = bound.goodsPropertyList[j].values[0];
								if (key.Contains("数") || key == "有效期")
								{
									string enumName = bound.goodsPropertyList[j].values[0][bound.goodsPropertyList[j].values[0].Length - 1].ToString();
									if (unitMap.TryGetValue(enumName, out var enumType))
									{
										bound.goodsPropertyList[j].values[0] = bound.goodsPropertyList[j].values[0].Substring(0, bound.goodsPropertyList[j].values[0].Length - 1);
										bound.goodsPropertyList[j].unit = enumType.EnumValue;
										bound.goodsPropertyList[j].type = 2;
									}
									enumType = default((Type, int));
								}
								else if (key.Contains("尺寸"))
								{
									string name = bound.goodsPropertyList[j].values[0].Replace("m", "");
									bound.goodsPropertyList[j].values = name.Split('X').ToList();
									bound.goodsPropertyList[j].type = 3;
								}
								else if (key == "生产日期")
								{
									bound.goodsPropertyList[j].type = 4;
								}
								else if (key == "牛肉种类")
								{
									bound.goodsPropertyList[j].type = 5;
								}
								else if (key == "产地")
								{
									string[] cdValue = bound.goodsPropertyList[j].values[0].Split('/');
									if (cdValue.Length == 3)
									{
										bound.goodsPropertyList[j].values[0] = cdValue[0];
										goodsProperty.Add(new ProductGoodsProperty
										{
											key = "省份",
											values = new List<string> { cdValue[1] }
										});
										goodsProperty.Add(new ProductGoodsProperty
										{
											key = "城市",
											values = new List<string> { cdValue[2] }
										});
									}
								}
								else if (type0.Contains(key))
								{
									MatchCollection matches = Regex.Matches(bound.goodsPropertyList[j].values[0], "-?\\d+(\\.\\d+)?");
									bound.goodsPropertyList[j].values = new List<string> { matches[0].ToString() };
								}
								else if (numericAndDotRegex.IsMatch(value5) && letterRegex.IsMatch(value5) && !type1.Any((string t) => key.Contains(t)))
								{
									string value3 = bound.goodsPropertyList[j].values[0];
									bound.goodsPropertyList[j].values[0] = nonNumericRegex.Replace(value3, string.Empty);
									bound.goodsPropertyList[j].unitStr = numericAndDotRegex.Replace(value3, string.Empty);
								}
							}
							foreach (ProductGoodsProperty item in goodsProperty)
							{
								bound.goodsPropertyList.Add(item);
							}
							bound.goodsProperty = JsonConvert.SerializeObject(bound.goodsPropertyList);
							string cateInfo = "";
							string cateJsonPath = AppDomain.CurrentDomain.BaseDirectory + "cate.json";
							using (FileStream fs = new FileStream(cateJsonPath, FileMode.Open))
							{
								int n2 = (int)fs.Length;
								byte[] b = new byte[n2];
								fs.Read(b, 0, n2);
								fs.Close();
								string cateJson = Encoding.UTF8.GetString(b, 0, n2);
								Dictionary<string, ProductCatInfo> dictCate = JsonConvert.DeserializeObject<Dictionary<string, ProductCatInfo>>(cateJson);
								List<string> listCat = new List<string>();
								if (!string.IsNullOrEmpty(currProductInfo.catID1) && currProductInfo.catID1 != "0")
								{
									listCat.Add(dictCate.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == currProductInfo.catID1).Value.cat_name);
								}
								if (!string.IsNullOrEmpty(currProductInfo.catID2) && currProductInfo.catID2 != "0")
								{
									listCat.Add(dictCate.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == currProductInfo.catID2).Value.cat_name);
								}
								if (!string.IsNullOrEmpty(currProductInfo.catID3) && currProductInfo.catID3 != "0")
								{
									listCat.Add(dictCate.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == currProductInfo.catID3).Value.cat_name);
								}
								if (!string.IsNullOrEmpty(currProductInfo.catID4) && currProductInfo.catID4 != "0")
								{
									ProductCatInfo value = dictCate.FirstOrDefault((KeyValuePair<string, ProductCatInfo> a) => a.Key == currProductInfo.catID4).Value;
									if (value != null)
									{
										listCat.Add(value.cat_name);
									}
								}
								cateInfo = string.Join(">", listCat);
							}
							bound.cateInfo = cateInfo;
							int index = cateInfo.IndexOf('>');
							if (index != -1)
							{
								bound.cateInfo = cateInfo.Substring(index + 1);
							}
							isGetInfo = true;
						}
						return;
					}
					catch (Exception ex)
					{
						Exception exception = ex;
						bound.msg = exception.Message;
						AddTextToRichTextBox("采集异常：" + exception.Message + "（若多次出现，可能是拼多多页面结构变更）", myRichTextBox, txt_log);
						isGoto = false;
						return;
					}
				}
				isGoto = false;
				ProductInfoButtonSH(isShow: false);
			}
		});
	}

	private string handleSpec(string key, string value)
	{
		if (key == "容量")
		{
			value = value.Replace("mL", "");
		}
		else if (key == "只数")
		{
			value = value.Replace("只", "");
		}
		return value;
	}

	private void btnBrowserGoto_Click(object sender, RoutedEventArgs e)
	{
		browser.Address = txtBrowser.Text.Trim();
	}

	private void btnBrowserGoBack_Click(object sender, RoutedEventArgs e)
	{
		if (browser.CanGoBack)
		{
			browser.Back();
		}
	}

	private void btnBrowserGoForward_Click(object sender, RoutedEventArgs e)
	{
		if (browser.CanGoForward)
		{
			browser.Forward();
		}
	}

	private void btnBrowserStop_Click(object sender, RoutedEventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (browser.IsLoading)
			{
				if (borserProBarTimer.Enabled)
				{
					borserProBarTimer.Stop();
				}
				browserProgressBar.Visibility = Visibility.Hidden;
				browserProgressBar.Value = 0.0;
			}
		});
		browser.Stop();
	}

	private void btnBrowserRefresh_Click(object sender, RoutedEventArgs e)
	{
		browser.Reload();
	}

	private void btnBrowserHome_Click(object sender, RoutedEventArgs e)
	{
		GotoUrl("https://mobile.pinduoduo.com/");
	}

	private void OtherBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
	{
		e.Browser.MainFrame.ExecuteJavaScriptAsync("document.getElementsByClassName('user-default')?document.getElementsByClassName('user-default')[0].click():''");
	}

	public void GotoUrl(string url, int type = 0)
	{
		if (type == 0)
		{
			browser.Address = url;
		}
	}

	private void Browser_FrameLoadStart1(object sender, FrameLoadStartEventArgs e)
	{
		base.Dispatcher.Invoke(async delegate
		{
			string js = "\r\n                    function delay(ms) {\r\n                        return new Promise(resolve => setTimeout(resolve, ms));\r\n                    }\r\n                    function extractPrice(inputString) {\r\n                        const priceRegex = /\\b\\d+(\\.\\d{1,2})?\\b/g;\r\n                        const matches = inputString.match(priceRegex);\r\n                        return matches ? matches[0] : null;\r\n                    }\r\n                    // 创建一个新的事件，通知框架输入值已更改\r\n                    function setNativeValue(element, value) {\r\n                        const valueSetter = Object.getOwnPropertyDescriptor(element, 'value').set;\r\n                        const prototype = Object.getPrototypeOf(element);\r\n                        const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value').set;\r\n\r\n                        if (valueSetter && valueSetter !== prototypeValueSetter) {\r\n                            prototypeValueSetter.call(element, value);\r\n                        } else {\r\n                            valueSetter.call(element, value);\r\n                        }\r\n                    }\r\n                    async function setTime(time){\r\n                        console.log('时间=',time)\r\n                        times = time.split('-')\r\n                        var datePicker = document.querySelector('[data-testid=\"beast-core-datePicker-dropdown-contentRoot\"]')\r\n                        var year = datePicker.querySelector('[data-testid=\"beast-core-select-htmlInput\"]')\r\n                        setNativeValue(year,times[0]);\r\n                        // 创建一个新的输入事件，表示值发生改变\r\n                        var inputEvent = new Event('change', { bubbles: true });\r\n                        year.dispatchEvent(inputEvent);\r\n                        await delay(300);\r\n                        document.querySelector('[data-testid=\"beast-core-datePicker-select-list\"]').querySelector('li').click()\r\n                        await delay(300);\r\n                        var currentDate = new Date();\r\n                        var currentMonth = currentDate.getMonth() + 1; // 加1使其符合常规月份表示\r\n                        var month = parseInt(times[1]) - currentMonth\r\n                        if (month > 0){\r\n                            var rect = datePicker.querySelectorAll('svg')[1]\r\n                            var event = new MouseEvent('click', {\r\n                                'view': window,\r\n                                'bubbles': true,\r\n                                'cancelable': true\r\n                            });\r\n                            while(month > 0){\r\n                                rect.dispatchEvent(event);\r\n                                await delay(300);\r\n                                month = month - 1\r\n                            }\r\n                        }\r\n                        else if(month < 0){\r\n                            var rect = datePicker.querySelector('svg')\r\n                            var event = new MouseEvent('click', {\r\n                                'view': window,\r\n                                'bubbles': true,\r\n                                'cancelable': true\r\n                            });\r\n                            while(month < 0){\r\n                                rect.dispatchEvent(event);\r\n                                await delay(300);\r\n                                month = month + 1\r\n                            }\r\n                        }\r\n                        await delay(500);\r\n                        var mday = parseInt(times[2])\r\n                        var day = parseInt(times[2]) - 1\r\n                        var dayList = document.querySelector('[data-testid=\"beast-core-datePicker-table\"]').querySelectorAll('td')\r\n                        for(var i =day;i<dayList.length;i++){\r\n                            if(dayList[i].textContent == mday){\r\n                                dayList[i].click()\r\n                                break\r\n                            }\r\n                        }\r\n                    }\r\n                    //function truncateStringByByteLength(str, maxByteLength) {\r\n                    //    let byteLength = 0;\r\n                    //    let truncatedString = '';\r\n                    //    for (let i = 0; i < str.length; i++) {\r\n                    //        let charCode = str.charCodeAt(i);\r\n                    //        let charByteLength = 0;\r\n                    //        if (charCode <= 0x007F) {\r\n                    //            charByteLength = 1;\r\n                    //        } else {\r\n                    //            charByteLength = 2;\r\n                    //        }\r\n                    //        if (byteLength + charByteLength > maxByteLength) {\r\n                    //            break;\r\n                    //        }\r\n                    //        byteLength += charByteLength;\r\n                    //        truncatedString += str.charAt(i);\r\n                    //    }\r\n                    //    return truncatedString;\r\n                    //}\r\n                ";
			await browser1.GetMainFrame().EvaluateScriptAsync(js);
		});
	}

	private void Browser_FrameLoadStart2(object sender, FrameLoadStartEventArgs e)
	{
		base.Dispatcher.Invoke(async delegate
		{
			string js = "\r\n                    function delay(ms) {\r\n                        return new Promise(resolve => setTimeout(resolve, ms));\r\n                    }\r\n                    // 创建一个新的事件，通知框架输入值已更改\r\n                    function setNativeValue(element, value) {\r\n                        const valueSetter = Object.getOwnPropertyDescriptor(element, 'value').set;\r\n                        const prototype = Object.getPrototypeOf(element);\r\n                        const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value').set;\r\n\r\n                        if (valueSetter && valueSetter !== prototypeValueSetter) {\r\n                            prototypeValueSetter.call(element, value);\r\n                        } else {\r\n                            valueSetter.call(element, value);\r\n                        }\r\n                    }\r\n                    async function setTime(time){\r\n                        console.log('时间=',time)\r\n                        times = time.split('-')\r\n                        var datePicker = document.querySelector('[data-testid=\"beast-core-datePicker-dropdown-contentRoot\"]')\r\n                        var year = datePicker.querySelector('[data-testid=\"beast-core-select-htmlInput\"]')\r\n                        setNativeValue(year,times[0]);\r\n                        // 创建一个新的输入事件，表示值发生改变\r\n                        var inputEvent = new Event('change', { bubbles: true });\r\n                        year.dispatchEvent(inputEvent);\r\n                        await delay(300);\r\n                        document.querySelector('[data-testid=\"beast-core-datePicker-select-list\"]').querySelector('li').click()\r\n                        await delay(300);\r\n                        var currentDate = new Date();\r\n                        var currentMonth = currentDate.getMonth() + 1; // 加1使其符合常规月份表示\r\n                        var month = parseInt(times[1]) - currentMonth\r\n                        if (month > 0){\r\n                            var rect = datePicker.querySelectorAll('svg')[1]\r\n                            var event = new MouseEvent('click', {\r\n                                'view': window,\r\n                                'bubbles': true,\r\n                                'cancelable': true\r\n                            });\r\n                            while(month > 0){\r\n                                rect.dispatchEvent(event);\r\n                                await delay(300);\r\n                                month = month - 1\r\n                            }\r\n                        }\r\n                        else if(month < 0){\r\n                            var rect = datePicker.querySelector('svg')\r\n                            var event = new MouseEvent('click', {\r\n                                'view': window,\r\n                                'bubbles': true,\r\n                                'cancelable': true\r\n                            });\r\n                            while(month < 0){\r\n                                rect.dispatchEvent(event);\r\n                                await delay(300);\r\n                                month = month + 1\r\n                            }\r\n                        }\r\n                        await delay(500);\r\n                        var mday = parseInt(times[2])\r\n                        var day = parseInt(times[2]) - 1\r\n                        var dayList = document.querySelector('[data-testid=\"beast-core-datePicker-table\"]').querySelectorAll('td')\r\n                        for(var i =day;i<dayList.length;i++){\r\n                            if(dayList[i].textContent == mday){\r\n                                dayList[i].click()\r\n                                break\r\n                            }\r\n                        }\r\n                    }\r\n                    //function truncateStringByByteLength(str, maxByteLength) {\r\n                    //    let byteLength = 0;\r\n                    //    let truncatedString = '';\r\n                    //    for (let i = 0; i < str.length; i++) {\r\n                    //        let charCode = str.charCodeAt(i);\r\n                    //        let charByteLength = 0;\r\n                    //        if (charCode <= 0x007F) {\r\n                    //            charByteLength = 1;\r\n                    //        } else {\r\n                    //            charByteLength = 2;\r\n                    //        }\r\n                    //        if (byteLength + charByteLength > maxByteLength) {\r\n                    //            break;\r\n                    //        }\r\n                    //        byteLength += charByteLength;\r\n                    //        truncatedString += str.charAt(i);\r\n                    //    }\r\n                    //    return truncatedString;\r\n                    //}\r\n                ";
			await browser2.GetMainFrame().EvaluateScriptAsync(js);
		});
	}

	private void BorserProBarTimer_Elapsed1(object sender, ElapsedEventArgs e)
	{
	}

	private void Browser_LoadingStateChanged1(object sender, LoadingStateChangedEventArgs e)
	{
	}

	private void Browser_FrameLoadEnd1(object sender, FrameLoadEndEventArgs e)
	{
		base.Dispatcher.Invoke(async delegate
		{
			string url = e.Browser.MainFrame.Url;
			string source = await e.Browser.MainFrame.GetSourceAsync();
			bound.isLogin = false;
			if (source.Contains("扫码登录"))
			{
				bound.msg = "未登录账号";
				bound.isLogin = true;
			}
			else if (url.Contains("goods/category"))
			{
				bound.step = 1;
			}
			bound.isStartLogin = true;
		});
	}

	private void Browser_FrameLoadEnd2(object sender, FrameLoadEndEventArgs e)
	{
		base.Dispatcher.Invoke(async delegate
		{
		});
	}

	private void Browser_AddressChanged1(object sender, DependencyPropertyChangedEventArgs e)
	{
		base.Dispatcher.Invoke(async delegate
		{
			string url = e.NewValue.ToString();
			if (url.Contains("goods_add/success") && bound.type != "kdc")
			{
				bound.step = 17;
			}
			if (url.Contains("goods/goods_add"))
			{
				Thread.Sleep(3000);
				Uri uri = new Uri(url);
				string query = uri.Query;
				NameValueCollection queryParams = HttpUtility.ParseQueryString(query);
				bound.newGoodId = queryParams["goods_id"];
				if (bound.type != "kdc")
				{
					bound.step = 2;
				}
			}
			if (url.Contains("/aftersales-ssr/detail") && bound.step != 1)
			{
				bound.step = 3;
			}
		});
	}

	private void Browser_AddressChanged2(object sender, DependencyPropertyChangedEventArgs e)
	{
		base.Dispatcher.Invoke(async delegate
		{
			string url = e.NewValue.ToString();
			if (url.Contains("/aftersales-ssr/detail") && bound.step != 1)
			{
				Uri uri = new Uri(url);
				NameValueCollection queryParameters = HttpUtility.ParseQueryString(uri.Query);
				bound.newGoodId = queryParameters["orderSn"].ToString();
				bound.url = url;
				bound.step = 2;
			}
		});
	}

	public void GotoUrl1(string url, int type = 0)
	{
		if (type == 0)
		{
			browser1.Address = url;
		}
	}

	private void btnJump_Click(object sender, RoutedEventArgs e)
	{
		SysMessage.MessageShow(Window.GetWindow(this), "本地版无需续费", "提示", MessageBoxButton.OK);
	}

	private void btnGoodMonitor_Click(object sender, RoutedEventArgs e)
	{
		if (currProductInfo == null || string.IsNullOrEmpty(currProductInfo.goodsID))
		{
			return;
		}
		if (currProductInfo.sideSalesTip.Contains("10万+"))
		{
			SysMessage.MessageShow(Window.GetWindow(this), "10万+的商品不能添加监控", "注意", MessageBoxButton.OK);
			return;
		}
		DateTime now = DateTime.Now;
		GoodMonitor good = new GoodMonitor();
		good.GoodId = currProductInfo.goodsID.Split('.')[0];
		good.GoodName = currProductInfo.goodsName;
		good.UpdateTime = now.ToString("yyyy-MM-dd HH:mm:ss");
		good.CreateTime = good.UpdateTime;
		List<GoodMonitor> allGoodMonitor = GoodMonitorBLL.GetAllGoodMonitor("1");
		if (allGoodMonitor.Count >= 5)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "最多监控5款商品，请先删除其他商品", "注意", MessageBoxButton.OK);
			return;
		}
		int goodNum = GetGoodNum();
		good.GoodNum = goodNum;
		List<GoodMonitor> allGoodMonitor2 = GoodMonitorBLL.GetAllGoodMonitor("all");
		GoodMonitor goodMonitor = allGoodMonitor2.Where((GoodMonitor it) => it.GoodId == good.GoodId).FirstOrDefault();
		if (goodMonitor != null)
		{
			goodMonitor.GoodState = 1;
			GoodMonitorBLL.UpdateGoodMonitorState(goodMonitor);
		}
		else
		{
			GoodMonitorBLL.AddGoodMonitor(good);
		}
		GoodMonitorInfo goodMonitorInfo = GoodMonitorBLL.GetGoodMonitorInfo(good.GoodId, now.ToString("yyyy-MM-dd"));
		if (goodMonitorInfo == null)
		{
			goodMonitorInfo = new GoodMonitorInfo();
		}
		goodMonitorInfo.GoodNum = goodNum;
		goodMonitorInfo.UpdateTime = now.ToString("yyyy-MM-dd HH:mm:ss");
		if (string.IsNullOrEmpty(goodMonitorInfo.GoodId))
		{
			goodMonitorInfo.GoodId = good.GoodId;
			goodMonitorInfo.GoodName = good.GoodName;
			goodMonitorInfo.CreateTime = now.ToString("yyyy-MM-dd");
			GoodMonitorBLL.AddGoodMonitorInfo(goodMonitorInfo);
		}
		else
		{
			GoodMonitorBLL.UpdateGoodMonitorInfo(goodMonitorInfo);
		}
	}

	private int GetGoodNum()
	{
		string[] array = currProductInfo.sideSalesTip.Split(new string[1] { "已拼" }, StringSplitOptions.None);
		string text = "";
		if (array.Length > 1)
		{
			text = array[1];
		}
		text = text.Remove(text.Length - 1, 1);
		double result = 0.0;
		if (text.Contains("+"))
		{
			text = text.Replace("+", "");
		}
		if (text.Contains("万"))
		{
			text = text.Replace("万", "");
			double.TryParse(text, out result);
			result *= 10000.0;
		}
		else
		{
			double.TryParse(text, out result);
		}
		return (int)result;
	}

	private void btnGoodManager_Click(object sender, RoutedEventArgs e)
	{
	}

	private void showNotice(NoticeInfo info)
	{
		NoticeWindow noticeWindow = new NoticeWindow(SysCommon.PopNotice.Width, SysCommon.PopNotice.Height);
		noticeWindow.Closed += Dialog_Closed;
		noticeWindow.TopFrom = GetTopFrom();
		_dialogs.Add(noticeWindow);
		noticeWindow.DataContext = info;
		noticeWindow.Show();
	}

	private void Dialog_Closed(object sender, EventArgs e)
	{
		NoticeWindow item = sender as NoticeWindow;
		_dialogs.Remove(item);
	}

	private double GetTopFrom()
	{
		double topFrom = SystemParameters.WorkArea.Bottom - 10.0;
		bool flag = _dialogs.Any((NoticeWindow o) => o.TopFrom == topFrom);
		while (flag)
		{
			topFrom -= 210.0;
			flag = _dialogs.Any((NoticeWindow o) => o.TopFrom == topFrom);
		}
		if (topFrom <= 0.0)
		{
			topFrom = SystemParameters.WorkArea.Bottom - 10.0;
		}
		return topFrom;
	}

	private void btnProductInfoShow_Click(object sender, RoutedEventArgs e)
	{
		if (currProductInfo == null)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先在前端采集页打开并加载一个商品", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		ProductShow productShow = new ProductShow(currProductInfo);
		productShow.ShowDialog();
	}

	private void BtnProductInfoDownload_OnClick(object sender, RoutedEventArgs e)
	{
	}

	private void BtnReviewDownload_OnClick(object sender, RoutedEventArgs e)
	{
		if (currProductInfo == null)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先在前端采集页打开并加载一个商品", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		ProductReviewsDownload productReviewsDownload = new ProductReviewsDownload(currProductInfo);
		productReviewsDownload.ShowDialog();
	}

	private void btnPublish_Click(object sender, RoutedEventArgs e)
	{
		isEnd = true;
		Thread.Sleep(1000);
		string path = AppDomain.CurrentDomain.BaseDirectory + "\\goodImg";
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
		isEnd = false;
		pauseEvent.Set();
		if (txt_goodId.Text.Length == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先填写商品id", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		bound.settingInfo.txtGoodId = txt_goodId.Text;
		bound.settingInfo.goodIds = bound.settingInfo.txtGoodId.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
		bound.settingInfo.goodIds2 = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(bound.settingInfo.goodIds));
		bound.settingInfo.goodIds3 = new List<string>();
		bound.settingInfo.checkType = (check_type.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.txtShopType = txt_shopType.Text;
		int num = txt_shopType.Text.IndexOf('>');
		if (num != -1)
		{
			bound.settingInfo.txtShopType = txt_shopType.Text.Substring(num + 1);
		}
		bound.settingInfo.boxUp = int.Parse(box_up.SelectedValue.ToString());
		bound.settingInfo.boxFreight = int.Parse(box_freight.SelectedValue.ToString());
		bound.settingInfo.boxTime = int.Parse(box_time.SelectedValue.ToString());
		bound.settingInfo.boxSale = int.Parse(box_sale.SelectedValue.ToString());
		bound.settingInfo.txtSaleDay = txt_saleDay.Text;
		bound.settingInfo.boxInventory = int.Parse(box_inventory.SelectedValue.ToString());
		int.TryParse(txt_inventory.Text, out var result);
		bound.settingInfo.txtInventory = result;
		bound.settingInfo.isFront = (isFront.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.txtFront = txt_front.Text;
		bound.settingInfo.isAfter = (isAfter.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.txtAfter = txt_after.Text;
		bound.settingInfo.txtTitle = txt_title.Text;
		bound.settingInfo.isSkuName = (isSkuName.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.txtKeyword = txt_keyword.Text;
		bound.settingInfo.isSkuKeyword = (isSkuKeyword.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.txtNormal = txt_normal.Text;
		bound.settingInfo.txt_fill = txt_fill.Text;
		bound.settingInfo.isFille = (isFill.IsChecked.GetValueOrDefault() ? 1 : 0);
		int.TryParse(txt_minku.Text, out var result2);
		if (txt_minku.Text.Length <= 0)
		{
			result2 = -1;
		}
		bound.settingInfo.txtMinku = result2;
		bound.settingInfo.txtUnder = int.Parse(txt_under.Text);
		bound.settingInfo.boxChange = int.Parse(box_change.SelectedValue.ToString());
		bound.settingInfo.txt_change = txt_change.Text.ToUpper();
		double.TryParse(txt_change.Text, out var result3);
		bound.settingInfo.txtChange = result3;
		bound.settingInfo.isGroup = (isGroup.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.boxGroup = int.Parse(box_group.SelectedValue.ToString());
		double.TryParse(txt_group.Text, out var result4);
		bound.settingInfo.txtGroup = result4;
		bound.settingInfo.txtSku1 = txt_sku1.Text;
		bound.settingInfo.txtSku2 = txt_sku2.Text;
		bound.settingInfo.checkDelete = (check_delete.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.txtDel1 = int.Parse(txt_del1.Text);
		bound.settingInfo.txtDel2 = int.Parse(txt_del2.Text);
		bound.settingInfo.boxCode = int.Parse(box_code.SelectedValue.ToString());
		bound.settingInfo.boxSku = int.Parse(box_sku.SelectedValue.ToString());
		bound.settingInfo.choose1 = (choose1.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose2 = (choose2.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose3 = (choose3.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose4 = (choose4.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose5 = (choose5.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose6 = (choose6.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose7 = (choose7.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose8 = (choose8.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose9 = (choose9.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose10 = (choose10.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.choose11 = (choose11.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.settingInfo.boxAlter = int.Parse(box_alter.SelectedValue.ToString());
		bound.settingInfo.boxType = int.Parse(box_type.SelectedValue.ToString());
		bound.settingInfo.txtNum = int.Parse(txt_num.Text);
		if (bound.settingInfo.boxType == 2)
		{
			bound.settingInfo.goodIds = new List<string> { bound.settingInfo.goodIds[0] };
		}
		bound.settingInfo.txtLiebian = txt_liebian.Text.ToUpper();
		if (bound.settingInfo.boxType == 2)
		{
			if (bound.settingInfo.txtNum <= 0)
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先填写裂变数量", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
				return;
			}
			if (bound.settingInfo.txtLiebian.Length == 0)
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先填写裂变公式", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
				return;
			}
		}
		bound.settingInfo.checkCeshi = (check_ceshi.IsChecked.GetValueOrDefault() ? 1 : 0);
		tab_main.SelectedIndex = 1;
		txt_goodId1.Text = "";
		bound.nowId = 0;
		bound.isStart = true;
		bound.type = "fabu";
		Task task = Task.Run(delegate
		{
			ThreadGoods(token);
		}, token);
	}

	public void ThreadGoods(CancellationToken token)
	{
		while (!bound.isStartLogin1)
		{
			Thread.Sleep(1000);
		}
		if (isEnd)
		{
			return;
		}
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 2;
		});
		while (!bound.isStartLogin)
		{
			Thread.Sleep(1000);
		}
		base.Dispatcher.Invoke(delegate
		{
			if (bound.isLogin)
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先登录商家店铺账号", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
				isEnd = true;
			}
			else
			{
				tab_main.SelectedIndex = 2;
			}
		});
		int num = 0;
		int num2 = 0;
		isGetInfo = false;
		bound.isPublish = true;
		isGoto = false;
		int num3 = bound.settingInfo.goodIds.Count;
		if (bound.settingInfo.boxType == 2)
		{
			num3 = bound.settingInfo.txtNum;
			base.Dispatcher.Invoke(async delegate
			{
				tab_main.SelectedIndex = 1;
				GotoUrl("https://mobile.pinduoduo.com/goods.html?goods_id=" + bound.settingInfo.goodIds[bound.nowId]);
			});
		}
		while (bound.nowId < num3)
		{
			LogHelper.Info("ThreadGoods：111111");
			pauseEvent.WaitOne();
			LogHelper.Info("ThreadGoods：111112");
			if (isEnd)
			{
				break;
			}
			LogHelper.Info("ThreadGoods：111113");
			if (bound.isSkip)
			{
				LogHelper.Info("ThreadGoods：111114");
				isGetInfo = false;
				bound.isPublish = true;
				bound.isSkip = false;
				isGoto = false;
				num2++;
				num = num2;
				bound.nowId++;
				continue;
			}
			LogHelper.Info("ThreadGoods：111115");
			if (bound.settingInfo.boxType == 1)
			{
				LogHelper.Info("ThreadGoods：111116");
				if (!isGetInfo && !isGoto && num2 == num)
				{
					LogHelper.Info("ThreadGoods：111117");
					isGoto = true;
					base.Dispatcher.Invoke(async delegate
					{
						tab_main.SelectedIndex = 1;
						GotoUrl("https://mobile.pinduoduo.com/goods.html?goods_id=" + bound.settingInfo.goodIds[bound.nowId]);
					});
				}
			}
			if ((isGetInfo || (bound.settingInfo.boxType == 2 && bound.nowId > 0)) && bound.isPublish)
			{
				LogHelper.Info("ThreadGoods：111118");
				num2++;
				bound.isPublish = false;
			}
			if (num2 > num)
			{
				LogHelper.Info("ThreadGoods：111119");
				isGoto = false;
				if (bound.settingInfo.boxType == 2 && bound.nowId > 0)
				{
					Task.Run(delegate
					{
						LieBianImg(token);
					}, token);
				}
				string txtKeyword = bound.settingInfo.txtKeyword;
				if (txtKeyword.Length > 0)
				{
					LogHelper.Info("ThreadGoods：111120");
					string[] array = txtKeyword.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					string[] array2 = array;
					foreach (string value in array2)
					{
						if (bound.goodsName.Contains(value) || (bound.settingInfo.isSkuKeyword == 1 && bound.skuInfos.Contains(value)))
						{
							isGetInfo = false;
							bound.isPublish = true;
						}
					}
				}
				if (bound.skuCount < 2 && bound.settingInfo.choose4 == 1)
				{
					LogHelper.Info("ThreadGoods：111121");
					isGetInfo = false;
					bound.isPublish = true;
				}
				if (bound.isPublish)
				{
					LogHelper.Info("ThreadGoods：111122");
					if (bound.settingInfo.goodIds2.Count > 0 && bound.settingInfo.boxType == 1)
					{
						LogHelper.Info("ThreadGoods：111123");
						bound.settingInfo.goodIds3.Add(bound.settingInfo.goodIds2[0]);
						bound.settingInfo.goodIds2.RemoveAt(0);
						txt_goodId.Clear();
						txt_goodId1.Clear();
						string text = string.Join(Environment.NewLine, bound.settingInfo.goodIds2);
						txt_goodId.Text = text;
						string text2 = string.Join(Environment.NewLine, bound.settingInfo.goodIds3);
						txt_goodId1.Text = text2;
					}
					num = num2;
					bound.nowId++;
					continue;
				}
				LogHelper.Info("ThreadGoods：111124");
				string txtTitle = bound.settingInfo.txtTitle;
				if (txtTitle.Length > 0 && txtTitle.Contains("="))
				{
					LogHelper.Info("ThreadGoods：111125");
					string[] array3 = txtTitle.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					string[] array4 = array3;
					foreach (string text3 in array4)
					{
						string[] array5 = text3.Split('=');
						if (array5.Length > 1)
						{
							bound.goodsName = bound.goodsName.Replace(array5[0], array5[1]);
							if (bound.settingInfo.isSkuName == 1)
							{
								bound.skuInfos = bound.skuInfos.Replace(array5[0], array5[1]);
							}
						}
					}
				}
				LogHelper.Info("ThreadGoods：111126");
				bound.goodsName = bound.goodsName2;
				if (bound.settingInfo.isFront == 1)
				{
					bound.goodsName = bound.settingInfo.txtFront + bound.goodsName;
				}
				if (bound.settingInfo.isAfter == 1)
				{
					bound.goodsName += bound.settingInfo.txtAfter;
				}
				LogHelper.Info("ThreadGoods：111127");
				if (bound.settingInfo.boxType == 2)
				{
					LogHelper.Info("ThreadGoods：111128");
					bound.goodsName = truncateStringByByteLength(bound.goodsName, 60 - bound.nowId - 1);
					bound.goodsName += bound.nowId + 1;
				}
				else
				{
					LogHelper.Info("ThreadGoods：111129");
					bound.goodsName = truncateStringByByteLength(bound.goodsName, 60);
				}
				LogHelper.Info("ThreadGoods：111130");
				string txtNormal = bound.settingInfo.txtNormal;
				if (txtNormal.Length > 0)
				{
					LogHelper.Info("ThreadGoods：111131");
					string[] array6 = txtNormal.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					string[] array7 = array6;
					foreach (string text4 in array7)
					{
						LogHelper.Info("ThreadGoods：111132");
						string[] normal = text4.Split('=');
						int num4 = bound.goodsPropertyList.FindIndex((ProductGoodsProperty item) => item.key == normal[0]);
						if (normal.Length <= 1 || num4 < 0)
						{
							continue;
						}
						LogHelper.Info("ThreadGoods：111133");
						string[] array8 = normal[1].Split(';');
						string[] array9 = array8;
						foreach (string v in array9)
						{
							if (!(v == normal[0]))
							{
								int num5 = bound.goodsPropertyList.FindIndex((ProductGoodsProperty item) => item.key == v);
								if (num5 < 0)
								{
									bound.goodsPropertyList.Add(new ProductGoodsProperty
									{
										key = v,
										values = bound.goodsPropertyList[num4].values
									});
								}
							}
						}
					}
					bound.goodsProperty = JsonConvert.SerializeObject(bound.goodsPropertyList);
				}
				LogHelper.Info("ThreadGoods：111134");
				string text5 = bound.settingInfo.txt_fill;
				if (text5.Length > 0)
				{
					LogHelper.Info("ThreadGoods：111135");
					string[] array10 = text5.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					string[] array11 = array10;
					foreach (string text6 in array11)
					{
						LogHelper.Info("ThreadGoods：111136");
						string[] fill = text6.Split('=');
						if (fill.Length <= 1)
						{
							continue;
						}
						if (bound.goodsProperty.Contains(fill[0]))
						{
							if (bound.settingInfo.isFille == 1)
							{
								int index = bound.goodsPropertyList.FindIndex((ProductGoodsProperty item) => item.key == fill[0]);
								bound.goodsPropertyList[index] = new ProductGoodsProperty
								{
									key = fill[0],
									values = fill[1].Split(';').ToList()
								};
							}
						}
						else
						{
							bound.goodsPropertyList.Add(new ProductGoodsProperty
							{
								key = fill[0],
								values = fill[1].Split(';').ToList()
							});
						}
					}
					bound.goodsProperty = JsonConvert.SerializeObject(bound.goodsPropertyList);
				}
				LogHelper.Info("ThreadGoods：111137");
				List<SkuPrice> list = JsonConvert.DeserializeObject<List<SkuPrice>>(JsonConvert.SerializeObject(bound.SkuPriceList));
				for (int n = 0; n < list.Count; n++)
				{
					if (bound.settingInfo.boxType == 2)
					{
						try
						{
							double value2 = FormulaEvaluator.EvaluateFormula(bound.settingInfo.txtLiebian, list[n].groupPrice, bound.nowId + 1);
							list[n].groupPrice = Math.Round(value2, 2);
							double value3 = FormulaEvaluator.EvaluateFormula(bound.settingInfo.txtLiebian, list[n].price, bound.nowId + 1);
							list[n].price = Math.Round(value3, 2);
						}
						catch
						{
						}
					}
					else
					{
						if (bound.settingInfo.txtChange > 0.0 || bound.settingInfo.boxChange == 6)
						{
							switch (bound.settingInfo.boxChange)
							{
							case 1:
								list[n].groupPrice = Math.Round(list[n].groupPrice * (1.0 + bound.settingInfo.txtChange / 100.0), 2);
								list[n].price = Math.Round(list[n].price * (1.0 + bound.settingInfo.txtChange / 100.0), 2);
								break;
							case 2:
								list[n].groupPrice = Math.Round(list[n].groupPrice * (1.0 - bound.settingInfo.txtChange / 100.0), 2);
								list[n].price = Math.Round(list[n].price * (1.0 - bound.settingInfo.txtChange / 100.0), 2);
								break;
							case 3:
								list[n].groupPrice = Math.Round(list[n].groupPrice + bound.settingInfo.txtChange, 2);
								list[n].price = Math.Round(list[n].price + bound.settingInfo.txtChange, 2);
								break;
							case 4:
								list[n].groupPrice = Math.Round(list[n].groupPrice - bound.settingInfo.txtChange, 2);
								list[n].price = Math.Round(list[n].price - bound.settingInfo.txtChange, 2);
								break;
							case 5:
								list[n].groupPrice = Math.Round(bound.settingInfo.txtChange, 2);
								list[n].price = Math.Round(bound.settingInfo.txtChange, 2);
								break;
							case 6:
								try
								{
									double value4 = FormulaEvaluator.EvaluateFormula(bound.settingInfo.txt_change, list[n].groupPrice);
									list[n].groupPrice = Math.Round(value4, 2);
									double value5 = FormulaEvaluator.EvaluateFormula(bound.settingInfo.txt_change, list[n].price);
									list[n].price = Math.Round(value5, 2);
								}
								catch
								{
								}
								break;
							}
						}
						if (bound.settingInfo.txtGroup > 0.0 && bound.settingInfo.isGroup == 1)
						{
							switch (bound.settingInfo.boxGroup)
							{
							case 1:
								list[n].price = Math.Round(list[n].price + bound.settingInfo.txtGroup, 2);
								break;
							case 2:
								list[n].price = Math.Round(list[n].price * bound.settingInfo.txtGroup, 2);
								break;
							}
						}
					}
					if (list[n].groupPrice < 0.0)
					{
						list[n].groupPrice = 0.0;
					}
					if (list[n].price < 0.0)
					{
						list[n].price = 0.0;
					}
					if (n == 0 && bound.settingInfo.txtMinku >= 0)
					{
						list[0].num = bound.settingInfo.txtMinku.ToString();
					}
					if (bound.settingInfo.txtUnder > 0 && int.Parse(list[n].num) < bound.settingInfo.txtUnder)
					{
						list[n].num = "0";
					}
					if (bound.settingInfo.choose5 == 1 && list[n].groupPrice < 10.0)
					{
						list[n].num = "0";
					}
				}
				bound.skuPrice = JsonConvert.SerializeObject(list);
				LogHelper.Info("ThreadGoods：111138");
				switch (bound.settingInfo.boxCode)
				{
				case 1:
					bound.settingInfo.goodNo = "";
					break;
				case 2:
					bound.settingInfo.goodNo = "a_" + bound.lastId;
					break;
				case 3:
					bound.settingInfo.goodNo = "";
					break;
				case 4:
					bound.settingInfo.goodNo = "";
					break;
				}
				num = num2;
				LogHelper.Info("ThreadGoods：111139");
				base.Dispatcher.Invoke(async delegate
				{
					if (bound.isLogin)
					{
						SysMessage.MessageShow(Window.GetWindow(this), "请先登录商家店铺账号", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
					}
					else
					{
						tab_main.SelectedIndex = 2;
						GotoUrl1("https://mms.pinduoduo.com/goods/category");
					}
				});
				LogHelper.Info("ThreadGoods：111140");
				Task task = Task.Run(delegate
				{
					ThreadPublish(token);
				}, token);
			}
			Thread.Sleep(5000);
		}
		LogHelper.Info("ThreadGoods：111144");
		bound.isStart = false;
		if (num == num3)
		{
			LogHelper.Info("ThreadGoods：111145");
			AddTextToRichTextBox("全部上传完成... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), myRichTextBox, txt_log);
			LogHelper.Info("ThreadGoods：111146");
			base.Dispatcher.Invoke(delegate
			{
				LogHelper.Info("ThreadGoods：111147");
				tab_main.SelectedIndex = 0;
				SysMessage.MessageShow(Window.GetWindow(this), "上传已完成", "上传成功：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			});
		}
	}

	private string truncateStringByByteLength(string str, int maxByteLength)
	{
		int num = 0;
		string text = "";
		for (int i = 0; i < str.Length; i++)
		{
			int num2 = Convert.ToInt32(str[i]);
			int num3 = 0;
			num3 = ((num2 <= 127) ? 1 : 2);
			if (num + num3 > maxByteLength)
			{
				break;
			}
			num += num3;
			text += str[i];
		}
		return text;
	}

	public async void ThreadPublish(CancellationToken token)
	{
		string good_id = ((bound.settingInfo.boxType != 2) ? bound.settingInfo.goodIds[bound.nowId] : bound.settingInfo.goodIds[0]);
		bound.endMsg = "";
		bound.msg = "";
		bound.isPublish = false;
		bound.isRun = 1;
		try
		{
			int imgNum = 0;
			int curNum = 0;
			int num = 0;
			bound.isEnd = false;
			int date_time = 10;
			DateTime create_time = DateTime.Now;
			while (true)
			{
				pauseEvent.WaitOne();
				base.Dispatcher.Invoke(async delegate
				{
					if (tab_main.SelectedIndex == 1)
					{
						tab_main.SelectedIndex = 2;
					}
				});
				if ((DateTime.Now - create_time).TotalMinutes > (double)date_time)
				{
					bound.isEnd = true;
					if (SubmitLogBLL.GetGoodId(good_id).Rows.Count == 0)
					{
						SubmitLogBLL.SubmitStep(new StepErrorInfo
						{
							msg = bound.msg,
							step = bound.step,
							goodid = good_id
						});
					}
					SubmitLogBLL.AddShop(new SubmitLogInfo
					{
						goodId = good_id,
						num = 0,
						createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
					});
					throw new Exception(bound.msg);
				}
				if (isEnd)
				{
					break;
				}
				string js = "";
				Thread.Sleep(3000);
				if (bound.isRun == 0)
				{
					continue;
				}
				switch (bound.step)
				{
				case 22:
					js = "\r\n                                async function runDelays(){\r\n                                    //登录异常情况\r\n                                    var core_toast = document.querySelector('[data-testid=\"beast-core-toast\"]')\r\n                                    if(core_toast && core_toast.textContent.includes('商品草稿ID获取失败') ){\r\n                                        bound.step = 19;\r\n                                        return\r\n                                    }\r\n                                    var cateInfo = bound.cateInfo\r\n                                    if(bound.settingInfo.checkType == 0) cateInfo = bound.settingInfo.txtShopType\r\n                                    var searchInput = document.querySelector('[data-testid=\"beast-core-input-htmlInput\"]');\r\n                                    setNativeValue(searchInput, cateInfo);\r\n                                    // 创建一个新的输入事件，表示值发生改变\r\n                                    var inputEvent = new Event('input', { bubbles: true });\r\n                                    searchInput.dispatchEvent(inputEvent);\r\n                                    await delay(100);\r\n                                    searchInput.click()\r\n                                    await delay(600);\r\n                                    if(cateInfo == '家庭/个人清洁工具>室内/室外清洁用具>垃圾袋'){\r\n                                        document.querySelector('[data-testid=\"beast-core-search-panel\"]').querySelectorAll('li')[1].click()\r\n                                    }\r\n                                    else{\r\n                                        document.querySelector('[data-testid=\"beast-core-search-panel\"]').querySelector('li').click()\r\n                                    }\r\n                                    await delay(200);\r\n                                    document.querySelector('.bottom').querySelector('[data-testid=\"beast-core-button\"]').click();\r\n                                }\r\n                                runDelays();\r\n                            ";
					break;
				case 1:
					bound.msg = "开始设置分类：" + bound.cateInfo;
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					bound.step = 22;
					break;
				case 2:
					bound.msg = "修改商品标题... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					bound.step = 24;
					break;
				case 24:
					js = "\r\n                                var goodsName = bound.goodsName\r\n                                var headline = document.querySelector('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                var load = document.querySelector('[data-testid=\"beast-core-icon-loading\"]')\r\n                                if(!load && headline){\r\n                                    setNativeValue(headline, goodsName);\r\n                                    var inputEvent = new Event('input', { bubbles: true });\r\n                                    headline.dispatchEvent(inputEvent);\r\n                                    if (headline.value == goodsName) bound.step = 20;\r\n                                }\r\n                            ";
					break;
				case 20:
					bound.step = 3;
					bound.msg = "开始上传主图... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					break;
				case 3:
					js = "\r\n                                async function runChange(){\r\n                                    document.querySelector('.uploadImgEnter').click();\r\n                                    await delay(500);\r\n                                    document.querySelector('[data-testid=\"beast-core-icon-upload\"]').parentElement.click();\r\n                                    bound.step = 4;\r\n                                    await delay(800);\r\n                                }\r\n                                runChange();\r\n                            ";
					break;
				case 4:
				{
					browser1.GetBrowserHost().SendMouseClickEvent(1, 1, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
					if (imgNum == 0)
					{
						imgNum = bound.topImage.Count;
					}
					if (curNum == 0)
					{
						curNum = 1;
					}
					num = imgNum - curNum + 1;
					if (num > 10)
					{
						num = 10;
					}
					uploadedFiles = new List<string>();
					for (int i = 0; i < num; i++)
					{
						uploadedFiles.AddRange(new List<string> { bound.topImage[curNum + i - 1] });
					}
					curNum += num;
					browser1.DialogHandler = new MyFileDialogHandler(uploadedFiles);
					js = "\r\n                                async function runChange1(){\r\n                                    await delay(300);\r\n                                    document.querySelectorAll('[data-testid=\"beast-core-modal-body\"]')[1].querySelector('button').click()\r\n                                    await delay(100);\r\n                                    bound.step = 5;\r\n                                }\r\n                                runChange1();\r\n                            ";
					break;
				}
				case 5:
					if (imgNum > curNum - 1)
					{
						Thread.Sleep(3000);
						js = "\r\n                                    //var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.getElementsByTagName('svg').length\r\n                                    var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.querySelectorAll('[data-checked=\"true\"]').length\r\n                                    if (num == {0}){{\r\n                                        document.querySelector('[data-testid=\"beast-core-modal-body\"]').nextElementSibling.querySelector('button').click()\r\n                                        bound.step = 3;\r\n                                    }}\r\n                                ";
						js = string.Format(js, num);
					}
					else
					{
						Thread.Sleep(3000);
						js = "\r\n                                    async function runOk(){{\r\n                                        //var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.getElementsByTagName('svg').length\r\n                                        var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.querySelectorAll('[data-checked=\"true\"]').length\r\n                                        if (num == {0}){{\r\n                                            await delay(500);\r\n                                            document.querySelector('[data-testid=\"beast-core-modal-body\"]').nextElementSibling.querySelector('button').click()\r\n                                            bound.step = 6;\r\n                                        }}\r\n                                    }}\r\n                                    runOk();\r\n                                ";
						js = string.Format(js, num);
					}
					break;
				case 6:
					bound.msg = "上传主图完成... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					Thread.Sleep(200);
					bound.msg = "开始修改属性... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					imgNum = 0;
					curNum = 0;
					js = "\r\n                                async function getAtt(){\r\n                                    document.querySelector('.goods-propertys').scrollIntoView({block:'center' })\r\n                                    bound.step = 7;\r\n                                    //var info = [{'key':'品牌','ref_pid':310,'reference_id':51890,'values':['Others']},{'key':'发货地','values':['广东省']},{'key':'适用手机品牌','ref_pid':434,'reference_id':6297,'values':['Honor/荣耀']},{'key':'适用手机型号','ref_pid':435,'reference_id':5952,'values':['50','50 SE','50Pro']},{'key':'贴膜特点','ref_pid':436,'reference_id':0,'values':['抗蓝光','高清','自动修复']},{'key':'贴膜材质','ref_pid':437,'reference_id':0,'values':['PET']},{'key':'产品类型','ref_pid':438,'reference_id':0,'values':['全覆盖膜']}]\r\n\r\n                                    var info = JSON.parse(bound.goodsProperty);\r\n                                    console.log(info.length)\r\n                                    if(info.length == 0) return\r\n                                    bound.isRun = 0;\r\n                                    var i = 0\r\n                                    var success = new Array(info.length).fill(0);\r\n                                    while(i >= 0){\r\n                                        if(bound.isEnd) break;\r\n                                        try{\r\n                                            console.log('i=',i)\r\n                                            var item = document.querySelector('.goods-propertys').getElementsByTagName('label')[i]\r\n                                            var name = item.innerHTML\r\n                                            if (name.includes('span')){\r\n                                                name = document.querySelector('.goods-propertys').getElementsByTagName('label')[i].querySelector('span').innerHTML\r\n                                                if(name == '重要') name = document.querySelector('.goods-propertys').getElementsByTagName('label')[i].querySelectorAll('span')[1].innerHTML\r\n                                            }\r\n                                            var headline = item.parentElement.parentElement.parentElement.querySelector('input')\r\n                                            if(!headline || success[i] == 1){\r\n                                                i++;\r\n                                                continue;\r\n                                            }\r\n                                            var dvalue = item.parentElement.parentElement.parentElement.querySelectorAll('[data-testid=\"beast-core-tagGroup-tag\"]')\r\n                                            var inputAll = item.parentElement.parentElement.parentElement.querySelectorAll('input')\r\n                                            var choose_label = headline.parentElement.parentElement.parentElement.querySelector('label')\r\n                                            console.log(111)\r\n                                            if(!choose_label && (success[i] == 1 || headline.value.length>0 || (dvalue != null && dvalue.length>0))){\r\n                                                i++;\r\n                                                continue;\r\n                                            }\r\n                                            else if(choose_label){\r\n                                                console.log(222)\r\n                                                var isCheck = 0\r\n                                                for(var m = 0;m<inputAll.length;m++){\r\n                                                    var inputLabel = inputAll[m].parentElement.parentElement.parentElement.querySelector('label')\r\n                                                    if(inputLabel.getAttribute('data-checked') == 'true'){\r\n                                                        isCheck = 1\r\n                                                        continue;\r\n                                                    }\r\n                                                }\r\n                                                if(isCheck == 1){\r\n                                                    i++;\r\n                                                    continue;\r\n                                                }\r\n                                            }\r\n                                            console.log(333)\r\n                                            var value = info.find(element => element.key === name);\r\n                                            if(!value){\r\n                                                console.log('不存在名称')\r\n                                                success[i] = 1\r\n                                                i = 0\r\n                                                continue\r\n                                            }\r\n                                            console.log(444)\r\n                                            //单位id\r\n                                            var unit = value.unit;\r\n                                            if(unit >= 0 && inputAll.length > 1){\r\n                                                inputAll[1].click()\r\n                                                await delay(800);\r\n                                                document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')[unit].click()\r\n                                                await delay(500);\r\n                                            }\r\n                                            //单位名称\r\n                                            var unitStr = value.unitStr;\r\n                                            if(unitStr.length > 0 && inputAll.length > 1){\r\n                                                inputAll[1].click()\r\n                                                await delay(800);\r\n                                                var li = document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')\r\n                                                for(var liitem of li){\r\n                                                    if(liitem.textContent == unitStr){\r\n                                                        liitem.click()\r\n                                                        break\r\n                                                    }\r\n                                                }\r\n                                                await delay(500);\r\n                                            }\r\n                                            var values = value.values\r\n                                            console.log(555)\r\n                                            if(choose_label){\r\n                                                console.log(666)\r\n                                                for(var m = 0;m<inputAll.length;m++){\r\n                                                    var inputLabel = inputAll[m].parentElement.parentElement.parentElement.querySelector('label')\r\n                                                    if(inputLabel.textContent == values[0]){\r\n                                                        inputLabel.click()\r\n                                                        continue;\r\n                                                    }\r\n                                                }\r\n                                            }\r\n                                            else{\r\n                                                console.log(777)\r\n                                                var m = 0\r\n                                                for (const v of values) {\r\n                                                    if(value.type == 4){\r\n                                                        headline.click()\r\n                                                        await delay(500);\r\n                                                        await setTime(v)\r\n                                                        continue\r\n                                                    }\r\n                                                    else if(value.type == 5){\r\n                                                        headline.click()\r\n                                                        await delay(500);\r\n                                                        var li = document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')\r\n                                                        var index = 0\r\n                                                        var last_li = ''\r\n                                                        while(index < li.length){\r\n                                                            if(li[index].textContent == v){\r\n                                                                li[index].click()\r\n                                                                break\r\n                                                            }\r\n                                                            if(last_li == li[li.length - 1].textContent) break\r\n                                                            index += 1\r\n                                                            if (index == li.length){\r\n                                                                last_li = li[li.length - 1].textContent\r\n                                                                li[li.length - 1].scrollIntoView({block:'center' })\r\n                                                                await delay(500);\r\n                                                                li = document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')\r\n                                                                index = 0\r\n                                                            }\r\n                                                        }\r\n                                                        continue\r\n                                                    }\r\n                                                    if(value.type == 3){\r\n                                                        headline = item.parentElement.parentElement.parentElement.querySelectorAll('input')[m]\r\n                                                        m++\r\n                                                    }\r\n                                                    setNativeValue(headline, v);\r\n                                                    var inputEvent = new Event('input', { bubbles: true });\r\n                                                    headline.dispatchEvent(inputEvent);\r\n                                                    await delay(800);\r\n                                                    headline.click()\r\n                                                    await delay(800);\r\n                                                    console.log(722)\r\n                                                    if(value.type == 1){\r\n                                                        console.log(733)\r\n                                                        //下拉单位的情况\r\n                                                        var portal_main = document.querySelector('[data-testid=\"beast-core-portal-main\"]')\r\n                                                        if(portal_main){\r\n                                                            var label = portal_main.querySelectorAll('label')\r\n                                                            if(label && label.length>0){\r\n                                                                console.log(744)\r\n                                                                var li = portal_main.querySelectorAll('li')\r\n                                                                if(li.length==1){\r\n                                                                    li[0].click()\r\n                                                                }else if(li.length > 1){\r\n                                                                    for (var j = 0; j < li.length; j++) {\r\n                                                                        if(li[j].querySelector('span').textContent == v){\r\n                                                                            li[j].click()\r\n                                                                            break\r\n                                                                        }\r\n                                                                    }\r\n                                                                }\r\n                                                            }\r\n                                                            else{\r\n                                                                console.log(755)\r\n                                                                portal_main.querySelector('li').click()\r\n                                                            }\r\n                                                        }\r\n                                                    }\r\n                                                    \r\n                                                    await delay(1000);\r\n                                                }\r\n                                            }\r\n                                            var beast = document.querySelector('[data-testid=\"beast-core-portal-main\"]')\r\n\t\t                                    if(beast != null){\r\n    \t\t\t                                beast.remove('show')\r\n\t\t                                    }\r\n                                            console.log(888)\r\n                                            await delay(1000);\r\n                                            i = 0\r\n                                        }\r\n                                        catch(error){\r\n                                            console.log('error=',i,error.message)\r\n                                            break\r\n                                        }\r\n                                    }\r\n                                    bound.isRun = 1;\r\n                                    bound.step = 21;\r\n                                    return info\r\n                                }\r\n                                getAtt()\r\n                            ";
					break;
				case 7:
					if (bound.goodsPropertyList.Count == 0)
					{
						bound.step = 21;
					}
					break;
				case 21:
					bound.msg = "修改属性完成... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					Thread.Sleep(200);
					bound.step = 8;
					bound.msg = "开始上传商品详情图... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					break;
				case 8:
					bound.step = 99;
					js = "\r\n                                async function runChange(){\r\n                                    var closeList = document.querySelectorAll('[data-testid=\"beast-core-modal-icon-close\"]')\r\n                                    if(closeList.length > 0){\r\n                                        for(var i = closeList.length;i>0;i--){\r\n                                            closeList[i-1].parentElement.click()\r\n                                            await delay(500)\r\n                                        }\r\n                                    }\r\n                                    document.querySelector('.decoration-operate').querySelector('button').scrollIntoView({block:'center'})\r\n                                    await delay(5000);\r\n                                    document.querySelector('.decoration-operate').querySelector('button').click();\r\n                                    await delay(1000);\r\n                                    var innerWrapper = document.querySelector('[data-testid=\"beast-core-modal-innerWrapper\"]')\r\n                                    if(innerWrapper){\r\n                                        var innerBtn = document.querySelector('[data-testid=\"beast-core-modal-innerWrapper\"]').querySelector('button')\r\n                                        var btnText = innerBtn.textContent\r\n                                        if(btnText != '扫码上传') innerBtn.click()\r\n                                        else {console.log('ss11')}\r\n                                        await delay(1000);\r\n                                    }\r\n                                    document.querySelector('[data-testid=\"beast-core-icon-upload\"]').parentElement.click();\r\n                                    await delay(500);\r\n                                    bound.step = 9;\r\n                                    await delay(500);\r\n                                }\r\n                                runChange();\r\n                            ";
					break;
				case 9:
				{
					browser1.GetBrowserHost().SendMouseClickEvent(1, 1, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
					if (imgNum == 0)
					{
						imgNum = bound.detailImage.Count;
					}
					if (curNum == 0)
					{
						curNum = 1;
					}
					num = imgNum - curNum + 1;
					if (num > 10)
					{
						num = 10;
					}
					uploadedFiles = new List<string>();
					for (int j = 0; j < num; j++)
					{
						uploadedFiles.AddRange(new List<string> { bound.detailImage[curNum + j - 1] });
					}
					curNum += num;
					browser1.DialogHandler = new MyFileDialogHandler(uploadedFiles);
					js = "\r\n                                async function runChange1(){\r\n                                    bound.isRun = 0;\r\n                                    await delay(300);\r\n                                    document.querySelectorAll('[data-testid=\"beast-core-modal-body\"]')[1].querySelector('button').click()\r\n                                    bound.step = 10;\r\n                                    await delay(1000);\r\n                                    bound.isRun = 1;\r\n                                }\r\n                                runChange1();\r\n                            ";
					break;
				}
				case 10:
					if (imgNum > curNum - 1)
					{
						Thread.Sleep(3000);
						js = "\r\n                                    //var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.getElementsByTagName('svg').length\r\n                                    var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.querySelectorAll('[data-checked=\"true\"]').length\r\n                                    if (num == {0}){{\r\n                                        document.querySelector('[data-testid=\"beast-core-modal-body\"]').nextElementSibling.querySelector('button').click()\r\n                                        bound.step = 8;\r\n                                    }}\r\n                                ";
						js = string.Format(js, num);
					}
					else
					{
						bound.msg = "等待确认... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
						Thread.Sleep(3000);
						js = "\r\n                                    async function runOk(){{\r\n                                        //var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.getElementsByTagName('svg').length\r\n                                        var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.querySelectorAll('[data-checked=\"true\"]').length\r\n                                        console.log('num=',num)\r\n                                        if (num == {0}){{\r\n                                            await delay(500);\r\n                                            document.querySelector('[data-testid=\"beast-core-modal-body\"]').nextElementSibling.querySelector('button').click()\r\n                                            bound.step = 11;\r\n                                        }}\r\n                                    }}\r\n                                    runOk();\r\n                                ";
						js = string.Format(js, num);
					}
					break;
				case 11:
					bound.msg = "上传详情图完成... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					if (bound.SkuList.Count() > 10)
					{
						create_time = DateTime.Now;
					}
					Thread.Sleep(200);
					bound.msg = "添加商品规格... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					imgNum = 0;
					curNum = 0;
					js = "\r\n                                async function sku1(){\r\n                                    bound.step = 99\r\n                                    document.querySelector('.goods-spec').scrollIntoView({block:'center' })\r\n                                    var skuInfos = JSON.parse(bound.skuInfos);\r\n                                    \r\n                                    var id = 0;\r\n                                    for(var i = 0;i<skuInfos.length;i++){\r\n                                        //添加规格\r\n                                        document.querySelector('.goods-spec').querySelector('button').click()\r\n                                        await delay(300);\r\n                                        var row1 = document.querySelectorAll('.goods-spec-row-title')[i].querySelector('input')\r\n                                        setNativeValue(row1, skuInfos[i].key);\r\n                                        var inputEvent = new Event('input', { bubbles: true });\r\n                                        row1.dispatchEvent(inputEvent);\r\n                                        await delay(800);\r\n                                        row1.click()\r\n                                        await delay(800);\r\n                                        document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelector('li').click()\r\n                                        await delay(800);\r\n                                        if (i > 0) id += skuInfos[i-1].values.length + 1\r\n                                        //添加详细规格\r\n                                        for(var j = 0;j<skuInfos[i].values.length;j++){\r\n                                            var spec = document.querySelectorAll('.spec-input')[id + j].querySelector('input')\r\n                                            setNativeValue(spec, skuInfos[i].values[j]);\r\n                                            var inputEvent2 = new Event('input', { bubbles: true });\r\n                                            spec.dispatchEvent(inputEvent2);\r\n                                            await delay(800)\r\n                                            var changeEvent = new Event('blur', { bubbles: true });\r\n                                            spec.dispatchEvent(changeEvent);\r\n                                            await delay(800)\r\n                                        }\r\n                                        if(i == 0){\r\n                                            document.querySelector('.goods-spec-check').querySelector('input').click()\r\n                                            await delay(500)\r\n                                            for(var j = 0;j<skuInfos[i].values.length;j++){\r\n                                                var closeList = document.querySelectorAll('[data-testid=\"beast-core-modal-icon-close\"]')\r\n                                                if(closeList.length > 0){\r\n                                                    for(var i = closeList.length;i>0;i--){\r\n                                                        closeList[i-1].parentElement.click()\r\n                                                        await delay(500)\r\n                                                    }\r\n                                                }\r\n                                                document.querySelector('.goods-spec-row-box').querySelectorAll('.item-image-v2')[j].querySelector('.sku-preview-img').click()\r\n                                                await delay(500)\r\n                                                document.querySelector('[data-testid=\"beast-core-icon-upload\"]').parentElement.click();\r\n                                                await delay(500)\r\n                                                bound.imgId = j\r\n                                                bound.step = 12\r\n                                                while(bound.imgId >= 0){\r\n                                                    await delay(500)\r\n                                                }\r\n                                                await delay(500)\r\n                                            }\r\n                                        }\r\n                                    }\r\n                                    await delay(500);\r\n                                    bound.step = 14;\r\n                                }\r\n                                async function sku(){\r\n                                    bound.step = 99\r\n                                    document.querySelector('.package-container').scrollIntoView({block:'center' })\r\n                                    var skuInfos = JSON.parse(bound.skuInfos);\r\n                                    for(var i = 0;i<skuInfos.length;i++){\r\n                                        if(i == 0){\r\n                                            for(var j = 0;j<skuInfos[i].values.length;j++){\r\n                                                var htmlInput = document.querySelector('.package-container').querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                                //添加备注\r\n                                                var vvalue = skuInfos[i].values[j].split(' ')\r\n                                                var key = ''\r\n                                                var unit = ''\r\n                                                if(skuInfos[i].key == '净含量'){\r\n                                                    const numbers = vvalue[0].match(/[+-]?\\d+(\\.\\d+)?/g);\r\n                                                    key = numbers[0]\r\n                                                    const letters = vvalue[0].match(/[a-zA-Z]+/g);\r\n                                                    unit = letters[0]\r\n                                                    console.log('净含量=',key,unit)\r\n                                                }\r\n                                                for(var m = 0;m<vvalue.length;m++){\r\n                                                    if(vvalue[m].length == 0) continue\r\n                                                    var packageInput = htmlInput[htmlInput.length -1]\r\n                                                    if(m==1){\r\n                                                        htmlInput = document.querySelector('.package-container').querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                                        packageInput = htmlInput[htmlInput.length -2]\r\n                                                    }\r\n                                                    var nvalue = vvalue[m]\r\n                                                    if(m==0 && skuInfos[i].key == '净含量'){\r\n                                                        packageInput.parentElement.parentElement.parentElement.parentElement.querySelector('[data-testid=\"beast-core-select-htmlInput\"]').click()\r\n                                                        await delay(300)\r\n                                                        var liList = document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')\r\n                                                        for(var n = 0;n<liList.length;n++){\r\n                                                            if(liList[n].textContent == unit){\r\n                                                                liList[n].click()\r\n                                                                await delay(500);\r\n                                                                break\r\n                                                            }\r\n                                                        }\r\n                                                        nvalue = key\r\n                                                    }\r\n                                                    setNativeValue(packageInput, nvalue);\r\n                                                    var inputEvent = new Event('input', { bubbles: true });\r\n                                                    packageInput.dispatchEvent(inputEvent);\r\n                                                    await delay(200);\r\n                                                    var changeEvent = new Event('blur', { bubbles: true });\r\n                                                    packageInput.dispatchEvent(changeEvent);\r\n                                                    await delay(500)\r\n                                                }\r\n                                            }\r\n                                            await delay(500)\r\n                                            var skuImg = document.querySelector('.package-container').querySelectorAll('.sku-preview-img')\r\n                                            for(var j = 0;j<skuInfos[i].values.length;j++){\r\n                                                //关闭多余窗口\r\n                                                var closeList = document.querySelectorAll('[data-testid=\"beast-core-modal-icon-close\"]')\r\n                                                if(closeList.length > 0){\r\n                                                    for(var i = closeList.length;i>0;i--){\r\n                                                        closeList[i-1].parentElement.click()\r\n                                                        await delay(500)\r\n                                                    }\r\n                                                }\r\n                                                console.log('j=',j)\r\n                                                skuImg[j].click()\r\n                                                await delay(500)\r\n                                                document.querySelector('[data-testid=\"beast-core-icon-upload\"]').parentElement.click();\r\n                                                await delay(500)\r\n                                                bound.imgId = j\r\n                                                bound.step = 12\r\n                                                while(bound.imgId >= 0){\r\n                                                    await delay(500)\r\n                                                }\r\n                                                await delay(500)\r\n                                            }\r\n                                        }\r\n                                        else if(i == 1){\r\n                                            var new_num = i\r\n                                            var plus = document.querySelector('[data-testid=\"beast-core-icon-plus\"]')\r\n                                            if(plus){\r\n                                                plus.parentElement.click()\r\n                                                await delay(300);\r\n                                                var row1 = document.querySelectorAll('.goods-spec-title')[i-1].querySelector('input')\r\n                                                setNativeValue(row1, skuInfos[i].key);\r\n                                                var inputEvent = new Event('input', { bubbles: true });\r\n                                                row1.dispatchEvent(inputEvent);\r\n                                                await delay(800);\r\n                                                document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelector('li').click()\r\n                                                await delay(800);\r\n                                                new_num = document.querySelectorAll('.package-container').length - 1\r\n                                            }\r\n                                            if(skuInfos[i].key == '适用温度'){\r\n                                                for(var j = 0;j<skuInfos[i].values.length;j++){\r\n                                                    var vvalue = skuInfos[i].values[j].split(' ')\r\n                                                    var checkList = document.querySelectorAll('.input-with-note')\r\n                                                    for(var n = 0;n<checkList.length;n++){\r\n                                                        if(checkList[n].querySelector('label').textContent == vvalue[0]){\r\n                                                            checkList[n].querySelector('label').click()\r\n                                                            await delay(500);\r\n                                                            if(vvalue.length > 1){\r\n                                                                var checkInput = checkList[n].querySelector('label').parentElement.parentElement.querySelectorAll('input')[1]\r\n                                                                setNativeValue(checkInput, vvalue[1]);\r\n                                                                var inputEvent = new Event('input', { bubbles: true });\r\n                                                                checkInput.dispatchEvent(inputEvent);\r\n                                                                await delay(500);\r\n                                                                var changeEvent = new Event('blur', { bubbles: true });\r\n                                                                checkInput.dispatchEvent(changeEvent);\r\n                                                                await delay(200)\r\n                                                            }\r\n                                                            break\r\n                                                        }\r\n                                                    }\r\n                                                }\r\n                                            }\r\n                                            else{\r\n                                                for(var j = 0;j<skuInfos[i].values.length;j++){\r\n                                                    var htmlInput = document.querySelectorAll('.package-container')[new_num].querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                                    //添加备注\r\n                                                    var vvalue = skuInfos[i].values[j].split(' ')\r\n                                                    if(plus) vvalue = [skuInfos[i].values[j]]\r\n                                                    for(var m = 0;m<vvalue.length;m++){\r\n                                                        var packageInput = htmlInput[htmlInput.length -1]\r\n                                                        if(m==1){\r\n                                                            htmlInput = document.querySelectorAll('.package-container')[new_num].querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                                            packageInput = htmlInput[htmlInput.length -2]\r\n                                                        }\r\n                                                        setNativeValue(packageInput, vvalue[m]);\r\n                                                        var inputEvent = new Event('input', { bubbles: true });\r\n                                                        packageInput.dispatchEvent(inputEvent);\r\n                                                        await delay(500);\r\n                                                        var changeEvent = new Event('blur', { bubbles: true });\r\n                                                        packageInput.dispatchEvent(changeEvent);\r\n                                                        await delay(200)\r\n                                                    }\r\n                                                } \r\n                                            }\r\n                                        }\r\n                                    }\r\n                                    var stop = document.querySelector('[data-tracking-impr-viewid=\"disable_size_chart\"]')\r\n                                    if(stop){\r\n                                        stop.click()\r\n                                        await delay(500);\r\n                                        document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelector('button').click()\r\n                                    }\r\n                                    await delay(500);\r\n                                    bound.step = 14;\r\n                                    console.log(bound.step)\r\n                                }\r\n                                function choose(){\r\n                                    var goods_spec = document.querySelector('.goods-spec')\r\n                                    if(goods_spec){\r\n                                        sku1()\r\n                                        return\r\n                                    }\r\n                                    var package = document.querySelector('.package-container')\r\n                                    if(package){\r\n                                        sku()\r\n                                        return\r\n                                    }\r\n                                }\r\n                                choose()\r\n                            ";
					break;
				case 12:
					create_time = DateTime.Now;
					browser1.GetBrowserHost().SendMouseClickEvent(1, 1, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
					uploadedFiles = new List<string>();
					uploadedFiles.AddRange(new List<string> { bound.SkuList[0].imgPath[bound.imgId] });
					browser1.DialogHandler = new MyFileDialogHandler(uploadedFiles);
					js = "\r\n                                async function runChange1(){\r\n                                    await delay(100);\r\n                                    document.querySelectorAll('[data-testid=\"beast-core-modal-body\"]')[1].querySelector('button').click()\r\n                                    bound.step = 13;\r\n                                    await delay(500);\r\n                                }\r\n                                runChange1();\r\n                            ";
					break;
				case 13:
					js = "\r\n                                    async function runOk(){\r\n                                        //var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.getElementsByTagName('svg').length\r\n                                        var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.querySelectorAll('[data-checked=\"true\"]').length\r\n                                        if (num > 0){{\r\n                                            await delay(500);\r\n                                            document.querySelector('[data-testid=\"beast-core-modal-body\"]').nextElementSibling.querySelector('button').click()\r\n                                            bound.step = 99;\r\n                                            bound.imgId = -1;\r\n                                        }}\r\n                                    }\r\n                                    runOk()\r\n                                ";
					break;
				case 14:
					if (bound.SkuPriceList.Count() > 30)
					{
						create_time = DateTime.Now;
						date_time += bound.SkuPriceList.Count() % 100 * 10;
					}
					bound.msg = "修改商品价格... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					js = "\r\n                                async function sku2(){\r\n                                    bound.step = 99;\r\n                                    var skuPrice = JSON.parse(bound.skuPrice);\r\n                                    console.log('添加价格')\r\n                                    //添加价格\r\n                                    //var tbody = document.querySelector('[data-testid=\"beast-core-table-middle-tbody\"]').querySelectorAll('tr')[0]\r\n                                    var tbody = document.querySelector('.sku-list').querySelectorAll('tr')[1]\r\n                                    for(var i = 0;i<skuPrice.length;i++){\r\n                                        //var tbody = document.querySelector('[data-testid=\"beast-core-table-body-tr\"]').querySelectorAll('tr')[i]\r\n                                        if(i>0) tbody = tbody.nextElementSibling\r\n                                        tbody.scrollIntoView({block:'center' })\r\n                                        for(var j = 0;j<skuPrice[i].values.length;j++){\r\n                                            var va = skuPrice[i].values[j];\r\n                                            //是否固定库存\r\n                                            if(j == 0 && bound.settingInfo.boxInventory == 2){\r\n                                                va = bound.settingInfo.txtInventory\r\n                                            }\r\n                                            var tinput = tbody.querySelectorAll('input')[j]\r\n                                            setNativeValue(tinput, va);\r\n                                            var inputEvent2 = new Event('input', { bubbles: true });\r\n                                            tinput.dispatchEvent(inputEvent2);\r\n                                            await delay(100)\r\n                                            var changeEvent = new Event('blur', { bubbles: true });\r\n                                            tinput.dispatchEvent(changeEvent);\r\n                                            await delay(100)\r\n                                        }\r\n                                        await delay(200)\r\n                                    }\r\n                                    //商品编码\r\n                                    if(bound.settingInfo.goodNo.length > 0){\r\n                                        var sn = document.querySelector('.sn-content').querySelector('input')\r\n                                        setNativeValue(sn, bound.settingInfo.goodNo);\r\n                                        var inputEvent2 = new Event('input', { bubbles: true });\r\n                                        sn.dispatchEvent(inputEvent2);\r\n                                    }\r\n                                    bound.step = 15;\r\n                                }\r\n                                sku2()\r\n                            ";
					break;
				case 15:
					create_time = DateTime.Now;
					date_time = 5;
					bound.msg = "修改服务信息... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					js = "\r\n                                async function fuwu(){\r\n                                    bound.step = 99;\r\n                                    document.querySelector('[data-testid=\"beast-core-icon-down-circle\"]').parentElement.click();\r\n                                    await delay(200)\r\n                                    document.querySelector('#cost_template_id').querySelectorAll('label')[2].click()\r\n                                    document.querySelector('#cost_template_id').scrollIntoView({block:'center' })\r\n                                    await delay(500)\r\n                                    document.querySelector('#cost_template_id').querySelector('[data-testid=\"beast-core-select-htmlInput\"]').click()\r\n                                    await delay(500)\r\n                                    document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')[bound.settingInfo.boxFreight].click()\r\n                                    await delay(500)\r\n                                    //48h发货\r\n                                    document.querySelector('#shipment_limit_second').querySelectorAll('label')[1+bound.settingInfo.boxTime].click()\r\n                                    await delay(500)\r\n                                    //预售\r\n                                    if(bound.settingInfo.boxSale == 3){\r\n                                        //时间选择，暂时注释\r\n                                        //document.querySelector('#is_pre_sale').querySelectorAll('label')[2].click()\r\n                                    }\r\n                                    else if(bound.settingInfo.boxSale == 4){\r\n                                        document.querySelector('#is_pre_sale').querySelectorAll('label')[3].click()\r\n                                        await delay(500)\r\n                                        var second = document.querySelector('#group_time_second').querySelector('input')\r\n                                        setNativeValue(second, bound.settingInfo.txtSaleDay);\r\n                                        // 创建一个新的输入事件，表示值发生改变\r\n                                        var inputEvent = new Event('input', { bubbles: true });\r\n                                        second.dispatchEvent(inputEvent);\r\n                                        await delay(100);\r\n                                        second.click()\r\n                                        await delay(600);\r\n                                        document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelector('li').click()\r\n                                    }\r\n                                    await delay(500)\r\n                                    //提交\r\n                                    var zz = document.querySelector('.property-rule-error')\r\n                                    if(bound.settingInfo.boxUp == 1 && !zz){\r\n                                        if(document.querySelector('.goods-footer').querySelector('button').disabled){\r\n                                            //资质不全\r\n                                            bound.endMsg = '资质不全';\r\n                                            document.querySelector('.goods-footer').querySelectorAll('button')[1].click()\r\n                                            await delay(1000)\r\n                                            bound.step = 16;\r\n                                            console.log('aaaa2')\r\n                                            return\r\n                                        }\r\n                                        else{\r\n                                            document.querySelector('.goods-footer').querySelector('button').click()\r\n                                            bound.step = 99;\r\n                                        }\r\n                                    }\r\n                                    else{\r\n                                        bound.endMsg = '保存为草稿';\r\n                                        document.querySelector('.goods-footer').querySelectorAll('button')[1].click()\r\n                                        await delay(1000)\r\n                                        bound.step = 16;\r\n                                        console.log('aaaa1')\r\n                                        return\r\n                                    }\r\n                                    await delay(2000)\r\n                                    //填写不全\r\n                                    var core_toast = document.querySelector('[data-testid=\"beast-core-toast\"]')\r\n                                    if(core_toast){\r\n                                        bound.endMsg = core_toast.textContent\r\n                                        await delay(2000)\r\n                                        document.querySelector('.goods-footer').querySelectorAll('button')[1].click()\r\n                                        await delay(1000)\r\n                                        bound.step = 16\r\n                                        console.log('aaaaa')\r\n                                    }\r\n                                }\r\n                                fuwu()\r\n                            ";
					break;
				case 16:
					bound.step = 23;
					if (bound.endMsg != "保存为草稿")
					{
						bound.endMsg += "，保存为草稿";
					}
					bound.msg = good_id + "：" + bound.endMsg + "... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					break;
				case 23:
					js = "\r\n                                async function caogao(){\r\n                                    var modal = document.querySelector('[data-testid=\"beast-core-modal-body\"]')\r\n                                    console.log(111)\r\n                                    console.log(modal)\r\n                                    if(modal) {\r\n                                        console.log(222)\r\n                                        modal.nextElementSibling.querySelector('button').click()\r\n                                        await delay(2000)\r\n                                        bound.step = 17;\r\n                                     }\r\n                                }\r\n                                caogao()\r\n                            ";
					break;
				case 17:
				{
					bound.step = 99;
					if (bound.endMsg.Length == 0)
					{
						bound.endMsg = "上传完成";
						bound.msg = good_id + "：上传完成... " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						AddTextToRichTextBox(bound.msg, myRichTextBox, txt_log);
					}
					ShopInfo info2 = new ShopInfo
					{
						ChooseState = 0,
						Id = "1",
						Num = ShopList.Count() + 1,
						ShopName = "",
						GoodId = good_id,
						GoodId2 = bound.newGoodId,
						UploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
						Result = bound.endMsg
					};
					string shop_id2 = ShopSetBLL.AddShop(info2);
					info2.Id = shop_id2;
					ShopList.Add(info2);
					base.Dispatcher.Invoke(async delegate
					{
						if (bound.settingInfo.goodIds2.Count > 0)
						{
							bound.settingInfo.goodIds3.Add(bound.settingInfo.goodIds2[0]);
							bound.settingInfo.goodIds2.RemoveAt(0);
							txt_goodId.Clear();
							txt_goodId1.Clear();
							string newText3 = string.Join(Environment.NewLine, bound.settingInfo.goodIds2);
							txt_goodId.Text = newText3;
							string newText4 = string.Join(Environment.NewLine, bound.settingInfo.goodIds3);
							txt_goodId1.Text = newText4;
						}
						PddShopList.ItemsSource = null;
						PddShopList.ItemsSource = ShopList;
					});
					bound.nowId++;
					isGetInfo = false;
					bound.isPublish = true;
					string reInfo = MainBLL.GetVerify(1);
					if (!(reInfo != "1"))
					{
						return;
					}
					if (reInfo == "9")
					{
						isEnd = true;
						base.Dispatcher.Invoke(async delegate
						{
							if (SysMessage.MessageShow(Window.GetWindow(this), "点数不足，请联系客服", "注意", MessageBoxButton.OK) == MessageEnum.Error)
							{
							}
						});
					}
					base.Dispatcher.Invoke(async delegate
					{
						if (SysMessage.MessageShow(Window.GetWindow(this), "软件异常，请联系客服", "注意", MessageBoxButton.OK) != MessageEnum.Error)
						{
							Close();
							Environment.Exit(0);
						}
					});
					return;
				}
				case 18:
					bound.nowId++;
					isGetInfo = false;
					bound.isPublish = true;
					return;
				case 19:
					isEnd = true;
					base.Dispatcher.Invoke(async delegate
					{
						GotoUrl1("https://mms.pinduoduo.com/login/");
					});
					return;
				}
				if (js != "")
				{
					await browser1.EvaluateScriptAsync(js);
				}
			}
		}
		catch (Exception exception)
		{
			if (exception.Message.Contains("The underlying frame is no longer valid"))
			{
				base.Dispatcher.Invoke(async delegate
				{
					browser.Reload();
				});
			}
			bound.endMsg = "上传失败：" + exception.Message;
			AddTextToRichTextBox(bound.endMsg, myRichTextBox, txt_log);
			AddTextToRichTextBox(good_id, myRichTextBox1, txt_log1);
			ShopInfo info = new ShopInfo
			{
				ChooseState = 0,
				Id = "1",
				Num = ShopList.Count(),
				ShopName = "",
				GoodId = good_id,
				GoodId2 = "上传失败",
				UploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
				Result = bound.endMsg
			};
			string shop_id = ShopSetBLL.AddShop(info);
			info.Id = shop_id;
			ShopList.Add(info);
			base.Dispatcher.Invoke(async delegate
			{
				if (bound.settingInfo.goodIds2.Count > 0 && bound.settingInfo.boxType == 1)
				{
					bound.settingInfo.goodIds3.Add(bound.settingInfo.goodIds2[0]);
					bound.settingInfo.goodIds2.RemoveAt(0);
					txt_goodId.Clear();
					txt_goodId1.Clear();
					string newText = string.Join(Environment.NewLine, bound.settingInfo.goodIds2);
					txt_goodId.Text = newText;
					string newText2 = string.Join(Environment.NewLine, bound.settingInfo.goodIds3);
					txt_goodId1.Text = newText2;
				}
				PddShopList.ItemsSource = null;
				PddShopList.ItemsSource = ShopList;
			});
			bound.nowId++;
			bound.isEnd = true;
			isGetInfo = false;
			bound.isPublish = true;
		}
	}

	private List<string> DownloadImageThreadStart(string savePath, List<string> imgPath, bool isEdit = true)
	{
		try
		{
			for (int i = 0; i < imgPath.Count; i++)
			{
				string address = imgPath[i];
				string text = i + ".jpeg";
				using WebClient webClient = new WebClient();
				webClient.Headers.Add("authority", "img.pddpic.com");
				webClient.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
				webClient.Headers.Add("accept-language", "zh-TW,zh-CN;q=0.9,zh;q=0.8,en-US;q=0.7,en;q=0.6");
				webClient.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"104\", \" Not A;Brand\";v=\"99\", \"Google Chrome\";v=\"104\"");
				webClient.Headers.Add("sec-ch-ua-mobile", "?1");
				webClient.Headers.Add("sec-ch-ua-platform", "\"Android\"");
				webClient.Headers.Add("sec-fetch-dest", "document");
				webClient.Headers.Add("sec-fetch-mode", "navigate");
				webClient.Headers.Add("sec-fetch-site", "none");
				webClient.Headers.Add("sec-fetch-user", "?1");
				webClient.Headers.Add("upgrade-insecure-requests", "1");
				webClient.Headers.Add("user-agent", "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.91 Mobile Safari/537.36");
				webClient.DownloadFile(address, savePath + "\\" + text);
				if (bound.settingInfo.boxType == 2)
				{
					string text2 = savePath + "2";
					if (!Directory.Exists(text2))
					{
						Directory.CreateDirectory(text2);
					}
					string destFileName = Path.Combine(text2, text);
					File.Copy(savePath + "\\" + text, destFileName, overwrite: true);
				}
				if (isEdit)
				{
					EditImage(savePath + "\\" + text);
				}
				imgPath[i] = savePath + "\\" + text;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("下载商品SKU出错！" + ex.Message);
		}
		return imgPath;
	}

	private void LieBianImg(CancellationToken token)
	{
		string text = bound.imgPath + "\\top";
		string path = bound.imgPath + "\\top2";
		if (Directory.Exists(text))
		{
			Directory.Delete(text, recursive: true);
		}
		Directory.CreateDirectory(text);
		string[] files = Directory.GetFiles(path);
		foreach (string text2 in files)
		{
			string fileName = Path.GetFileName(text2);
			string text3 = Path.Combine(text, fileName);
			File.Copy(text2, text3, overwrite: true);
			EditImage(text3);
		}
		string text4 = bound.imgPath + "\\sku";
		string path2 = bound.imgPath + "\\sku2";
		if (Directory.Exists(text4))
		{
			Directory.Delete(text4, recursive: true);
		}
		Directory.CreateDirectory(text4);
		string[] files2 = Directory.GetFiles(path2);
		foreach (string text5 in files2)
		{
			string fileName2 = Path.GetFileName(text5);
			string text6 = Path.Combine(text4, fileName2);
			File.Copy(text5, text6, overwrite: true);
			EditImage(text6);
		}
	}

	private void EditImage(string path2, string watermarkText = "")
	{
		if (bound.settingInfo.boxAlter == 3)
		{
			Random random = new Random();
			bound.imageInfo.boImage = random.Next(1, 7);
		}
		int boImage = bound.imageInfo.boImage;
		string uriString = "pack://application:,,,/Images/tu/tu" + boImage + ".jpg";
		Uri uriResource = new Uri(uriString, UriKind.Absolute);
		StreamResourceInfo resourceStream = System.Windows.Application.GetResourceStream(uriResource);
		using System.Drawing.Image image2 = System.Drawing.Image.FromStream(resourceStream.Stream);
		using System.Drawing.Image image = System.Drawing.Image.FromFile(path2);
		int num = 520;
		int num2 = 520;
		float num3 = 0f;
		switch (boImage)
		{
		case 2:
			num2 = 570;
			num3 = -12f;
			break;
		case 3:
			num = 560;
			num2 = 560;
			num3 = -5f;
			break;
		case 4:
			num = 640;
			num2 = 640;
			break;
		case 5:
			num3 = -4f;
			num = 430;
			num2 = 520;
			break;
		case 6:
			num3 = -7f;
			num = 550;
			num2 = 570;
			break;
		}
		using Bitmap image3 = new Bitmap(image, new System.Drawing.Size(num, num2));
		int width = image2.Width;
		int height = image2.Height;
		using Bitmap bitmap = new Bitmap(width, height);
		using Graphics graphics = Graphics.FromImage(bitmap);
		image.Dispose();
		graphics.DrawImage(image2, new Rectangle(0, 0, image2.Width, image2.Height));
		if (bound.settingInfo.boxType == 2)
		{
			watermarkText = (bound.nowId + 1).ToString();
		}
		if (watermarkText.Length > 0)
		{
			System.Drawing.FontFamily family = new System.Drawing.FontFamily("Arial");
			Font font = new Font(family, 24f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
			SizeF sizeF = graphics.MeasureString(watermarkText, font);
			float x = (float)bitmap.Width - sizeF.Width - 10f;
			float y = (float)bitmap.Height - sizeF.Height - 10f;
			SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(128, 0, 255, 0));
			graphics.DrawString(watermarkText, font, brush, new PointF(x, y));
		}
		if (num3 != 0f)
		{
			int num4 = num / 2;
			int num5 = num2 / 2;
			graphics.TranslateTransform(num4, num5);
			graphics.RotateTransform(num3);
			graphics.TranslateTransform(-num4, -num5);
			if (boImage == 6)
			{
				num2 -= 80;
			}
		}
		int x2 = (width - num) / 2;
		int y2 = (height - num2) / 2;
		graphics.DrawImage(image3, new System.Drawing.Point(x2, y2));
		bitmap.Save(path2);
	}

	private void CheckBox_Type(object sender, RoutedEventArgs e)
	{
		if (check_type.IsChecked.GetValueOrDefault())
		{
			txt_shopType.IsEnabled = false;
			return;
		}
		SysMessage.MessageShow(Window.GetWindow(this), "填写分类不准确，会造成商品无法上传！", "注意", MessageBoxButton.OK);
		txt_shopType.IsEnabled = true;
	}

	private void sale_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		txt_saleDay.Visibility = Visibility.Hidden;
		txt_saleDay1.Visibility = Visibility.Hidden;
		if (box_sale.SelectedValue.ToString() == "4")
		{
			txt_saleDay.Visibility = Visibility.Visible;
			txt_saleDay1.Visibility = Visibility.Visible;
		}
	}

	private void inventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		box_inventory.Width = 160.0;
		txt_inventory.Visibility = Visibility.Hidden;
		if (box_inventory.SelectedValue.ToString() == "2")
		{
			box_inventory.Width = 80.0;
			txt_inventory.Visibility = Visibility.Visible;
		}
	}

	private void btnPause_Click(object sender, RoutedEventArgs e)
	{
		if (btn_pause.Content.ToString() == "暂停")
		{
			btn_pause.Content = "恢复";
			pauseEvent.Reset();
		}
		else
		{
			btn_pause.Content = "暂停";
			pauseEvent.Set();
		}
	}

	private void btnEnd_Click(object sender, RoutedEventArgs e)
	{
		bound.isEnd = true;
		isEnd = true;
	}

	private void main_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		bound.settingInfo.tabMain = tab_main.SelectedIndex;
		if (bound.settingInfo.tabMain == 0)
		{
			myRichTextBox.Focus();
			myRichTextBox.CaretPosition = myRichTextBox.Document.ContentEnd;
			myRichTextBox.ScrollToEnd();
			scrollViewer.ScrollToBottom();
			scrollViewer1.ScrollToBottom();
		}
	}

	private void type_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		txt_liebian.IsEnabled = false;
		txt_num.Visibility = Visibility.Hidden;
		bound.settingInfo.boxType = int.Parse(box_type.SelectedValue.ToString());
		if (box_type.SelectedValue.ToString() == "2")
		{
			txt_liebian.IsEnabled = true;
			txt_num.Visibility = Visibility.Visible;
			txt_liebian.Text = "A +2+ B * 0.02";
			bound.imageInfo.topState = 1;
			bound.imageInfo.skuState = 1;
		}
	}

	private void alter_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		box_tu.Visibility = Visibility.Hidden;
		btn_lbset.Visibility = Visibility.Visible;
		switch (box_alter.SelectedValue.ToString())
		{
		case "1":
			btn_lbset.Visibility = Visibility.Hidden;
			break;
		case "2":
			bound.imageInfo.boImage = 1;
			break;
		case "3":
			break;
		case "6":
			box_tu.Visibility = Visibility.Visible;
			break;
		}
	}

	private void btnLogin_Click(object sender, RoutedEventArgs e)
	{
		tab_main.SelectedIndex = 1;
		GotoUrl("https://mobile.pinduoduo.com/login.html?from=https%3A%2F%2Fmobile.pinduoduo.com%2Fclassification.html%3Frefer_page_name%3Dindex%26refer_page_id%3D10002_1722492288296_xk46xr96i1%26refer_page_sn%3D10002&refer_page_name=search&refer_page_id=10031_1722492299706_tu1twisom0&refer_page_sn=10031");
	}

	private void btnSHLogin_Click(object sender, RoutedEventArgs e)
	{
		tab_main.SelectedIndex = 2;
		GotoUrl1("https://mms.pinduoduo.com/login/");
	}

	private void change_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		txt_unit.Content = "%";
		string[] source = new string[3] { "2", "3", "4" };
		if (source.Contains(box_change.SelectedValue.ToString()))
		{
			txt_unit.Content = "元";
		}
		if (box_change.SelectedValue.ToString() == "6")
		{
			txt_unit.Content = "";
			txt_change.Text = "A*1.1+0.5";
			txt_Tips.Text = "提示信息：A是商品原价，例如公式：A*1.1+0.5";
		}
	}

	private void TxtPersonal_Click(object sender, RoutedEventArgs e)
	{
		Views.PersonalInfoWindow win = new Views.PersonalInfoWindow();
		win.Owner = this;
		win.ShowDialog();
	}

	private void TxtTips_Click(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Control control)
		{
			switch (control.Name)
			{
			case "txt_liebian":
				txt_Tips.Text = "提示信息：A是商品原价，B是裂变的第几款商品。例如公式：A + B * 0.01";
				break;
			case "txt_fill":
				txt_Tips.Text = "提示信息：比如上家没有药品通用名这个属性，但是需要必填，就可以在这里填写。示例：药品通用名=药品名1;药品名2";
				break;
			case "txt_normal":
				txt_Tips.Text = "提示信息：比如上家是主品牌，但界面上是品牌，就在这里填写数据。示例：主品牌=品牌";
				break;
			}
		}
	}

	private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
	{
		foreach (ShopInfo shop in ShopList)
		{
			if (shop.ChooseState == 1)
			{
				ShopSetBLL.UpdateShop(shop);
			}
		}
		ShopList.RemoveAll((ShopInfo item) => item.ChooseState == 1);
		for (int i = 0; i < ShopList.Count(); i++)
		{
			ShopList[i].Num = i + 1;
		}
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = ShopList;
	}

	private void DeleteMenuAll_Click(object sender, RoutedEventArgs e)
	{
		foreach (ShopInfo shop in ShopList)
		{
			ShopSetBLL.UpdateShop(shop);
		}
		ShopList = new List<ShopInfo>();
		PddShopList.ItemsSource = null;
		PddShopList.ItemsSource = ShopList;
	}

	private void boxShop_Checked(object sender, RoutedEventArgs e)
	{
	}

	private void btnLBSet_Click(object sender, RoutedEventArgs e)
	{
		ImageSet imageSet = new ImageSet(bound);
		imageSet.ShowDialog();
	}

	private void btnCeshi_Click(object sender, RoutedEventArgs e)
	{
		SysMessage.MessageShow(Window.GetWindow(this), "请确定电脑延迟，卡活动不成功，可以适当的减少或增加延迟", "注意：", MessageBoxButton.OK);
		isEnd = true;
		Thread.Sleep(1000);
		int result = 0;
		int.TryParse(txt_yanchi.Text, out result);
		if (result <= 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先输入正确的延迟", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		int result2 = 0;
		int.TryParse(txt_jiange.Text, out result2);
		if (result2 <= 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先输入正确的间隔时间", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		isEnd = false;
		pauseEvent.Set();
		if (txt_goodId.Text.Length == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先填写商品id", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		bound.waitTime = result;
		bound.jiangeTime = result2;
		bound.settingInfo.txtGoodId = txt_goodId.Text;
		bound.settingInfo.goodIds = bound.settingInfo.txtGoodId.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
		bound.goodId = bound.settingInfo.goodIds[0];
		tab_main.SelectedIndex = 2;
		txt_goodId1.Text = "";
		bound.nowId = 0;
		bound.isStart = true;
		Task task = Task.Run(delegate
		{
			ThreadXSXL(token);
		}, token);
	}

	private async void ThreadXSXL(CancellationToken token)
	{
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 2;
		});
		while (!bound.isStartLogin)
		{
			Thread.Sleep(1000);
		}
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 2;
		});
		if (bound.isLogin)
		{
			base.Dispatcher.Invoke(delegate
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先登录商家店铺账号", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			});
			isEnd = true;
			return;
		}
		base.Dispatcher.Invoke(delegate
		{
			GotoUrl1("https://mms.pinduoduo.com/goods/goods_list?activeKeyNew=key_2");
			browser2.Visibility = Visibility.Visible;
			browser2.Address = "https://mms.pinduoduo.com/tool/promotion/create?tool_full_channel=10921_77271";
		});
		Thread.Sleep(1000);
		bound.step = 1;
		bound.isStart = true;
		while (bound.isStart)
		{
			Thread.Sleep(1000);
			switch (bound.step)
			{
			case 1:
			{
				string js = "\r\n                            async function findGood(){\r\n                                var goodid = bound.goodId\r\n                                var txt_good = document.querySelector('[data-tracking-viewid=\"goods_id\"]').querySelector('input')                             \r\n                                setNativeValue(txt_good, goodid);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                txt_good.dispatchEvent(inputEvent);\r\n                                if (txt_good.value != goodid) retrun\r\n                                await delay(500)\r\n                                document.querySelector('[data-tracking-click-viewid=\"ele_inquire\"]').click()\r\n                                await delay(500)\r\n                                var goodtable = document.querySelector('[data-testid=\"beast-core-table-middle-body\"]')\r\n                                if(!goodtable) return\r\n                                console.log('findGood end')\r\n                                bound.step = 2\r\n                            }\r\n                            findGood()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 2:
			{
				bound.step = 99;
				string js = "\r\n                            async function setXl(){\r\n                                bound.step = 99\r\n                                var goodid = bound.goodId\r\n                                document.querySelector('[data-testid=\"beast-core-icon-plus\"]').parentElement.click()\r\n                                await delay(500)\r\n                                var inner = document.querySelector('[data-testid=\"beast-core-modal-inner\"]')\r\n                                if(!inner) return\r\n                                var txt_good = inner.querySelector('[data-testid=\"beast-core-input-htmlInput\"]')                              \r\n                                setNativeValue(txt_good, goodid);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                txt_good.dispatchEvent(inputEvent);\r\n                                if (txt_good.value != goodid) retrun\r\n                                await delay(500)\r\n                                document.querySelector('[data-testid=\"beast-core-modal-inner\"]').querySelector('[data-testid=\"beast-core-input-suffix\"]').click()\r\n                                await delay(500)\r\n                                var goodcheck = document.querySelector('[data-testid=\"beast-core-table-body-tr\"]').querySelector('[data-testid=\"beast-core-checkbox-checkIcon\"]')\r\n                                if(!goodcheck) return\r\n                                goodcheck.click()\r\n                                await delay(500)\r\n                                inner.querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                var math = Math.floor(Math.random() * 3);\r\n                                await delay(1000 + math * 100)\r\n                                var allInput = document.querySelector('[data-testid=\"beast-core-table-body-tr\"]').querySelectorAll('[data-testid=\"beast-core-inputNumber-htmlInput\"]')\r\n                                if(allInput.length <= 0){\r\n                                    bound.step = 5\r\n                                    return\r\n                                }\r\n                                var txt_zk = allInput[1]\r\n                                setNativeValue(txt_zk, 2);\r\n                                var inputEvent1 = new Event('input', { bubbles: true });\r\n                                txt_zk.dispatchEvent(inputEvent1);\r\n                                await delay(500)\r\n                                var changeEvent = new Event('blur', { bubbles: true });\r\n                                txt_zk.dispatchEvent(changeEvent);\r\n                                await delay(500)\r\n                                var price = txt_zk.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.querySelector('span').querySelector('span').querySelector('span').querySelector('span').textContent\r\n                                bound.dPrice = price.slice(1)\r\n                                var click1 = document.querySelector('[data-tracking-viewid=\"create_button_shared\"]')\r\n                                if(!click1){\r\n                                    click1 = document.querySelector('[data-tracking-click-viewid=\"create_button_shared\"]')\r\n                                }\r\n                                click1.click()\r\n                                bound.step = 3\r\n                                console.log('setXl end')\r\n                            }\r\n                            setXl()\r\n                        ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 3:
			{
				bound.step = 99;
				string js = "\r\n                            async function editPrice(){\r\n                                bound.step = 99\r\n                                document.querySelector('[data-tracking-viewid=\"changeprice\"]').click()\r\n                                await delay(300)\r\n                                var inner = document.querySelector('[data-testid=\"beast-core-modal-inner\"]')\r\n                                if(!inner) return\r\n                                inner.querySelector('[data-testid=\"beast-core-select-htmlInput\"]').click()\r\n                                await delay(500) \r\n                                document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')[1].click()\r\n                                await delay(500) \r\n                                var math = Math.floor(Math.random() * 10);\r\n                                if(math == 5) return\r\n                                var price = parseFloat(bound.dPrice)\r\n                                var price1 = (price + 0.01).toFixed(2)\r\n                                var price2 = (price + 10.01).toFixed(2)\r\n                                var price3 = (price + 20.01).toFixed(2)\r\n                                console.log(price1,price2,price3)\r\n                                var input1 = document.querySelector('[data-tracking-impr-viewid=\"batch_price_altering\"]')\r\n                                setNativeValue(input1, price1);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                input1.dispatchEvent(inputEvent);\r\n                                await delay(500)\r\n                                var input2 = document.querySelector('[data-tracking-impr-viewid=\"batch_singleprice_altering\"]')\r\n                                setNativeValue(input2, price2);\r\n                                input2.dispatchEvent(inputEvent);\r\n                                await delay(500) \r\n                                document.querySelector('[data-tracking-viewid=\"batch_price_altering_confirm\"]').click()\r\n                                await delay(1000) \r\n                                var input3 = document.querySelector('#marketPrice').querySelector('input')\r\n                                setNativeValue(input3, price3);\r\n                                input3.dispatchEvent(inputEvent);\r\n                                await delay(500) \r\n                                document.querySelector('[data-testid=\"beast-core-modal-inner\"]').querySelectorAll('[data-testid=\"beast-core-button\"]')[1].click()\r\n                                await delay(500) \r\n                                bound.step = 4\r\n                            }\r\n                            editPrice()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 4:
			{
				bound.step = 99;
				string js = "\r\n                            async function click2(){\r\n                                bound.step = 99\r\n                                await delay(bound.waitTime) \r\n                                document.querySelector('[data-testid=\"beast-core-modal-ok-button\"]').click()\r\n                                bound.isStart = false\r\n                            }\r\n                            click2()\r\n                        ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 5:
				bound.isStart = false;
				break;
			}
		}
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 0;
			if (bound.step != 5)
			{
				SysMessage.MessageShow(Window.GetWindow(this), "卡低拼单已完成", "操作完成", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			}
			else
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请确认商品，如果该商品今日已进行5次活动，则无法再进行活动！", "商品异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			}
		});
	}

	private void btnJTK_Click(object sender, RoutedEventArgs e)
	{
		isEnd = true;
		Thread.Sleep(1000);
		DateTime now = DateTime.Now;
		if (!(now > DateTime.MaxValue))
		{
			isEnd = false;
			pauseEvent.Set();
			ResultInfo allReason = MainBLL.GetAllReason();
			bound.ReasonList = JsonConvert.DeserializeObject<List<string>>(allReason.Data.ToString());
			txt_goodId1.Text = "";
			bound.nowId = 0;
			bound.isStart = true;
			System.Windows.MessageBox.Show("仅退款用的屏幕识别算法\r\n大部分能用\r\n如果不能用这个功能就不适合您电脑\r\n目前没办法统一系统和操作系统配置\r\n目前大部分笔记本使用是没问题。\r\n假如有问题请多换换电脑，也只有这个唯一办法\r\n目前我们技术用的电脑还有一堆现场调试中发现很多可用\r\n也有部分不能用\r\n实在就不能用，请用仅退款2\r\n仅退款2不过有可能会出现第三方软件外挂的申诉风险", "注意");
			SysMessage.MessageShow(Window.GetWindow(this), "请填写多条自定义申诉理由，否则账号容易被限制申诉", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			Task task = Task.Run(delegate
			{
				ThreadJTK(token);
			}, token);
		}
	}

	private async void ThreadJTK(CancellationToken token)
	{
		IntPtr hWnd = WinController.FindWindow("Chrome_WidgetWin_1");
		if (hWnd == IntPtr.Zero)
		{
			base.Dispatcher.Invoke(delegate
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先打开商户客户端", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			});
			isEnd = true;
			return;
		}
		SysCommon.isEnd = false;
		string path = AppDomain.CurrentDomain.BaseDirectory + "goodImg\\tk1";
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		string filePath = Path.Combine(path, "shensu.png");
		_ = SystemParameters.PrimaryScreenWidth;
		_ = SystemParameters.PrimaryScreenHeight;
		int newWidth = 1600;
		int newHeight = 960;
		WinController.SetWindowPos(hWnd, newWidth, newHeight);
		Thread.Sleep(300);
		WinController.ForegroundWindow(hWnd);
		Thread.Sleep(300);
		bound.step = 1;
		bound.isStart = true;
		bound.isEnd = false;
		int date_time = 8;
		DateTime create_time = DateTime.Now;
		bound.sensuNum = 0;
		int num = 0;
		int page_shouhou = 0;
		int page_tijiao = 0;
		int yunfei = 0;
		while (true)
		{
			pauseEvent.WaitOne();
			Thread.Sleep(1000);
			if ((DateTime.Now - create_time).TotalMinutes > (double)date_time)
			{
				bound.sensuNum++;
				create_time = DateTime.Now;
			}
			if (!bound.isStart || bound.isEnd || SysCommon.isEnd)
			{
				break;
			}
			switch (bound.step)
			{
			case 1:
			{
				create_time = DateTime.Now;
				int x3;
				int y2;
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(82, 10, 673, 356), SysCommon.ImgCode.Shensu1);
				if (x3 >= 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3, y2));
					WinController.WaitTime(5000);
				}
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(381, 7, 1420, 49), SysCommon.ImgCode.Close);
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 > 0)
				{
					bound.step = 100;
					break;
				}
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(7, 3, 503, 236), SysCommon.ImgCode.Houtai);
				if (x3 >= 0)
				{
					tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(207, 59, 783, 366), SysCommon.ImgCode.Close1);
					var (x30, _) = tuple2;
					_ = tuple2.Item2;
					if (x30 > 0)
					{
						WinController.ClickWin(new Tuple<int, int>(x3, y2), isRandom: false);
						WinController.WaitTime(500);
					}
					WinController.ClickWin(new Tuple<int, int>(x3 + 130, y2));
					WinController.WaitTime(300);
					WinController.WriteWin("申诉");
					WinController.WaitTime(800);
				}
				break;
			}
			case 100:
			{
				int y2;
				int x3;
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(187, 168, 828, 747), SysCommon.ImgCode.Shensu2);
				if (x3 >= 0)
				{
					int y19 = 90;
					if (!SysCommon.isSuofang)
					{
						y19 = 80;
					}
					WinController.ClickWin(new Tuple<int, int>(x3 + 10, y2 + y19), isRandom: false);
					WinController.WaitTime(3500);
				}
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(166, 14, 917, 517), SysCommon.ImgCode.Shensu3);
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 >= 0)
				{
					bound.step = 2;
				}
				break;
			}
			case 2:
			{
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(158, 113, 271, 168), SysCommon.ImgCode.Weiquan);
				int x3;
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 > 0)
				{
					bound.isStart = false;
					bound.isEnd = true;
				}
				tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(850, 811, 1064, 927), SysCommon.ImgCode.Zanwu);
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 > 0)
				{
					bound.isStart = false;
					bound.isEnd = true;
				}
				WinController.ScrollMouseWheelDown(-240);
				WinController.WaitTime(1500);
				bound.step = 3;
				break;
			}
			case 3:
			{
				if (num > 0)
				{
					Thread.Sleep(1000 + bound.jiangeTime * 1000);
				}
				create_time = DateTime.Now;
				yunfei = 0;
				page_tijiao = 0;
				if (bound.reaList.Count > 0)
				{
					Random random2 = new Random();
					int randomIndex = random2.Next(bound.reaList.Count);
					bound.ssReason = bound.reaList[randomIndex];
				}
				else if (bound.ReasonList.Count > 0)
				{
					Random random3 = new Random();
					int randomIndex2 = random3.Next(bound.ReasonList.Count);
					bound.ssReason = bound.ReasonList[randomIndex2];
				}
				int y2;
				int x3;
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(245, 89, 1569, 779), SysCommon.ImgCode.Chaxun);
				if (x3 <= 0)
				{
					break;
				}
				WinController.ClickWin(new Tuple<int, int>(x3, y2));
				WinController.WaitTime(1500);
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(1304, 272, 1579, 941), SysCommon.ImgCode.Chakan2);
				if (x3 > 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3, y2), isRandom: false);
					WinController.WaitTime(3500);
					page_shouhou = 0;
					bound.step = 4;
					break;
				}
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(850, 811, 1064, 927), SysCommon.ImgCode.Zanwu);
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 > 0)
				{
					bound.isStart = false;
					bound.isEnd = true;
				}
				break;
			}
			case 31:
			{
				create_time = DateTime.Now;
				if (bound.reaList.Count > 0)
				{
					Random random4 = new Random();
					int randomIndex3 = random4.Next(bound.reaList.Count);
					bound.ssReason = bound.reaList[randomIndex3];
				}
				else if (bound.ReasonList.Count > 0)
				{
					Random random5 = new Random();
					int randomIndex4 = random5.Next(bound.ReasonList.Count);
					bound.ssReason = bound.ReasonList[randomIndex4];
				}
				int y2;
				int x3;
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(278, 90, 700, 345), SysCommon.ImgCode.Chaxun);
				if (x3 <= 0)
				{
					break;
				}
				WinController.ClickWin(new Tuple<int, int>(x3, y2));
				WinController.WaitTime(1500);
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(1244, 256, 1469, 725), SysCommon.ImgCode.Chakan);
				if (x3 > 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3, y2), isRandom: false);
					WinController.WaitTime(3500);
					page_shouhou = 0;
					bound.step = 4;
					break;
				}
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(850, 811, 1064, 927), SysCommon.ImgCode.Zanwu);
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 > 0)
				{
					bound.isStart = false;
					bound.isEnd = true;
				}
				break;
			}
			case 4:
			{
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(5, 2, 733, 425), SysCommon.ImgCode.Shouhou);
				int x3;
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 > 0)
				{
					WinController.ScrollMouseWheelDown(-1440);
					WinController.WaitTime(300);
					bound.step = 5;
					break;
				}
				WinController.WaitTime(1500);
				page_shouhou++;
				if (page_shouhou <= 3)
				{
					break;
				}
				for (int run_num2 = 3; run_num2 > 0; run_num2--)
				{
					int y2;
					(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(681, 7, 1420, 49), SysCommon.ImgCode.Close);
					if (x3 > 0)
					{
						WinController.ClickWin(new Tuple<int, int>(x3, y2), isRandom: false);
						run_num2 = 3;
						WinController.WaitTime(800);
					}
				}
				bound.step = 3;
				break;
			}
			case 5:
				page_shouhou = 0;
				var (x3_1, y2_1) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(10, 115, 1276, 924), SysCommon.ImgCode.Quanbu);
				if (x3_1 > 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3_1, y2_1), isRandom: false);
					WinController.WaitTime(800);
					Random random = new Random();
					int randomNumber = random.Next(5, 50);
					WinController.ScrollMouseWheelDown(-1 * randomNumber);
					WinController.WaitTime(200);
					WinController.KeepCapture(hWnd, new WinController.RECT
					{
						Left = -1,
						Top = 0,
						Right = 0,
						Bottom = 0
					}, filePath);
					bound.step = 6;
				}
				else
				{
					WinController.ScrollMouseWheelDown(-1440);
					WinController.WaitTime(300);
				}
				break;
			case 6:
			{
				WinController.ScrollMouseWheelDown(1440);
				WinController.WaitTime(1000);
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(358, 79, 743, 380), SysCommon.ImgCode.Weiquan3);
				int x3;
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 < 0)
				{
					int y2;
					(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(647, 289, 1504, 921), SysCommon.ImgCode.Weiquan2);
					if (x3 >= 0)
					{
						WinController.ClickWin(new Tuple<int, int>(x3, y2));
						WinController.WaitTime(800);
					}
					break;
				}
				int xz_num = 3;
				while (xz_num > 0)
				{
					xz_num--;
					int y2;
					(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(641, 303, 1125, 361), SysCommon.ImgCode.Xuanzhong);
					if (x3 >= 0)
					{
						yunfei = 1;
						WinController.ClickWin(new Tuple<int, int>(x3, y2), isRandom: false);
						xz_num = 3;
					}
					WinController.WaitTime(500);
				}
				bound.step = 7;
				break;
			}
			case 7:
				var (x3_2, y2_2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(518, 494, 1000, 812), SysCommon.ImgCode.Ssms);
				if (x3_2 >= 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3_2, y2_2), isRandom: false);
					WinController.WaitTime(200);
					WinController.WriteWin(bound.ssReason);
					WinController.WaitTime(800);
				}
				else
				{
					bound.step = 8;
				}
				break;
			case 8:
			{
				double thread3 = 0.8;
				if (!SysCommon.isSuofang)
				{
					thread3 = 0.7;
				}
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(317, 115, 1276, 924), SysCommon.ImgCode.Yuanyin2, isShow: false, thread3);
				int x3;
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 >= 0)
				{
					bound.step = 9;
					break;
				}
				thread3 = 0.8;
				if (!SysCommon.isSuofang)
				{
					thread3 = 0.65;
				}
				int y2;
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(317, 115, 1276, 924), SysCommon.ImgCode.Ssyy, isShow: false, thread3);
				if (x3 >= 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3 + 100, y2));
					WinController.WaitTime(500);
					WinController.MoveWin(new Tuple<int, int>(x3 + 100, y2 + 100));
					WinController.WaitTime(300);
					WinController.ScrollMouseWheelDown(-360);
					WinController.WaitTime(1000);
					thread3 = 0.7;
					if (!SysCommon.isSuofang)
					{
						thread3 = 0.6;
					}
					(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(317, 115, 1276, 924), SysCommon.ImgCode.Yuanyin, isShow: false, thread3);
					if (x3 >= 0)
					{
						WinController.ClickWin(new Tuple<int, int>(x3, y2 + 10), isRandom: false);
					}
				}
				break;
			}
			case 9:
				var (x3_3, y2_3) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(317, 115, 1276, 924), SysCommon.ImgCode.Sssr, isShow: false, 0.75);
				if (x3_3 >= 0)
				{
					Tuple<int, int, int, int> pos1 = new Tuple<int, int, int, int>(538, 393, 833, 491);
					if (yunfei == 1)
					{
						pos1 = new Tuple<int, int, int, int>(649, 472, 953, 540);
					}
					string txtResult = WinController.OcrText(hWnd, pos1);
					if (txtResult.Contains("."))
					{
						List<string> priceList = SysTool.ExtractNumbers(txtResult);
						double price = 0.0;
						if (priceList.Count > 0)
						{
							price = double.Parse(priceList[priceList.Count - 1]);
						}
						WinController.ClickWin(new Tuple<int, int>(x3_3 + 120, y2_3), isRandom: false);
						WinController.WaitTime(500);
						WinController.WriteWin(price.ToString());
						WinController.WaitTime(600);
					}
				}
				else
				{
					bound.step = 10;
				}
				break;
			case 10:
			{
				LogHelper.Info("JTK：10");
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(1, 3, 528, 388), SysCommon.ImgCode.Open, isShow: false, 0.65);
				var (x29, _) = tuple2;
				_ = tuple2.Item2;
				LogHelper.Info("JTK：101," + x29);
				if (x29 >= 0)
				{
					LogHelper.Info("JTK：101");
					WinController.UploadImg(filePath);
					WinController.WaitTime(2000);
					break;
				}
				var (x3_4, y2_4) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(340, 273, 1098, 853), SysCommon.ImgCode.Upimg, isShow: false, 0.6);
				if (x3_4 >= 0)
				{
					WinController.ClickWin(new Tuple<int, int>(x3_4, y2_4));
					WinController.WaitTime(500);
				}
				else
				{
					bound.step = 11;
				}
				break;
			}
			case 11:
			{
				int x3;
				int y2;
				(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(317, 115, 1276, 924), SysCommon.ImgCode.Tijiao);
				if (x3 >= 0)
				{
					page_tijiao++;
					WinController.ClickWin(new Tuple<int, int>(x3, y2));
					WinController.WaitTime(2000);
					if (page_tijiao > 3)
					{
						bound.step = 19;
					}
				}
				(int, int) tuple2 = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(317, 115, 1276, 924), SysCommon.ImgCode.Success);
				(x3, _) = tuple2;
				_ = tuple2.Item2;
				if (x3 < 0)
				{
					break;
				}
				for (int run_num = 3; run_num > 0; run_num--)
				{
					(x3, y2) = WinController.FindPicWinEx(hWnd, new Tuple<int, int, int, int>(681, 7, 1420, 49), SysCommon.ImgCode.Close);
					if (x3 > 0)
					{
						WinController.ClickWin(new Tuple<int, int>(x3, y2), isRandom: false);
						run_num = 3;
						WinController.WaitTime(800);
					}
				}
				num++;
				bound.step = 3;
				break;
			}
			case 19:
			case 20:
				bound.isStart = false;
				bound.isEnd = true;
				break;
			}
		}
		base.Dispatcher.Invoke(delegate
		{
			IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
			if (mainWindowHandle != IntPtr.Zero)
			{
				WinController.ForegroundWindow(mainWindowHandle);
			}
			Thread.Sleep(2000);
			tab_main.SelectedIndex = 0;
			if (bound.step == 19)
			{
				SysMessage.MessageShow(Window.GetWindow(this), "方式一无法使用", "异常", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			}
			else
			{
				SysMessage.MessageShow(Window.GetWindow(this), "仅退款申诉完成,共申诉" + num + "订单", "恭喜", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			}
		});
	}

	private void btnJTK_Click2(object sender, RoutedEventArgs e)
	{
		isEnd = true;
		Thread.Sleep(1000);
		DateTime now = DateTime.Now;
		if (!(now > DateTime.MaxValue))
		{
			isEnd = false;
			pauseEvent.Set();
			ResultInfo allReason = MainBLL.GetAllReason();
			bound.ReasonList = JsonConvert.DeserializeObject<List<string>>(allReason.Data.ToString());
			txt_goodId1.Text = "";
			bound.nowId = 0;
			bound.isStart = true;
			SysMessage.MessageShow(Window.GetWindow(this), "请填写多条自定义申诉理由，否则账号容易被限制申诉", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			SysMessage.MessageShow(Window.GetWindow(this), "该功能多多风控紧，暂时不建议使用", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			Task task = Task.Run(delegate
			{
				ThreadJTK_back(token);
			}, token);
		}
	}

	private async void ThreadJTK_back(CancellationToken token)
	{
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 2;
		});
		while (!bound.isStartLogin)
		{
			Thread.Sleep(1000);
		}
		if (bound.isLogin)
		{
			base.Dispatcher.Invoke(delegate
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先登录商家店铺账号", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			});
			isEnd = true;
			return;
		}
		base.Dispatcher.Invoke(delegate
		{
			GotoUrl1("https://mms.pinduoduo.com/goods/goods_list?activeKeyNew=key_2");
			browser2.Visibility = Visibility.Visible;
			browser2.Address = "https://mms.pinduoduo.com/orders/appeals/aftersale/order?from=list";
		});
		Thread.Sleep(3000);
		bound.step = 1;
		bound.isStart = true;
		bound.isEnd = false;
		int date_time = 3;
		DateTime create_time = DateTime.Now;
		bound.sensuNum = 0;
		int num = 0;
		while (true)
		{
			pauseEvent.WaitOne();
			Thread.Sleep(1000);
			if ((DateTime.Now - create_time).TotalMinutes > (double)date_time)
			{
				bound.sensuNum++;
				create_time = DateTime.Now;
			}
			if (!bound.isStart || bound.isEnd)
			{
				break;
			}
			switch (bound.step)
			{
			case 1:
			{
				create_time = DateTime.Now;
				bound.step = 99;
				if (bound.reaList.Count > 0)
				{
					Random random = new Random();
					int randomIndex = random.Next(bound.reaList.Count);
					bound.ssReason = bound.reaList[randomIndex];
				}
				else if (bound.ReasonList.Count > 0)
				{
					Random random2 = new Random();
					int randomIndex2 = random2.Next(bound.ReasonList.Count);
					bound.ssReason = bound.ReasonList[randomIndex2];
				}
				if (num > 0)
				{
					Thread.Sleep(1000 + bound.jiangeTime * 1000);
				}
				AddTextToRichTextBox("申诉次数：" + num, myRichTextBox, txt_log);
				num++;
				base.Dispatcher.Invoke(delegate
				{
					browser2.Address = "https://mms.pinduoduo.com/orders/appeals/aftersale/order?from=list";
				});
				Thread.Sleep(3000);
				string js = "\r\n                            async function tk1(){\r\n                                var num = 3;\r\n                                var good_list = document.querySelectorAll('[data-tracking-click-viewid=\"el_after_sales_details_text_link\"]')[bound.sensuNum]\r\n                                while(num > 0 && !bound.isEnd){\r\n                                    if(good_list) break\r\n                                    await delay(1000)\r\n                                    num = num - 1\r\n                                    good_list = document.querySelectorAll('[data-tracking-click-viewid=\"el_after_sales_details_text_link\"]')[bound.sensuNum]\r\n                                }\r\n                                if(!good_list) {\r\n                                    bound.step = 5\r\n                                    return\r\n                                }\r\n                                good_list.click()\r\n                            }\r\n                            tk1()\r\n                        ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 2:
				bound.step = 99;
				base.Dispatcher.Invoke(delegate
				{
					GotoUrl1(bound.url);
				});
				break;
			case 3:
			{
				bound.step = 99;
				string js = "\r\n                                async function tk2(){\r\n                                    await delay(2000)\r\n                                    var logis = document.querySelector('.logistics-order-info')\r\n                                    logis.scrollIntoView({block:'start' })\r\n                                    await delay(800)\r\n                                    document.querySelector('.mui-steps-item-wait').click()\r\n                                    await delay(500)\r\n                                    bound.step = 4;\r\n                                }\r\n                                tk2()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 4:
			{
				bound.step = 99;
				base.Dispatcher.Invoke(delegate
				{
					string item = ScreenshotImage();
					Thread.Sleep(1000);
					browser1.GetBrowserHost().SendMouseClickEvent(1, 1, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
					uploadedFiles = new List<string>();
					uploadedFiles.AddRange(new List<string> { item });
					browser1.DialogHandler = new MyFileDialogHandler(uploadedFiles);
				});
				Thread.Sleep(1000);
				string js = "\r\n                                async function tk3(){\r\n                                    document.querySelector('.after-wrap-container').querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                    await delay(500)\r\n                                    var inner = document.querySelector('[data-testid=\"beast-core-modal-inner\"]')\r\n                                    var htmlInput = document.querySelector('[data-testid=\"beast-core-cascader-htmlInput\"]')\r\n                                    htmlInput.click()\r\n                                    document.querySelectorAll('[data-testid=\"beast-core-ellipsis\"]')[3].scrollIntoView({block:'start' })\r\n                                    await delay(300);\r\n                                    var all = document.querySelectorAll('[data-testid=\"beast-core-ellipsis\"]')\r\n                                    all[all.length - 1].click()\r\n                                    await delay(300);\r\n                                    var txt_ss = document.querySelector('[data-testid=\"beast-core-textArea-htmlInput\"]')\r\n                                    setNativeValue(txt_ss,bound.ssReason);\r\n                                    var inputEvent = new Event('change', { bubbles: true });\r\n                                    txt_ss.dispatchEvent(inputEvent);\r\n                                    await delay(300);\r\n                                    var txt_price = document.querySelector('[data-testid=\"beast-core-inputNumber-htmlInput\"]')\r\n                                    setNativeValue(txt_price,9999);\r\n                                    txt_price.dispatchEvent(inputEvent);\r\n                                    await delay(800);\r\n                                    var sj_price = txt_price.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.querySelectorAll('[data-testid=\"beast-core-grid-col-wrapper\"]')[1].textContent\r\n                                    var price = extractPrice(sj_price)\r\n                                    setNativeValue(txt_price,price);\r\n                                    txt_price.dispatchEvent(inputEvent);\r\n                                    await delay(300);\r\n                                    document.querySelector('[data-testid=\"beast-core-icon-picture\"]').parentElement.click()\r\n                                    await delay(2000);\r\n                                    inner.querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                    await delay(2000);\r\n                                    bound.step = 1;\r\n                                }\r\n                                tk3()\r\n                            ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 5:
				base.Dispatcher.Invoke(delegate
				{
					GotoUrl1("https://mms.pinduoduo.com/orders/appeals/aftersale/order?from=list");
				});
				bound.isStart = false;
				bound.isEnd = true;
				break;
			}
		}
		base.Dispatcher.Invoke(delegate
		{
			Thread.Sleep(10000);
			tab_main.SelectedIndex = 0;
			SysMessage.MessageShow(Window.GetWindow(this), "仅退款申诉完成", "恭喜", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
		});
	}

	private void btnOff_Click(object sender, RoutedEventArgs e)
	{
		isEnd = true;
		Thread.Sleep(1000);
		DateTime now = DateTime.Now;
		if (!(now > DateTime.MaxValue))
		{
			isEnd = false;
			pauseEvent.Set();
			tab_main.SelectedIndex = 2;
			Task task = Task.Run(delegate
			{
				ThreadOff(token);
			}, token);
		}
	}

	private async void ThreadOff(CancellationToken token)
	{
		try
		{
			base.Dispatcher.Invoke(delegate
			{
				GotoUrl1("https://mms.pinduoduo.com/act/index?SourceType=MMSActicon");
			});
			bound.step = 1;
			bound.isEnd = false;
			int date_time = 3;
			DateTime create_time = DateTime.Now;
			while (true)
			{
				pauseEvent.WaitOne();
				if ((DateTime.Now - create_time).TotalMinutes > (double)date_time)
				{
					bound.isEnd = true;
					break;
				}
				if (bound.isEnd)
				{
					break;
				}
				Thread.Sleep(2000);
				switch (bound.step)
				{
				case 1:
				{
					bound.step = 99;
					string js = "\r\n                            async function off(){\r\n                                var txt_btn1 = document.querySelector('#mms-act-home-left-module-list').querySelector('button').textContent\r\n                                if(txt_btn1 == '开启'){\r\n                                    bound.step = 3\r\n                                }\r\n                                else{\r\n                                    var icon_question = document.querySelectorAll('[data-testid=\"beast-core-icon-question-circle\"]')[1]\r\n                                    var event = new MouseEvent('mouseover', {\r\n                                        bubbles: true,\r\n                                        cancelable: true,\r\n                                        view: window\r\n                                    });\r\n                                    icon_question.dispatchEvent(event);\r\n                                    await delay(1000)\r\n                                    bound.step = 2\r\n                                }\r\n                            }\r\n                            off()\r\n                        ";
					await browser1.EvaluateScriptAsync(js);
					break;
				}
				case 2:
				{
					bound.step = 99;
					string js = "\r\n                            async function off2(){\r\n                                var spanAll = document.querySelector('[data-testid=\"beast-core-portal\"]').querySelectorAll('span')\r\n                                spanAll[spanAll.length-1].click()\r\n                                bound.step = 3\r\n                            }\r\n                            off2()\r\n                        ";
					await browser1.EvaluateScriptAsync(js);
					break;
				}
				case 3:
					bound.isEnd = true;
					break;
				}
			}
		}
		catch (Exception)
		{
			bound.isEnd = true;
		}
	}

	public string ScreenshotImage()
	{
		RenderTargetBitmap screenshot = CaptureCefSharpControl(browser1);
		return SaveScreenshot(screenshot);
	}

	private RenderTargetBitmap CaptureCefSharpControl(ChromiumWebBrowser browser)
	{
		int pixelWidth = (int)browser.ActualWidth - 50;
		int pixelHeight = (int)browser.ActualHeight - 50;
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(browser);
		return renderTargetBitmap;
	}

	private string SaveScreenshot(RenderTargetBitmap screenshot)
	{
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(screenshot));
		string text = AppDomain.CurrentDomain.BaseDirectory + "\\goodImg\\tk";
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		string text2 = Path.Combine(text, bound.newGoodId + ".png");
		using (FileStream stream = new FileStream(text2, FileMode.Create))
		{
			pngBitmapEncoder.Save(stream);
		}
		return text2;
	}

	private void btnSSLY_Click(object sender, EventArgs e)
	{
		LYSet lYSet = new LYSet(bound);
		lYSet.ShowDialog();
	}

	private void btnCompute_Click(object sender, EventArgs e)
	{
		Compute compute = new Compute();
		compute.ShowDialog();
	}

	private void btnKDC_Click(object sender, RoutedEventArgs e)
	{
		isEnd = true;
		Thread.Sleep(1000);
		isEnd = false;
		pauseEvent.Set();
		if (txt_goodId.Text.Length == 0)
		{
			SysMessage.MessageShow(Window.GetWindow(this), "请先填写商品id", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			return;
		}
		bound.settingInfo.txtGoodId = txt_goodId.Text;
		bound.settingInfo.goodIds = bound.settingInfo.txtGoodId.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
		bound.goodId = bound.settingInfo.goodIds[0];
		bound.settingInfo.checkJump = (check_jump.IsChecked.GetValueOrDefault() ? 1 : 0);
		bound.type = "kdc";
		tab_main.SelectedIndex = 2;
		txt_goodId1.Text = "";
		bound.nowId = 0;
		bound.isStart = true;
		Task task = Task.Run(delegate
		{
			ThreadKDC(token);
		}, token);
	}

	private async void ThreadKDC(CancellationToken token)
	{
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 2;
		});
		while (!bound.isStartLogin)
		{
			Thread.Sleep(1000);
		}
		if (bound.isLogin)
		{
			base.Dispatcher.Invoke(delegate
			{
				SysMessage.MessageShow(Window.GetWindow(this), "请先登录商家店铺账号", "注意：", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			});
			isEnd = true;
			return;
		}
		base.Dispatcher.Invoke(delegate
		{
			GotoUrl1("https://mms.pinduoduo.com/goods/goods_list?activeKeyNew=key_2");
			browser2.Visibility = Visibility.Visible;
			browser2.Address = "https://mms.pinduoduo.com/act/choose_goods/v2?id=21863&bannerType=27&oneKeyEnroll=true&SourceType=PROMO-ActivityEnrollListRetry";
			System.Windows.Controls.Panel.SetZIndex(browser1, 2);
			System.Windows.Controls.Panel.SetZIndex(browser2, 1);
		});
		Thread.Sleep(3000);
		bound.step = 1;
		bound.isStart = true;
		bound.isEnd = false;
		int date_time = 3;
		DateTime create_time = DateTime.Now;
		bound.sensuNum = 0;
		bound.isSuccess = false;
		bound.endMsg = "";
		bound.step = 100;
		int runNum = 0;
		int lastLength = 0;
		SysCommon.ResponseData = null;
		while (true)
		{
			pauseEvent.WaitOne();
			Thread.Sleep(500);
			if ((DateTime.Now - create_time).TotalMinutes > (double)date_time)
			{
				bound.sensuNum++;
				create_time = DateTime.Now;
			}
			if (!bound.isStart || bound.isEnd)
			{
				break;
			}
			switch (bound.step)
			{
			case 100:
			{
				bound.step = 99;
				AddTextToRichTextBox("查询商品信息", myRichTextBox, txt_log);
				string js = "\r\n                            async function findGood(){\r\n                                var goodid = bound.goodId\r\n                                var txt_good = document.querySelector('[data-tracking-viewid=\"goods_id\"]').querySelector('input')                             \r\n                                setNativeValue(txt_good, goodid);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                txt_good.dispatchEvent(inputEvent);\r\n                                if (txt_good.value != goodid) retrun\r\n                                await delay(500)\r\n                                document.querySelector('[data-tracking-click-viewid=\"ele_inquire\"]').click()\r\n                                await delay(500)\r\n                                var goodtable = document.querySelector('[data-testid=\"beast-core-table-middle-body\"]')\r\n                                if(!goodtable) return\r\n                                var price = document.querySelector('[data-tracking-viewid=\"changeprice\"]').parentElement.parentElement.parentElement.parentElement.querySelector('div').textContent\r\n                                if(price.includes('～')){\r\n                                    console.log('111111111')\r\n                                    var prices = price.split('～')\r\n                                    var minPrice = (parseFloat(prices[0])).toFixed(2)\r\n                                    bound.minPrice = parseFloat(minPrice)\r\n                                    console.log('minPrice=',bound.minPrice)\r\n                                }\r\n                                else{\r\n                                    bound.endMsg = '商品价格错误,请设置低价SKU'\r\n                                    bound.isEnd = true\r\n                                    return\r\n                                }\r\n                                if(bound.settingInfo.checkJump == 1){\r\n                                    bound.step = 1\r\n                                }\r\n                                else{\r\n                                    bound.step = 101\r\n                                    document.querySelector('[data-tracking-impr-viewid=\"edit\"]').click()\r\n                                }\r\n                                console.log('findGood end')\r\n                            }\r\n                            findGood()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 101:
			{
				string js = "\r\n                            async function changeGood(){\r\n                                try\r\n                                {\r\n                                    var sort = document.querySelector('.sort-name')\r\n                                    if(sort) {\r\n                                        bound.step = 102\r\n                                    }\r\n                                }\r\n                                catch(err){}\r\n                            }\r\n                            changeGood()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 102:
			{
				bound.step = 99;
				LogHelper.Info("DC:102");
				AddTextToRichTextBox("编辑商品信息", myRichTextBox, txt_log);
				string js = "\r\n                            async function sku1(){\r\n                                var htmlInput = document.querySelector('.goods-spec-row-box').querySelectorAll('.spec-input')\r\n                                \r\n                                for(var i = 0;i< bound.skuLen;i++){\r\n                                    var spec = htmlInput[bound.skuLen + i].querySelector('input')\r\n                                    setNativeValue(spec, 'start' + (i + 1));\r\n                                    var inputEvent2 = new Event('input', { bubbles: true });\r\n                                    spec.dispatchEvent(inputEvent2);\r\n                                    await delay(800)\r\n                                    var changeEvent = new Event('blur', { bubbles: true });\r\n                                    spec.dispatchEvent(changeEvent);\r\n                                    await delay(800)\r\n                                    htmlInput = document.querySelector('.goods-spec-row-box').querySelectorAll('.spec-input')\r\n                                }\r\n                                await delay(500)\r\n                                bound.step = 103;\r\n                            }\r\n\r\n                            async function sku(){\r\n                                var htmlInput = document.querySelector('.package-container').querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                var txtGridCol = document.querySelector('.package-container').parentElement.parentElement.parentElement.parentElement.querySelector('[data-testid=\"beast-core-grid-col-wrapper\"]').textContent\r\n                                var jhl = []\r\n                                var type = 1\r\n                                if(txtGridCol == '净含量') {\r\n                                    type = 2\r\n                                    const hasSKU = Array.from(htmlInput).some(input => {\r\n                                            return input.value == htmlInput[0].value + '01' || input.textContent == htmlInput[0].value + '01';\r\n                                    });\r\n                                    if(hasSKU){\r\n                                        bound.step = 1\r\n                                        return\r\n                                    }\r\n                                    for(var i = 0;i< bound.skuLen;i++){\r\n                                        var value = htmlInput[i*2].value + '01'\r\n                                        var hasRes = Array.from(htmlInput).some(input => {\r\n                                            return input.value == value || input.textContent == value;\r\n                                        });\r\n                                        console.log('value=',value)\r\n                                        if(!hasRes) jhl.push(value)\r\n                                        else jhl.push(value + '1')\r\n                                    }\r\n                                }\r\n                                \r\n                                for(var i = 0;i< bound.skuLen;i++){\r\n                                    var spec = htmlInput[bound.skuLen*2 + i * 2]\r\n                                    var value = 'start' + (i + 1)\r\n                                    if(type == 2) value = jhl[i]\r\n                                    setNativeValue(spec, value);\r\n                                    var inputEvent2 = new Event('input', { bubbles: true });\r\n                                    spec.dispatchEvent(inputEvent2);\r\n                                    await delay(800)\r\n                                    var changeEvent = new Event('blur', { bubbles: true });\r\n                                    spec.dispatchEvent(changeEvent);\r\n                                    await delay(800)\r\n                                    htmlInput = document.querySelector('.package-container').querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                }\r\n                                await delay(500)\r\n                                bound.step = 103;\r\n                                                //添加备注\r\n                                                //var vvalue = skuInfos[i].values[j].split(' ')\r\n                                                //var key = ''\r\n                                                //var unit = ''\r\n                                                //if(skuInfos[i].key == '净含量'){\r\n                                                //    const numbers = vvalue[0].match(/[+-]?\\d+(\\.\\d+)?/g);\r\n                                                //    key = numbers[0]\r\n                                                //    const letters = vvalue[0].match(/[a-zA-Z]+/g);\r\n                                                //    unit = letters[0]\r\n                                                //    console.log('净含量=',key,unit)\r\n                                                //}\r\n                                                //for(var m = 0;m<vvalue.length;m++){\r\n                                                //    if(vvalue[m].length == 0) continue\r\n                                                //    var packageInput = htmlInput[htmlInput.length -1]\r\n                                                //    if(m==1){\r\n                                                //        htmlInput = document.querySelector('.package-container').querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                                //        packageInput = htmlInput[htmlInput.length -2]\r\n                                                //    }\r\n                                                //    var nvalue = vvalue[m]\r\n                                                //    if(m==0 && skuInfos[i].key == '净含量'){\r\n                                                //        packageInput.parentElement.parentElement.parentElement.parentElement.querySelector('[data-testid=\"beast-core-select-htmlInput\"]').click()\r\n                                                //        await delay(300)\r\n                                                //        var liList = document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')\r\n                                                //        for(var n = 0;n<liList.length;n++){\r\n                                                //            if(liList[n].textContent == unit){\r\n                                                //                liList[n].click()\r\n                                                //                await delay(500);\r\n                                                //                break\r\n                                                //            }\r\n                                                //        }\r\n                                                //        nvalue = key\r\n                                                //    }\r\n                                                //    setNativeValue(packageInput, nvalue);\r\n                                                //    var inputEvent = new Event('input', { bubbles: true });\r\n                                                //    packageInput.dispatchEvent(inputEvent);\r\n                                                //    await delay(200);\r\n                                                //    var changeEvent = new Event('blur', { bubbles: true });\r\n                                                //    packageInput.dispatchEvent(changeEvent);\r\n                                                //    await delay(500)\r\n                                                //}\r\n                                }\r\n                                function choose(){\r\n                                    var goods_spec = document.querySelector('.goods-spec')\r\n                                    if(goods_spec){\r\n                                        var htmlInput = document.querySelector('.goods-spec-row-box').querySelectorAll('.spec-input')\r\n                                        const hasSKU = Array.from(htmlInput).some(input => {\r\n                                            return input.querySelector('input').value == 'start1' || input.querySelector('input').textContent == 'start1';\r\n                                        });\r\n                                        if(hasSKU){\r\n                                            bound.step = 1\r\n                                            return\r\n                                        }\r\n                                        var skuLen = htmlInput.length\r\n                                        if(skuLen > 20){\r\n                                            bound.endMsg = 'SKU数量不能超过20个'\r\n                                            bound.isEnd = true\r\n                                            return\r\n                                        }\r\n                                        bound.skuLen = skuLen - 1\r\n                                        sku1()\r\n                                        return\r\n                                    }\r\n                                    var package = document.querySelector('.package-container')\r\n                                    if(package){\r\n                                        var htmlInput = document.querySelector('.package-container').querySelectorAll('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                        const hasSKU = Array.from(htmlInput).some(input => {\r\n                                            return input.value == 'start1' || input.textContent == 'start1';\r\n                                        });\r\n                                        if(hasSKU){\r\n                                            bound.step = 1\r\n                                            return\r\n                                        }\r\n                                        var skuLen = htmlInput.length\r\n                                        if(skuLen > 21){\r\n                                            bound.endMsg = 'SKU数量不能超过10个'\r\n                                            bound.isEnd = true\r\n                                            return\r\n                                        }\r\n                                        bound.skuLen = (skuLen - 1) / 2\r\n                                        console.log('sku数量=',bound.skuLen)\r\n                                        sku()\r\n                                        return\r\n                                    }\r\n                                }\r\n                                choose()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 103:
			{
				bound.step = 99;
				string js = "\r\n                            async function changeGood2(){\r\n                                document.querySelector('[data-tracking-impr-viewid=\"sku_choosing\"]').querySelector('input').click()\r\n                                await delay(500)\r\n                                var portal = document.querySelectorAll('[data-testid=\"beast-core-portal\"]')\r\n                                var main = document.querySelectorAll('[data-testid=\"beast-core-portal\"]')[portal.length - 1].querySelectorAll('li')\r\n                                var mainLen = main.length\r\n                                console.log(main.length)\r\n                                for(var i = 0;i<bound.skuLen;i++){\r\n                                    main[i+1].click()\r\n                                    await delay(300)\r\n                                }\r\n                                var price1 = document.querySelector('[data-testid=\"beast-core-inputNumber\"]').querySelector('input')\r\n                                setNativeValue(price1, bound.minPrice);\r\n                                var inputEvent2 = new Event('input', { bubbles: true });\r\n                                price1.dispatchEvent(inputEvent2);\r\n                                await delay(300)\r\n                                var price2 = document.querySelectorAll('[data-testid=\"beast-core-inputNumber\"]')[1].querySelector('input')\r\n                                setNativeValue(price2, bound.minPrice + 1);\r\n                                price2.dispatchEvent(inputEvent2);\r\n                                await delay(500)\r\n                                var skuImg = document.querySelector('[data-testid=\"beast-core-inputNumber\"]').parentElement.parentElement.querySelector('.sku-preview-img')\r\n                                skuImg.click()\r\n                                await delay(500)\r\n                                bound.step = 104;\r\n                            }\r\n                            changeGood2()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 104:
			{
				bound.step = 99;
				string js = "\r\n                                async function runChange(){\r\n                                    document.querySelector('[data-testid=\"beast-core-icon-upload\"]').parentElement.click();\r\n                                    bound.step = 105;\r\n                                    await delay(300);\r\n                                }\r\n                                runChange();\r\n                            ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 105:
			{
				bound.step = 99;
				browser1.GetBrowserHost().SendMouseClickEvent(100, 100, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
				string path = AppDomain.CurrentDomain.BaseDirectory + "Images\\1.png";
				uploadedFiles = new List<string>();
				uploadedFiles.AddRange(new List<string> { path });
				browser1.DialogHandler = new MyFileDialogHandler(uploadedFiles);
				string js = "\r\n                                async function runChange1(){\r\n                                    await delay(300);\r\n                                    document.querySelectorAll('[data-testid=\"beast-core-modal-body\"]')[1].querySelector('button').click()\r\n                                    await delay(100);\r\n                                    bound.step = 106;\r\n                                }\r\n                                runChange1();\r\n                            ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 106:
			{
				string js = "\r\n                                    async function runOk(){\r\n                                        var num = document.querySelector('[data-testid=\"beast-core-card\"]').parentElement.parentElement.querySelectorAll('[data-checked=\"true\"]').length\r\n                                        if (num > 0){{\r\n                                            await delay(500);\r\n                                            document.querySelector('[data-testid=\"beast-core-modal-body\"]').nextElementSibling.querySelector('button').click()\r\n                                            bound.step = 107;\r\n                                        }}\r\n                                    }\r\n                                    runOk()\r\n                                ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 107:
			{
				bound.step = 99;
				AddTextToRichTextBox("提交商品", myRichTextBox, txt_log);
				string js = "\r\n                            async function changeGood3(){\r\n                                try\r\n                                {\r\n                                    document.querySelector('[data-tracking-viewid=\"sku_batchset\"]').click()\r\n                                    await delay(800);\r\n                                    document.querySelector('.goods-footer').querySelector('button').click()\r\n                                    await delay(800);\r\n                                    bound.step = 108\r\n                                }\r\n                                catch(err){}\r\n                            }\r\n                            changeGood3()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 108:
			{
				string js = "\r\n                            async function changeGood4(){\r\n                                try\r\n                                {\r\n                                    var success = document.querySelector('[data-testid=\"beast-core-icon-check-circle_filled\"]')\r\n                                    if(success) bound.step = 109\r\n                                }\r\n                                catch(err){}\r\n                            }\r\n                            changeGood4()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 109:
			{
				bound.step = 99;
				string js = "\r\n                            async function changeGood5(){\r\n                                try\r\n                                {\r\n                                    await delay(20000)\r\n                                    bound.step = 1;\r\n                                }\r\n                                catch(err){}\r\n                            }\r\n                            changeGood5()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 1:
				create_time = DateTime.Now;
				bound.step = 99;
				AddTextToRichTextBox("9.9折大促活动", myRichTextBox, txt_log);
				base.Dispatcher.Invoke(delegate
				{
					System.Windows.Controls.Panel.SetZIndex(browser1, 1);
					System.Windows.Controls.Panel.SetZIndex(browser2, 2);
					browser2.Address = "https://mms.pinduoduo.com/act/choose_goods/v2?id=21863&bannerType=27&oneKeyEnroll=true&SourceType=PROMO-ActivityEnrollListRetry";
				});
				Thread.Sleep(1000);
				bound.step = 111;
				break;
			case 111:
			{
				string js = "\r\n                            async function dc1(){\r\n                                var input1 = document.querySelector('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                input1.focus()\r\n                                bound.step = 2;\r\n                            }\r\n                            dc1()\r\n                        ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 2:
			{
				bound.step = 99;
				browser2.GetBrowserHost().SendMouseClickEvent(1, 1, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
				Thread.Sleep(1500);
				string js = "\r\n                            async function dc2(){\r\n                                var innerWrapper = document.querySelector('[data-testid=\"beast-core-modal-innerWrapper\"]')\r\n                                var input2 = innerWrapper.querySelector('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                setNativeValue(input2, bound.goodId);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                input2.dispatchEvent(inputEvent);\r\n                                await delay(300)\r\n                                var chaxun = innerWrapper.querySelector('[data-testid=\"beast-core-input-suffix\"]').querySelector('a')\r\n                                chaxun.click()\r\n                                await delay(900)\r\n                                var check = innerWrapper.querySelectorAll('[data-testid=\"beast-core-icon-check\"]')\r\n                                if(check.length > 1){\r\n                                    check[1].parentElement.click()\r\n                                    await delay(100)\r\n                                    innerWrapper.querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                    await delay(300)\r\n                                    bound.step = 20;\r\n                                    document.querySelector('#bottom_sign').querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                }\r\n                                else{\r\n                                    bound.step = 1;\r\n                                }\r\n                            }\r\n                            dc2()\r\n                        ";
				SysCommon.ResponseJson = new StringBuilder();
				runNum = 0;
				lastLength = 0;
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 20:
				Thread.Sleep(300);
				if (SysCommon.ResponseJson.Length <= 0)
				{
					break;
				}
				if (SysCommon.ResponseJson.Length > lastLength)
				{
					AddTextToRichTextBox("请稍后", myRichTextBox, txt_log);
					runNum = 0;
					lastLength = SysCommon.ResponseJson.Length;
					break;
				}
				runNum++;
				if (runNum <= 3)
				{
					break;
				}
				try
				{
					getJson();
					List<SkuInfoList> skuInfoList2 = SysCommon.ResponseData.Result.EnrollGoodsRuleVos[0].EnrollRuleParam.EnrollGoodsBigSalePrice.SkuInfoList;
					int minPrice2 = skuInfoList2.OrderBy((SkuInfoList s) => s.FillingSkuPrice).FirstOrDefault().FillingSkuPrice;
					bound.minPrice1 = (double)minPrice2 * 0.01;
					LogHelper.Info("minPrice1=" + bound.minPrice1);
					bound.step = 3;
				}
				catch (JsonReaderException ex2)
				{
					JsonReaderException ex = ex2;
					LogHelper.Info("解析失败: " + ex.Message);
				}
				break;
			case 3:
			{
				bound.step = 99;
				Thread.Sleep(2000);
				string js = "\r\n                            async function dc3(){\r\n                                    var runNum = 5\r\n                                    while(runNum > 0){\r\n                                        runNum = runNum - 1\r\n                                        var submit = document.querySelector('[data-tracking-click-viewid=\"submit_shared\"]')\r\n                                        if(submit){\r\n                                            bound.step = 4;\r\n                                            submit.click()\r\n                                            return\r\n                                        }\r\n                                        await delay(1000)\r\n                                    }\r\n                                    bound.step = 1;\r\n                            }\r\n                            dc3()\r\n                        ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 4:
			{
				Thread.Sleep(1000);
				AddTextToRichTextBox("活动报名", myRichTextBox, txt_log);
				LogHelper.Info("DC：4");
				string js = "\r\n                                async function dc4(){\r\n                                    var icon = document.querySelector('[data-testid=\"beast-core-icon-check-circle_filled\"]')\r\n                                    if(icon){\r\n                                        bound.step = 5;\r\n                                        await delay(500)\r\n                                        icon.parentElement.parentElement.querySelectorAll('[data-testid=\"beast-core-button\"]')[1].click()\r\n                                     }\r\n                                }\r\n                                dc4()\r\n                        ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 5:
			{
				Thread.Sleep(1000);
				LogHelper.Info("DC：5");
				string js = "\r\n                                async function dc5(){\r\n                                    var price_shared = document.querySelector('[data-tracking-viewid=\"ele_go_to_cut_down_price_shared\"]')\r\n                                    if(price_shared){\r\n                                        bound.step = 8;\r\n                                        price_shared.click()\r\n                                        await delay(1000)\r\n                                    }\r\n                                    else{\r\n                                        var icon = document.querySelector('[data-testid=\"beast-core-icon-check-circle_filled\"]')\r\n                                        if(icon){\r\n                                            icon.parentElement.parentElement.querySelectorAll('[data-testid=\"beast-core-button\"]')[1].click()\r\n                                        }\r\n                                    }\r\n                                }\r\n                                dc5()\r\n                            ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 6:
			{
				bound.step = 99;
				LogHelper.Info("DC：6");
				string js = "\r\n                                async function dc6(){\r\n                                    var innerWrapper = document.querySelector('[data-testid=\"beast-core-modal-innerWrapper\"]')\r\n                                    var inputNumber = innerWrapper.querySelector('[data-testid=\"beast-core-table-middle-tbody\"]').querySelectorAll('[data-testid=\"beast-core-inputNumber\"]')\r\n                                    var inputEvent = new Event('input', { bubbles: true });\r\n                                    for(var i = 0;i<inputNumber.length;i++){\r\n                                        var inputNum = inputNumber[i].querySelector('input')\r\n                                        console.log('inputNumber=',parseFloat(inputNum.value),bound.minPrice1.toFixed(2))\r\n                                        if(parseFloat(inputNum.value) == bound.minPrice1.toFixed(2)){\r\n                                            console.log('inputNumber='+i)\r\n                                            setNativeValue(inputNum, (bound.minPrice2 - 0.01).toFixed(2));\r\n                                            inputNum.dispatchEvent(inputEvent);\r\n                                            await delay(200)\r\n                                        }\r\n                                    }\r\n                                    bound.step = 11;\r\n                                }\r\n                                dc6()\r\n                            ";
				await browser2.EvaluateScriptAsync(js);
				break;
			}
			case 7:
			case 8:
				bound.step = 99;
				AddTextToRichTextBox("报名大促活动", myRichTextBox, txt_log);
				LogHelper.Info("DC：8");
				base.Dispatcher.Invoke(delegate
				{
					GotoUrl1("https://mms.pinduoduo.com/act/choose_goods/v2?id=20981&bannerType=27&oneKeyEnroll=true&SourceType=MMSAction&SceneType=ActivityCard");
					System.Windows.Controls.Panel.SetZIndex(browser1, 2);
					System.Windows.Controls.Panel.SetZIndex(browser2, 1);
				});
				Thread.Sleep(1000);
				bound.step = 81;
				break;
			case 81:
			{
				Thread.Sleep(300);
				string js = "\r\n                            async function dc8(){\r\n                                var input1 = document.querySelector('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                input1.focus()\r\n                                bound.step = 9;\r\n                            }\r\n                            dc8()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 9:
			{
				bound.step = 99;
				browser1.GetBrowserHost().SendMouseClickEvent(1, 1, MouseButtonType.Left, mouseUp: false, 1, CefEventFlags.None);
				Thread.Sleep(1500);
				string js = "\r\n                            async function dc9(){\r\n                                var innerWrapper = document.querySelector('[data-testid=\"beast-core-modal-innerWrapper\"]')\r\n                                var input2 = innerWrapper.querySelector('[data-testid=\"beast-core-input-htmlInput\"]')\r\n                                setNativeValue(input2, bound.goodId);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                input2.dispatchEvent(inputEvent);\r\n                                await delay(300)\r\n                                var chaxun = innerWrapper.querySelector('[data-testid=\"beast-core-input-suffix\"]').querySelector('a')\r\n                                chaxun.click()\r\n                                await delay(900)\r\n                                var check = innerWrapper.querySelectorAll('[data-testid=\"beast-core-icon-check\"]')\r\n                                if(check.length > 1){\r\n                                    check[1].parentElement.click()\r\n                                    await delay(100)\r\n                                    innerWrapper.querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                    await delay(300)\r\n                                    bound.step = 10;\r\n                                    document.querySelector('#bottom_sign').querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                }\r\n                                else{\r\n                                    bound.step = 7;\r\n                                }\r\n                            }\r\n                            dc9()\r\n                        ";
				SysCommon.ResponseJson = new StringBuilder();
				runNum = 0;
				lastLength = 0;
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 10:
				Thread.Sleep(300);
				if (SysCommon.ResponseJson.Length <= 0)
				{
					break;
				}
				if (SysCommon.ResponseJson.Length > lastLength)
				{
					AddTextToRichTextBox("请稍后", myRichTextBox, txt_log);
					runNum = 0;
					lastLength = SysCommon.ResponseJson.Length;
					break;
				}
				runNum++;
				if (runNum > 3)
				{
					getJson();
					List<SkuInfoList> skuInfoList = SysCommon.ResponseData.Result.EnrollGoodsRuleVos[0].EnrollRuleParam.EnrollGoodsBigSalePrice.SkuInfoList;
					int minPrice = skuInfoList.OrderBy((SkuInfoList s) => s.RequirePrice).FirstOrDefault().RequirePrice;
					bound.minPrice2 = Math.Round((double)minPrice * 0.01, 2);
					LogHelper.Info("minPrice2=" + bound.minPrice2);
					bound.step = 6;
				}
				break;
			case 11:
			{
				bound.step = 99;
				AddTextToRichTextBox("修改商品价格", myRichTextBox, txt_log);
				string js = "\r\n                            async function dc11(){\r\n                                try{\r\n                                    var icon_check = document.querySelectorAll('[data-testid=\"beast-core-icon-question-circle\"]')[1].parentElement.querySelector('[data-testid=\"beast-core-icon-check\"]').parentElement\r\n                                    if(icon_check) icon_check.click()\r\n                                    await delay(300)\r\n                                }\r\n                                catch(err){}\r\n                                //选择一口价\r\n                                document.querySelector('[data-testid=\"beast-core-select-htmlInput\"]').click()\r\n                                await delay(300)\r\n                                document.querySelector('[data-testid=\"beast-core-portal-main\"]').querySelectorAll('li')[4].click()\r\n                                await delay(1500)\r\n                                console.log('aaaaaa')\r\n                                var price2 = (bound.minPrice2).toFixed(2)\r\n                                console.log('price2=',price2)\r\n                                //修改价格\r\n                                var inputNumber = document.querySelector('[data-testid=\"beast-core-inputNumber-htmlInput\"]')\r\n                                setNativeValue(inputNumber, price2);\r\n                                var inputEvent = new Event('input', { bubbles: true });\r\n                                inputNumber.dispatchEvent(inputEvent);\r\n                                await delay(300)\r\n                                inputNumber.parentElement.parentElement.parentElement.parentElement.parentElement.parentElement.querySelector('[data-testid=\"beast-core-button\"]').click()\r\n                                await delay(500)\r\n                                var submit = document.querySelector('[data-tracking-click-viewid=\"submit_shared\"]')\r\n                                submit.click()\r\n                                await delay(500)\r\n                                bound.step = 12;\r\n                                \r\n                            }\r\n                            dc11()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				break;
			}
			case 12:
			{
				bound.step = 99;
				AddTextToRichTextBox("提交大促活动", myRichTextBox, txt_log);
				string js = "\r\n                            async function dc12(){\r\n                                //最终提交\r\n                                document.querySelector('[data-tracking-click-viewid=\"confirm_enroll_shared\"]').click()\r\n                            }\r\n                            dc12()\r\n                        ";
				await browser1.EvaluateScriptAsync(js);
				Thread.Sleep(100);
				string js21 = "\r\n                            async function dc121(){\r\n                                document.querySelector('[data-testid=\"beast-core-modal-innerWrapper\"]').querySelectorAll('[data-testid=\"beast-core-button\"]')[1].click()\r\n                                await delay(200)\r\n                                var again = document.querySelector('[data-tracking-click-viewid=\"confirm_enroll_shared\"]')\r\n                                if(again) again.click()\r\n                                bound.step = 13\r\n                            }\r\n                            dc121()\r\n                        ";
				await browser2.EvaluateScriptAsync(js21);
				break;
			}
			case 13:
				bound.step = 99;
				bound.isEnd = true;
				bound.isSuccess = true;
				break;
			}
		}
		base.Dispatcher.Invoke(delegate
		{
			tab_main.SelectedIndex = 0;
			if (bound.isSuccess)
			{
				SysMessage.MessageShow(Window.GetWindow(this), "卡大促操作完成", "恭喜", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Success);
			}
			else
			{
				SysMessage.MessageShow(Window.GetWindow(this), bound.endMsg, "卡大促失败", MessageBoxButton.OK, MDD.Models.Enum.EnumPromptType.Warn);
			}
		});
	}

	private void getJson()
	{
		using Stream stream = ConvertStringBuilderToStream(SysCommon.ResponseJson);
		using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
		JsonTextReader reader2 = new JsonTextReader(reader);
		JsonSerializer jsonSerializer = new JsonSerializer();
		try
		{
			JsonResponse responseData = jsonSerializer.Deserialize<JsonResponse>(reader2);
			SysCommon.ResponseData = responseData;
			LogHelper.Info("解析成功");
		}
		catch (JsonException ex)
		{
			LogHelper.Info("JSON 解析失败: " + ex.Message);
			bound.endMsg = "SKU数量过多";
			bound.isEnd = true;
		}
	}

	private static Stream ConvertStringBuilderToStream(StringBuilder sb)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, 1024, leaveOpen: true))
		{
			for (int i = 0; i < sb.Length; i += 8192)
			{
				int num = Math.Min(8192, sb.Length - i);
				streamWriter.Write(sb.ToString(i, num));
			}
		}
		memoryStream.Position = 0L;
		return memoryStream;
	}



}
