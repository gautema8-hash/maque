# 麻雀一键铺货 (maque)

拼多多商品**一键采集 / 一键铺货**桌面工具（源码丢失后的逆向恢复重建版）。

> 本仓库为自有软件源码恢复项目：原程序为 .NET Framework 4.7.2 WPF 应用，因源码丢失，
> 通过静态逆向（Inno Setup 解包 + ILSpy 反编译 + BAML 文案还原）重建出完整可编译源码。
> 当前版本已完成**本地 MySQL 迁移**、**厂商生态完全隔离**与**多轮交互优化**，达到可交付状态。

## 功能特性

- **商品采集**：内嵌 CefSharp 浏览器登录拼多多买手号，按商品 ID 自动抓取标题、主图、详情图、SKU、规格、属性、类目
- **一键铺货**：驱动商家后台发布页状态机（分类→标题→主图→属性→详情图→SKU→价格→服务→草稿），全程可视化日志
- **裂变模式**：同品裂变 N 个标题 + 价格公式引擎（如 `A +2+ B * 0.02`）+ 图片裂变（图中图）
- **批量能力**：批量下架、一键卡大促、卡反拼单、商品监控、爆款推荐、评价/评论下载
- **辅助工具**：视频采集（抖音/快手）、盈亏计算器、目标投产自动计算、销量图表（LiveCharts）
- **店铺管理**：扫码登录即入库（MySQL）、多店铺 Cookie 隔离、勾选即选用、统一更新 Cookie
- **首页搜索**：商品搜索 / 店铺搜索，自动跳转买手页，未登录时给出明确提示

## 技术栈

| 层 | 技术 |
|---|---|
| UI | WPF (.NET Framework 4.7.2) + ZdfFlatUI(WPF.UI.dll) 控件库 |
| 浏览器内核 | CefSharp 111（x86/x64 双运行时，JS 双向桥 BoundObject） |
| 业务架构 | MaoDuoDuo(UI) + MDD.BLL / MDD.DAL / MDD.Models / MDD.Common / MDD.Utility |
| 存储 | **MySQL 8.0（maque 库，9 张业务表）**，可经配置切回 SQLite 单文件模式 |
| 图像/OCR | OpenCvSharp（水印/裂变图中图）、Tesseract（chi_sim） |
| 图表 | LiveCharts.Wpf |

## 目录结构

```
src/
├── MaoDuoDuo/            # 主窗口、登录窗、CefSharp 封装、JS 桥
├── MaoDuoDuo.Views/      # 首页/爆款/店铺管理/视频采集/商品监控/工具箱等视图
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

## 登录与账号

账号体系存储于**本地 MySQL**（`maque.users` 表，SHA256+盐哈希存储，不存明文）：

| 项 | 说明 |
|---|---|
| 默认管理员 | `admin` / `123456`（首次启动自动初始化） |
| 验证方式 | 登录时查询 MySQL 账号表并比对哈希，账号不存在/密码错误在窗内红条提示，不弹窗 |
| 添加账号 | `INSERT INTO maque.users (username, password_hash, role) VALUES ('用户名', SHA2(CONCAT('密码','\|maque#2026'), 256), 'user');` |
| 数据库切换 | `exe.config` 中 `DbType=MySql/Sqlite` 可切回 SQLite 模式（SQLite 同样内置 admin 账号） |
| 自动登录 | 勾选「记住密码」后，下次启动自动使用已保存账号登录，无需重复输入 |
| 登录窗 | 置顶显示不被遮挡；登录按钮常驻可见；失败信息在窗内提示；无头像与厂商按钮 |

命令行携带 `--autotest` 可自动登录并跳过记住密码逻辑（自动化冒烟测试用）。

> **安全提示**：自动登录依赖 `exe.config` 中明文保存的用户名/密码（`isRemember=true`）。
> 生产环境如需更安全，可改为 Windows 凭据管理器或 DPAPI 加密存储。

## 数据库与本地化

- 所有数据库访问集中在 `MDD.Utility/SqlHelper`，通过 `exe.config` 的 `DbType` 切换 MySQL / SQLite，上层 DAL 无需改动。
- MySQL 自动建表：`upload_info`、`upload_log`、`submit_log`、`dd_shop`、`dd_good_monitor`、
  `dd_good_monitor_info`、`dy_login`、`users` 等。
- **厂商生态完全隔离**：原版 `api.catduoduo.com`、`ddfxs.com`、`dogduoduo.cn` 等在线接口
  （公告、点数校验、更新、遥测、爆款推荐、申诉理由）已全部本地化；移除自动更新与官网/充值/续费入口。
- 店铺扫码登录后**立即写入数据库**：重复扫码更新 UUID 与手机号，不会再出现「查找店铺后记录消失」。

## 界面与交互

主窗口共 8 个标签页：**任务设置、买手页、商户页、首页、店铺管理、商品监控、视频采集、工具箱**
（原「售后服务」页已移除）。任务设置页重排为 10 个卡片区，功能不变、布局更清晰。

近期交互优化：

1. **自动登录**：勾选「记住密码」后下次启动免登录。
2. **登录窗按钮可见**：窗口布局调整，登录按钮完整显示；置顶防遮挡；失败窗内提示。
3. **已绑定店铺不重复绑定**：重复扫码识别为更新登录信息，而非新增。
4. **移除售后服务页**：精简主界面标签。
5. **首页搜索未登录提示**：商品/店铺搜索跳转买手页后若买手号未登录，顶部横幅明确提示；
   买手号与商家号为不同账号体系，需分别扫码登录。
6. **店铺管理重构**：新增「选用」勾选列，勾选即同步为当前使用店铺；
   「删除勾选店铺」替代原「删除失效店铺」；筛选标签扩展为多类目；统一更新 Cookie 用于批量刷新所选店铺登录态。

> 提示：买手页（mobile.yangkeduo.com）用于采集，商户页（mms.pinduoduo.com）用于铺货，二者 Cookie 相互隔离。

## 自动化测试

`test_e2e.py` / `test_dialogs*.py` / `test_labels.py` / `test_home_search.py` / `test_shopset.py` /
`test_mysql.py` / `test_login*.py` / `test_vendor_free.py` / `test_optimize6.py`：
基于 pywinauto 的真机 UI 自动化测试（启动→登录→标签页遍历→弹窗验证→截图取证），
依赖 `pip install pywinauto pillow`。

**测试注意**：请勿在包含真实店铺数据的库上自动执行删除类操作——早期回归测试曾误删真实扫码店铺记录，
后续测试脚本需对含数据的库跳过破坏性动作。

## 免责声明

本项目仅用于**自有软件源码丢失后的功能恢复与学习研究**，请勿用于任何侵权用途。
仓库不含原版发行包、第三方平台账号及任何凭据信息。

## 更新日志

- **v1.1（当前）** — 六项交互优化：自动登录 / 登录窗按钮可见与置顶 / 已绑定店铺不重复绑定 /
  售后服务页移除 / 首页搜索未登录提示 / 店铺管理勾选与筛选重构；README 同步至交付状态。
- **v1.0** — 源码恢复重建版发布。功能全量还原（采集/铺货/裂变/店铺管理/监控/视频采集/工具箱）；
  数据库切换本地 MySQL（双后端可配置）；厂商在线服务（会员/更新/遥测）完全本地化隔离；
  登录接入 MySQL 账号验证（SHA256+盐）；登录/主界面 UI 重设计；pywinauto 真机自动化测试脚本齐备。
