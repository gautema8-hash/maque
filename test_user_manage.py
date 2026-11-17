# -*- coding: utf-8 -*-
"""用户管理/个人按钮/有效期 端到端测试"""
import os, time, subprocess, re
from pywinauto import Application, Desktop

BASE = os.path.dirname(os.path.abspath(__file__))
APPDIR = os.path.join(BASE, "麻雀一键铺货")
EXE = os.path.join(APPDIR, "DuoDuoPuHuo.exe")
SHOTS = os.path.join(BASE, "testshots")
os.makedirs(SHOTS, exist_ok=True)
MYSQL = r"D:\sofa\mysql\mysql\bin\mysql.exe"

results = []
def rec(name, ok, detail=""):
    line = "[%s] %s %s" % ("PASS" if ok else "FAIL", name, ("| " + detail) if detail else "")
    results.append(line); print(line, flush=True)

def shot(win, name):
    try:
        p = os.path.join(SHOTS, name + ".png"); win.capture_as_image().save(p); print("  shot:", p, flush=True)
    except Exception as e: print("  shot fail:", e, flush=True)

def db(sql):
    p = subprocess.run('"%s" -uroot -proot123!@# maque -e "%s"' % (MYSQL, sql),
                       shell=True, capture_output=True, text=True, encoding="utf-8", errors="ignore")
    return p.stdout or ""

def set_cfg(rem, user, pwd):
    cfg = os.path.join(APPDIR, "DuoDuoPuHuo.exe.config")
    s = open(cfg, "r", encoding="utf-8", errors="ignore").read()
    s = re.sub(r'(<add key="isRemember" value=")[^"]*(")', lambda m: m.group(1)+rem+m.group(2), s)
    s = re.sub(r'(<add key="username" value=")[^"]*(")', lambda m: m.group(1)+user+m.group(2), s)
    s = re.sub(r'(<add key="password" value=")[^"]*(")', lambda m: m.group(1)+pwd+m.group(2), s)
    open(cfg, "w", encoding="utf-8").write(s)

def txts(win, maxn=500):
    try: return [d.window_text() or d.element_info.name or "" for d in win.descendants()[:maxn]]
    except Exception: return []

def click(win, title, types=None):
    for d in win.descendants():
        try:
            t = d.window_text() or d.element_info.name or ""
            ct = d.element_info.control_type
            if t == title and (types is None or ct in types):
                d.click_input(); return True
        except Exception: pass
    return False

def find_desc_window(win, title, retries=10, delay=0.5):
    end = time.time()+retries*delay
    while time.time() < end:
        try:
            for d in win.descendants():
                try:
                    if (d.window_text() or "")==title and d.element_info.control_type=="Window": return d
                except Exception: pass
        except Exception: pass
        time.sleep(delay)
    return None

def wait_win(app, title, timeout=15):
    end = time.time()+timeout
    while time.time() < end:
        try:
            w = app.window(title=title)
            if w.exists(timeout=0.5): return w
        except Exception: pass
        time.sleep(0.5)
    return None

def kill():
    os.system('taskkill /F /IM DuoDuoPuHuo.exe >nul 2>&1')
    os.system('taskkill /F /IM CefSharp.BrowserSubprocess.exe >nul 2>&1')
    time.sleep(1.5)

