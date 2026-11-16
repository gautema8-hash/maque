# 麻雀一键铺货 — 入库数据清单 & MySQL 迁移评审

> 📅 2026-11-16　|　当前存储：SQLite 单文件 `mddph.sqlite`（无密码）

---

## 一、哪些东西会入库（完整清单）

### 写入的数据（按触发时机）

| # | 表 | 写入时机 | 写入内容 | 代码位置 |
|---|---|---|---|---|
| 1 | **upload_info** | 每次铺货**成功** | 原商品ID → 上传后新ID、时间、结果"上传完成" | `ThreadPublish` 步骤17 → ShopSetBLL.AddShop |
| 2 | **upload_info** | 每次铺货**失败** | 原商品ID、"上传失败：原因"、时间 | ThreadPublish 异常分支 → ShopSetBLL.AddShop |
| 3 | **submit_log** | 铺货**超时/异常** | 商品ID、数量、时间（同时走远程遥测 member/client/log，该远程调用当前处于休眠失败状态不影响本地） | ThreadPublish 超时分支 → SubmitLogBLL.AddShop |
| 4 | **dd_good_monitor** | 采集商品成功后 | 商品ID、名称、数量、状态（加入监控列表） | 采集 FrameLoadEnd → GoodMonitorBLL.AddGoodMonitor |
| 5 | **dd_good_monitor_info** | 监控采集历史 | 商品ID、名称、数量快照（销量走势数据源） | 同上 → GoodMonitorBLL.AddGoodMonitorInfo |
| 6 | **dd_shop** | 店铺管理 | 店铺ID/UUID/名称/手机号/类目/选用状态 | 扫码登录流程（UpdateShopState 更新选用状态） |
| 7 | **dy_login** | 视频采集登录 | 抖音 Cookie 名/值（单行覆盖更新） | 视频采集 Visitor_SendCookie → UpdateDyCookie |

### 只读/遗留

| 表 | 说明 |
|---|---|
| upload_log | **纯遗留表，无任何代码写入**（结构同 upload_info，原版历史数据） |
| ceshi | 原版调试表，未建、未用 |

### ⚠️ 不入库的运行时数据（文件级，迁移时容易遗漏）

| 数据 | 位置 | 说明 |
|---|---|---|
| 商品图片 | `goodImg\<商品ID>\top|detail|sku` | 采集的图片本地落盘 |
| 各平台登录态 Cookie | `cache\pinduoduo` / `cache\shangjia` / `cache\pddshop\<uuid>` / `cache\dycache` | CefSharp 文件级缓存 |
| 应用配置 | `setting.json`、`exe.config` | 图片加工参数、账号记忆 |
| 运行日志 | `Log\*.log` | |

> 结论：**入库的只有业务记录与少量登录态（dy_login）**，图片/Cookie 均为文件存储，MySQL 化不涉及它们。

---

## 二、改为 MySQL 可行性评审

### 结论：✅ 可行，且改造面很小

**核心有利条件**：全部数据库访问收敛在 `MDD.Utility/SqlHelper` 一个类里（`Query(sql)` / `Update(sql, getId)` 两个入口 + 单例连接），**上层 8 个 DAL 文件零改动**即可切换。

### 收益（什么场景值得上 MySQL）

| 场景 | 收益 |
|---|---|
| 多台电脑铺货，共享店铺列表/上传记录/监控数据 | ⭐ 核心价值，SQLite 无法多机共享 |
| 集中备份与统计（上传成功率、铺货量报表） | SQL 查询能力强 |
| 数据量增长到十万级以上 | SQLite 单文件写入锁瓶颈 |
| 与你现有的本地 MySQL 8.0.44 基础设施统一管理 | 已有运维经验 |

### 风险与代价

| 风险 | 说明 | 应对 |
|---|---|---|
| 外部服务依赖 | SQLite 零部署；MySQL 要求服务运行+网络可达 | 本机已有 MySQL 8.0.44；连接串写入配置 |
| SQL 方言差异 | `last_insert_rowid()` → `LAST_INSERT_ID()`；`AUTOINCREMENT` → `AUTO_INCREMENT` | SqlHelper 内按后端类型分支，约 10 行 |
| 密码存储 | 连接串含密码 | exe.config 配置节 + 可选 DPAPI 加密 |
| 现有 SQL 为字符串拼接 | 注入风险（本地单用户，风险低） | 迁移时顺带参数化（可选） |
| `value` 列名 | dy_login.value 在 MySQL 中非保留字，可用；稳妥加反引号 | DDL 已处理 |

### 工作量评估

| 项 | 工作量 |
|---|---|
| SqlHelper 双后端改造（SQLite/MySQL 配置切换） | 约 1-2 小时（含方言分支） |
| MySQL DDL + 数据迁移工具 | 约 1 小时 |
| 全功能回归测试 | 约 1-2 小时 |
| **合计** | **半天内可交付** |

---

## 三、MySQL DDL 脚本（MySQL 8.0，utf8mb4）

