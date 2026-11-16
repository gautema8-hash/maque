# 麻雀一键铺货 (maque)

拼多多商品**一键采集 / 一键铺货**桌面工具（源码丢失后的逆向恢复重建版）。

> 本仓库为自有软件源码恢复项目：原程序为 .NET Framework 4.7.2 WPF 应用，因源码丢失，
> 通过静态逆向（Inno Setup 解包 + ILSpy 反编译 + BAML 文案还原）重建出完整可编译源码。

## 功能特性

- **商品采集**：内嵌 CefSharp 浏览器登录拼多多买手号，按商品 ID 自动抓取标题、主图、详情图、SKU、规格、属性、类目
- **一键铺货**：驱动商家后台发布页状态机（分类→标题→主图→属性→详情图→SKU→价格→服务→草稿），全程可视化日志
- **裂变模式**：同品裂变 N 个标题 + 价格公式引擎（如 `A +2+ B * 0.02`）+ 图片裂变（图中图）
- **批量能力**：批量下架、一键卡大促、卡反拼单、商品监控、爆款推荐、评价/评论下载
- **辅助工具**：视频采集（抖音/快手）、盈亏计算器、目标投产自动计算、销量图表（LiveCharts）
- **店铺管理**：扫码登录、多店铺 Cookie 管理、SQLite 上传记录

## 技术栈

| 层 | 技术 |
|---|---|
| UI | WPF (.NET Framework 4.7.2) + ZdfFlatUI(WPF.UI.dll) 控件库 |
| 浏览器内核 | CefSharp 111（x86/x64 双运行时，JS 双向桥 BoundObject） |
| 业务架构 | MaoDuoDuo(UI) + MDD.BLL / MDD.DAL / MDD.Models / MDD.Common / MDD.Utility |
| 存储 | SQLite（mddph.sqlite，9 张业务表） |
| 图像/OCR | OpenCvSharp（水印/裂变图中图）、Tesseract（chi_sim） |
| 图表 | LiveCharts.Wpf |

## 目录结构

```
src/
├── MaoDuoDuo/            # 主窗口、登录窗、CefSharp 封装、JS 桥
├── MaoDuoDuo.Views/      # 首页/爆款/店铺管理/视频采集/商品监控等视图
├── DuoDuoPuHuo.Views/    # 盈亏计算、申诉理由设置
├── MaoDuoDuo.Web/        # CefSharp 生命周期/请求/弹窗处理器
├── MDD.BLL / MDD.DAL / MDD.Models / MDD.Common / MDD.Utility
├── images/               # 原版 UI 图片资源
├── AppData/              # cate.json、mddph.sqlite、tessdata 等
└── libs/                 # 第三方依赖 DLL（取自原版安装包）
```

## 构建方法

```bash
# 需要 .NET SDK 8+（用于编译 net48 目标）与 Windows 系统
cd src
dotnet build DuoDuoPuHuo.csproj -c Release
# 产物：src/bin/Release/net48/DuoDuoPuHuo.exe
```

运行时还需将原版安装包中的 `x64/`、`x86/`（CefSharp 运行时）、`Resources/`（tessdata）、
`Images/`、`cate.json`、`mddph.sqlite` 等拷贝至 exe 同目录（本仓库不含发行包）。

## 登录说明

采用**本地授权**：任意账号密码即可登录（原版远程会员接口依赖已移除），
命令行携带 `--autotest` 可自动登录（自动化冒烟测试用）。

## 自动化测试

`test_e2e.py` / `test_dialogs*.py` / `test_labels.py`：基于 pywinauto 的真机 UI 自动化测试
（启动→登录→标签页遍历→弹窗验证→截图取证），依赖 `pip install pywinauto pillow`。

## 免责声明

本项目仅用于**自有软件源码丢失后的功能恢复与学习研究**，请勿用于任何侵权用途。
仓库不含原版发行包、第三方平台账号及任何凭据信息。