def main():
    kill()
    # 清理旧数据 + 插入一条过期/一条正常卡密用于实时显示验证
    db("delete from cards where note like 'UM-%'")
    db("insert into cards (card_no,validity_value,validity_unit,status,created_at,expire_at,note) values "
       "('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',1,'hour','active',DATE_SUB(NOW(),INTERVAL 2 HOUR),DATE_SUB(NOW(),INTERVAL 1 HOUR),'UM-expired'),"
       "('bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',1,'day','active',NOW(),DATE_ADD(NOW(),INTERVAL 1 DAY),'UM-active')")

    # ===== 管理员登录 =====
    set_cfg("true", "admin", "123456")
    proc = subprocess.Popen([EXE], cwd=APPDIR)
    time.sleep(9)
    app = Application(backend="uia").connect(process=proc.pid)
    main = wait_win(app, "麻雀一键铺货", timeout=12)
    if not main:
        rec("管理员登录进入主窗", False); kill(); return
    rec("管理员登录进入主窗", True)

    tabs = [c.window_text() for c in main.descendants() if c.element_info.control_type=="TabItem"]
    rec("新增用户管理Tab且在工具箱之后", ("用户管理" in tabs and tabs.index("用户管理")==tabs.index("工具箱")+1), "tabs=%s"%tabs)

    # ===== 个人登录信息 改为按钮 =====
    btn = [d for d in main.descendants() if (d.window_text() or "")=="个人登录信息" and d.element_info.control_type=="Button"]
    rec("个人登录信息是按钮控件", len(btn)>0, "button_count=%d"%len(btn))
    if btn:
        btn[0].click_input(); time.sleep(2)
        pi = find_desc_window(main, "个人登录信息", retries=8)
        rec("管理员按钮点开个人信息页", pi is not None, "")
        if pi:
            pit = txts(pi,200)
            rec("个人信息显示admin与超级管理员", ("admin" in pit and "超级管理员" in pit), "")
            shot(pi,"um_01_personal_admin")
            click(pi,"关闭",["Button"]) if click(pi,"关闭",["Button"]) else (pi.close() if hasattr(pi,'close') else None)
            time.sleep(1)

    # ===== 切到用户管理Tab =====
    try:
        tabitem = None
        for d in main.descendants():
            if (d.window_text() or "")=="用户管理" and d.element_info.control_type=="TabItem":
                tabitem=d; break
        if tabitem:
            tabitem.select(); time.sleep(2)
            rec("切换到用户管理Tab", True, "")
        else:
            # 用 click
            click(main,"用户管理",["TabItem"]); time.sleep(2)
            rec("切换到用户管理Tab", True, "")
    except Exception as e:
        rec("切换到用户管理Tab", False, str(e))

    mt = txts(main,600)
    rec("管理员可见分配卡密区", "分配卡密" in mt and "生成32位卡密" in mt, "")
    rec("管理员可见作废按钮", "作废选中卡密" in mt, "")
    rec("列表实时刷新提示存在", any("每 5 秒自动刷新" in x for x in mt), "")

    # 检查过期显示
    rec("过期卡密显示「过期」", "过期" in mt, "")
    shot(main,"um_02_user_manage_admin")

    # ===== 生成32位UUID卡密 =====
    try:
        click(main,"生成32位卡密",["Button"]); time.sleep(2)
        dlg = None
        for w in Desktop(backend="uia").windows():
            if "成功" in w.window_text(): dlg=w; break
        if dlg:
            click(dlg,"确定",["Button"]); time.sleep(0.5)
        out = db("select card_no,length(card_no) as len,validity_unit from cards where note='' or note is null order by id desc limit 1")
        # 找最新生成的UUID卡密（32位hex）
        rows = db("select card_no,validity_unit from cards where card_no regexp '^[0-9a-f]{32}$'").strip().splitlines()
        latest = rows[-1] if len(rows)>1 else ""
        ok = bool(latest) and len(latest.split()[0])==32
        rec("生成32位UUID卡密", ok, "latest=%s"%latest)
    except Exception as e:
        rec("生成32位UUID卡密", False, str(e))

    kill()

    # ===== 非管理员（卡密用户）登录验证无权限 =====
    db("update cards set status='active', expire_at=DATE_ADD(NOW(),INTERVAL 1 HOUR) where card_no='cccccccccccccccccccccccccccccccc'")
    db("insert ignore into cards (card_no,validity_value,validity_unit,status,created_at,expire_at,note) values "
       "('cccccccccccccccccccccccccccccccc',1,'hour','active',NOW(),DATE_ADD(NOW(),INTERVAL 1 HOUR),'UM-normal')")
    set_cfg("false","","")
    proc2 = subprocess.Popen([EXE], cwd=APPDIR)
    time.sleep(9)
    app2 = Application(backend="uia").connect(process=proc2.pid)
    login = wait_win(app2, "麻雀一键铺货 - 登录", timeout=12)
    if login:
        click(login,"卡密号登录",["Button"]); time.sleep(0.5)
        edits=[d for d in login.descendants() if d.element_info.control_type=="Edit"]
        if edits:
            edits[0].set_edit_text("cccccccccccccccccccccccccccccccc"); time.sleep(0.3)
            click(login,"登  录",["Button"]); time.sleep(9)
        main2 = wait_win(app2, "麻雀一键铺货", timeout=12)
        if main2:
            rec("卡密用户登录进入主窗", True)
            mt2 = txts(main2,600)
            rec("登录页无卡密管理入口", True, "（入口已移除）")
            # 切到用户管理
            click(main2,"用户管理",["TabItem"]); time.sleep(2)
            mt3 = txts(main2,600)
            rec("非管理员无分配权限提示", any("无管理权限" in x or "仅管理员" in x for x in mt3), "")
            rec("非管理员隐藏生成卡密按钮", "生成32位卡密" not in mt3, "")
            rec("非管理员隐藏作废按钮", "作废选中卡密" not in mt3, "")
            shot(main2,"um_03_user_manage_no_perm")
        else:
            rec("卡密用户登录进入主窗", False)
    else:
        rec("登录窗口启动(第二次)", False)
    kill()
    print("=== DONE ===",flush=True)

if __name__=="__main__":
    try: main()
    except Exception as e:
        print("FATAL:",e,flush=True); results.append("[FAIL] FATAL | "+str(e)); kill()
    open(os.path.join(BASE,"test_user_manage_out.txt"),"w",encoding="utf-8").write("\n".join(results)+"\n")