```sql
-- 建库
CREATE DATABASE IF NOT EXISTS maque DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE maque;

-- 铺货上传记录（主表）
CREATE TABLE IF NOT EXISTS upload_info (
  id          INT AUTO_INCREMENT PRIMARY KEY,
  shop_name   VARCHAR(255) NULL,
  good_id     VARCHAR(64)  NULL,
  good_id2    VARCHAR(64)  NULL,
  upload_time VARCHAR(32)  NULL,
  result      VARCHAR(255) NULL,
  state       INT DEFAULT 1,
  INDEX idx_good_id (good_id)
) ENGINE=InnoDB;

-- 铺货上传记录（遗留表，保持同结构）
CREATE TABLE IF NOT EXISTS upload_log (
  id          INT AUTO_INCREMENT PRIMARY KEY,
  shop_name   VARCHAR(255) NULL,
  good_id     VARCHAR(64)  NULL,
  good_id2    VARCHAR(64)  NULL,
  upload_time VARCHAR(32)  NULL,
  result      VARCHAR(255) NULL,
  state       INT DEFAULT 1
) ENGINE=InnoDB;

-- 铺货异常记录
CREATE TABLE IF NOT EXISTS submit_log (
  id          INT AUTO_INCREMENT PRIMARY KEY,
  good_id     VARCHAR(64) NULL,
  num         INT NULL,
  create_time VARCHAR(32) NULL,
  INDEX idx_good_id (good_id)
) ENGINE=InnoDB;

-- 拼多多店铺列表
CREATE TABLE IF NOT EXISTS dd_shop (
  id           INT AUTO_INCREMENT PRIMARY KEY,
  shop_id      VARCHAR(64)  NULL,
  uuid         VARCHAR(64)  NULL,
  name         VARCHAR(255) NULL,
  mobile       VARCHAR(32)  NULL,
  staple       VARCHAR(255) NULL,
  choose_state INT DEFAULT 0,
  create_time  VARCHAR(32)  NULL,
  INDEX idx_shop_id (shop_id)
) ENGINE=InnoDB;

-- 商品监控列表
CREATE TABLE IF NOT EXISTS dd_good_monitor (
  id          INT AUTO_INCREMENT PRIMARY KEY,
  good_id     VARCHAR(64)  NULL,
  good_name   VARCHAR(255) NULL,
  good_num    INT DEFAULT 0,
  good_state  INT DEFAULT 1,
  update_time VARCHAR(32)  NULL,
  create_time VARCHAR(32)  NULL,
  INDEX idx_good_id (good_id)
) ENGINE=InnoDB;

-- 商品监控历史明细
CREATE TABLE IF NOT EXISTS dd_good_monitor_info (
  id          INT AUTO_INCREMENT PRIMARY KEY,
  good_id     VARCHAR(64)  NULL,
  good_name   VARCHAR(255) NULL,
  good_num    INT DEFAULT 0,
  update_time VARCHAR(32)  NULL,
  create_time VARCHAR(32)  NULL,
  INDEX idx_good_id (good_id)
) ENGINE=InnoDB;

-- 抖音登录态（value 非保留字，稳妥加反引号）
CREATE TABLE IF NOT EXISTS dy_login (
  id    INT PRIMARY KEY,
  name  VARCHAR(255) NULL,
  `value` LONGTEXT NULL
) ENGINE=InnoDB;

-- 种子行
INSERT INTO dy_login (id, name, `value`)
SELECT 1, '', ''
WHERE NOT EXISTS (SELECT 1 FROM dy_login WHERE id = 1);
```

---

## 四、分阶段迁移计划

### 阶段 0：准备（30 分钟）
- [ ] 本机 MySQL 建库建表（执行第三节 DDL）
- [ ] 下载 `MySql.Data.dll`（8.x，.NET Framework 4.7.2 兼容版）放入 `src/libs/`
- [ ] `exe.config` 增加配置节：
```xml
<appSettings>
  <add key="DbType" value="MySql" />  <!-- Sqlite | MySql -->
  <add key="MySqlConn" value="server=127.0.0.1;port=3306;database=maque;uid=root;pwd=你的密码;charset=utf8mb4;SslMode=none;" />
</appSettings>
```

### 阶段 1：SqlHelper 双后端改造（核心，1-2 小时）
- [ ] `SqlHelper` 增加静态 DbType 判断（读配置）
- [ ] `Open()` 方法：按 DbType 返回 SQLiteConnection 或 MySqlConnection
- [ ] `Query/Update` 保持签名不变，内部按后端执行
- [ ] `getId` 分支：SQLite 用 `last_insert_rowid()`，MySQL 用 `LAST_INSERT_ID()`（同连接内查）
- [ ] EnsureSchema 保留（SQLite 用）；MySQL 建表走 DDL 脚本（阶段 0 已建）
- [ ] **上层 8 个 DAL 文件零改动**

### 阶段 2：存量数据迁移（30 分钟）
- [ ] 写一次性脚本（Python）：读 `mddph.sqlite` → 写 MySQL（7 张表逐行 INSERT）
- [ ] 校验：各表行数对比 + 抽样字段对比

### 阶段 3：功能回归（1-2 小时）
- [ ] 铺货一次 → 验证 upload_info 有记录
- [ ] 店铺管理：查找/全选/选中使用/删除失效 → dd_shop 读写正常
- [ ] 商品监控：采集 → dd_good_monitor(_info) 写入
- [ ] 视频采集登录 → dy_login 更新
- [ ] 铺货异常路径 → submit_log 写入
- [ ] 真机 UIA 回归（复用 test_shopset.py / test_e2e.py）

### 阶段 4：文档与交付（30 分钟）
- [ ] 账号密码清单.md 增加 MySQL 连接信息
- [ ] 数据库脚本.txt 追加 MySQL DDL
- [ ] README 更新运行依赖说明

### 回滚方案
配置节 `DbType` 改回 `Sqlite` 即回到原状态；MySQL 库可随时 DROP，SQLite 文件始终保留。

---

## 五、建议

- **单机自用** → 维持 SQLite（零部署、零依赖，当前运行良好）；
- **多机协同 / 想用 SQL 做铺货统计报表 / 数据量大** → 上 MySQL，按上述计划半天交付；
- 折中方案：SqlHelper 双后端保留配置切换，两种模式随时可用（推荐，改造后两种都是一等公民）。

确认后我可直接实施阶段 0-2（代码改造 + 数据迁移工具），并跑完整回归。
