# -*- coding: utf-8 -*-
"""卡密登录 + 个人登录信息 + 拼夕夕更名 测试（独立进程，卡密由 DB 预置）"""
import os, time, subprocess, re
from pywinauto import Desktop, Application

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
        p = os.path.join(SHOTS, name + ".png")
        win.capture_as_image().save(p); print("  shot:", p, flush=True)
    except Exception as e: print("  shot fail:", e, flush=True)

def db(sql):
    p = subprocess.run('"%s" -uroot -proot123!@# maque -e "%s"' % (MYSQL, sql),
                       shell=True, capture_output=True, text=True, encoding="utf-8", errors="ignore")
    return p.stdout or ""

def kill():
    os.system('taskkill /F /IM DuoDuoPuHuo.exe >nul 2>&1'); time.sleep(1)

def txts(win, maxn=400):
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

def set_cfg(rem, user, pwd):
    cfg = os.path.join(APPDIR, "DuoDuoPuHuo.exe.config")
    s = open(cfg, "r", encoding="utf-8", errors="ignore").read()
    s = re.sub(r'(<add key="isRemember" value=")[^"]*(")', lambda m: m.group(1)+rem+m.group(2), s)
    s = re.sub(r'(<add key="username" value=")[^"]*(")', lambda m: m.group(1)+user+m.group(2), s)
    s = re.sub(r'(<add key="password" value=")[^"]*(")', lambda m: m.group(1)+pwd+m.group(2), s)
    open(cfg, "w", encoding="utf-8").write(s)

def wait_win(app, title, timeout=15):
    end = time.time()+timeout
    while time.time() < end:
        try:
            w = app.window(title=title)
            if w.exists(timeout=0.5): return w
        except Exception: pass
        time.sleep(0.5)
    return None

def main():
    kill()
    # 预置卡密：有效 / 已使用 / 已失效 / 已过期（有效期单位覆盖 hour/day/month/year）
    db("delete from cards where card_no like 'E2E-%'")
    db("insert into cards (card_no,validity_value,validity_unit,status,created_at,expire_at,note) values "
       "('E2E-VALID-1H',1,'hour','active',NOW(),DATE_ADD(NOW(),INTERVAL 1 HOUR),'e2e'),"
       "('E2E-USED',1,'day','used',NOW(),DATE_ADD(NOW(),INTERVAL 1 DAY),'e2e'),"
       "('E2E-DISABLED',1,'month','disabled',NOW(),DATE_ADD(NOW(),INTERVAL 1 MONTH),'e2e'),"
       "('E2E-EXPIRED',1,'year','active',DATE_SUB(NOW(),INTERVAL 2 YEAR),DATE_SUB(NOW(),INTERVAL 1 YEAR),'e2e')")
    out = db("select card_no,status,validity_unit,expire_at from cards where card_no like 'E2E-%' order by card_no")
    rec("预置4种卡密状态（有效/已用/失效/过期）", out.count("E2E-")>=4, out.strip().replace("\n","; "))

    set_cfg("false","","")
    proc = subprocess.Popen([EXE], cwd=APPDIR)
    time.sleep(9)
    app = Application(backend="uia").connect(process=proc.pid)
    login = wait_win(app, "麻雀一键铺货 - 登录")
    if not login:
        rec("登录窗口启动", False); kill(); return
    rec("登录窗口启动", True)

    # 切卡密 Tab
    click(login, "卡密号登录", ["Button"]); time.sleep(0.5)
    edit = [c for c in login.descendants() if c.element_info.control_type=="Edit"]
    print("  login edit count:",len(edit),flush=True)
    if not edit:
        rec("卡密输入框存在",False); kill(); return
    card_input = edit[0]
    rec("卡密输入框存在", True)

    # 用例1：不存在的卡密
    card_input.set_edit_text("E2E-NOT-EXIST"); time.sleep(0.3)
    click(login, "登  录", ["Button"]); time.sleep(2)
    t = "|".join(txts(login,200))
    rec("无效卡密被拒绝", "卡密不存在" in t, t[t.find("卡密不存在")-10:t.find("卡密不存在")+30] if "卡密不存在" in t else "")

    # 用例2：已使用卡密
    card_input.set_edit_text("E2E-USED"); time.sleep(0.3)
    click(login, "登  录", ["Button"]); time.sleep(2)
    t = "|".join(txts(login,200))
    rec("已使用卡密被拒绝", "已被使用" in t, "")

    # 用例3：已失效卡密
    card_input.set_edit_text("E2E-DISABLED"); time.sleep(0.3)
    click(login, "登  录", ["Button"]); time.sleep(2)
    t = "|".join(txts(login,200))
    rec("已失效卡密被拒绝", "已失效" in t, "")

    # 用例4：已过期卡密
    card_input.set_edit_text("E2E-EXPIRED"); time.sleep(0.3)
    click(login, "登  录", ["Button"]); time.sleep(2)
    t = "|".join(txts(login,200))
    rec("已过期卡密被拒绝", "已过期" in t, "")

    # 用例5：有效卡密登录
    card_input.set_edit_text("E2E-VALID-1H"); time.sleep(0.3)
    click(login, "登  录", ["Button"]); time.sleep(9)
    main = wait_win(app, "麻雀一键铺货", timeout=12)
    rec("有效卡密登录进入主窗", main is not None, "")
    if main:
        shot(main, "94_main_after_card_login")
        # 卡密应被标记为已使用
        out = db("select status,bind_user from cards where card_no='E2E-VALID-1H'")
        rec("登录后卡密标记为已使用", "used" in out and "E2E-VALID-1H" in out, out.strip())
        # 需求1：更名
        tabs = [c.window_text() for c in main.descendants() if c.element_info.control_type=="TabItem"]
        rec("买手页已更名为拼夕夕", ("拼夕夕" in tabs and "买手页" not in tabs), "tabs=%s"%tabs)
        # 需求2：个人登录信息
        mt = txts(main,400)
        rec("个人登录信息入口存在", "个人登录信息" in mt, "")
        click(main, "个人登录信息", ["Text"]); time.sleep(2)
        pi = None
        for d in main.descendants():
            try:
                if (d.window_text() or "")=="个人登录信息" and d.element_info.control_type=="Window":
                    pi = d; break
            except Exception: pass
        if pi:
            pit = txts(pi,200)
            rec("个人登录信息窗打开", True, "")
            shot(pi, "95_personal_info")
            rec("展示卡密账号", any("E2E-VALID-1H" in x or "卡密用户" in x for x in pit), "")
            rec("退出登录按钮存在", "退出登录" in pit, "")
            rec("切换账号按钮存在", "切换账号" in pit, "")
            # 测试切换账号
            click(pi, "切换账号", ["Button"]); time.sleep(5)
            login3 = wait_win(app, "麻雀一键铺货 - 登录", timeout=10)
            rec("切换账号返回登录页", login3 is not None, "")
        else:
            rec("个人登录信息窗打开", False, "未找到子窗口")
    kill()
    print("=== DONE ===",flush=True)

if __name__=="__main__":
    try: main()
    except Exception as e:
        print("FATAL:",e,flush=True); results.append("[FAIL] FATAL | "+str(e)); kill()
    open(os.path.join(BASE,"test_card_login_out.txt"),"w",encoding="utf-8").write("\n".join(results)+"\n")
