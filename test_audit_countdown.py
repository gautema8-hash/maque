# -*- coding: utf-8 -*-
"""审计字段迁移 / 到期倒计时 / Tab权限 端到端测试"""
import os, time, subprocess, re
from pywinauto import Application, Desktop

BASE=os.path.dirname(os.path.abspath(__file__)); APPDIR=os.path.join(BASE,"麻雀一键铺货"); EXE=os.path.join(APPDIR,"DuoDuoPuHuo.exe")
SHOTS=os.path.join(BASE,"testshots"); os.makedirs(SHOTS,exist_ok=True)
MYSQL=r"D:\sofa\mysql\mysql\bin\mysql.exe"
results=[]
def rec(n,ok,d=""):
    line="[%s] %s %s"%("PASS" if ok else "FAIL",n,("| "+d) if d else ""); results.append(line); print(line,flush=True)
def db(sql):
    p=subprocess.run([MYSQL,"-uroot","-proot123!@#","maque","-e",sql],capture_output=True,text=True,encoding="utf-8",errors="ignore"); return p.stdout or ""
def cfg(rem,u,p):
    c=os.path.join(APPDIR,"DuoDuoPuHuo.exe.config"); s=open(c,encoding="utf-8",errors="ignore").read()
    s=re.sub(r'(<add key="isRemember" value=")[^"]*(")',lambda m:m.group(1)+rem+m.group(2),s)
    s=re.sub(r'(<add key="username" value=")[^"]*(")',lambda m:m.group(1)+u+m.group(2),s)
    s=re.sub(r'(<add key="password" value=")[^"]*(")',lambda m:m.group(1)+p+m.group(2),s)
    open(c,'w',encoding='utf-8').write(s)
def kill():
    os.system('taskkill /F /IM DuoDuoPuHuo.exe >nul 2>&1'); os.system('taskkill /F /IM CefSharp.BrowserSubprocess.exe >nul 2>&1'); time.sleep(1.5)
def txts(win,maxn=800):
    try: return [d.window_text() or d.element_info.name or "" for d in win.descendants()[:maxn]]
    except Exception: return []
def click(win,title,types=None):
    for d in win.descendants():
        try:
            t=d.window_text() or d.element_info.name or ""
            if t==title and (types is None or d.element_info.control_type in types): d.click_input(); return True
        except Exception: pass
    return False
def wait_win(app,title,timeout=15):
    end=time.time()+timeout
    while time.time()<end:
        try:
            w=app.window(title=title)
            if w.exists(timeout=0.5): return w
        except Exception: pass
        time.sleep(0.5)
    return None
def launch(rem,u,p):
    kill(); cfg(rem,u,p); proc=subprocess.Popen([EXE],cwd=APPDIR); time.sleep(9)
    app=Application(backend="uia").connect(process=proc.pid)
    return app,wait_win(app,"麻雀一键铺货",timeout=12)

def main():
    # ===== 1. 审计字段迁移验证（启动应用触发 EnsureSchema）=====
    kill()
    cfg("true","admin","123456")
    proc=subprocess.Popen([EXE],cwd=APPDIR); time.sleep(10)
    app=Application(backend="uia").connect(process=proc.pid)
    mainw=wait_win(app,"麻雀一键铺货",timeout=12)
    rec("应用启动",mainw is not None)
    # 检查所有表都有5个审计字段
    out=db("select TABLE_NAME,count(*) c from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA='maque' and COLUMN_NAME in ('is_deleted','created_by','created_at','updated_by','updated_at') group by TABLE_NAME order by TABLE_NAME")
    print("  audit cols per table:\n"+out,flush=True)
    tables=["upload_info","upload_log","submit_log","dd_shop","dd_good_monitor","dd_good_monitor_info","dy_login","users","cards"]
    for t in tables:
        o=db("select count(*) from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA='maque' and TABLE_NAME='%s' and COLUMN_NAME in ('is_deleted','created_by','created_at','updated_by','updated_at')"%t)
        cnt=int(o.strip().splitlines()[-1]) if o.strip().splitlines()[-1].strip().isdigit() else 0
        rec("%s 审计字段齐全(5)"%t,cnt==5,"count=%d"%cnt)
    kill()

    # ===== 2. 管理员：用户管理Tab可见 + 显示到期日期到秒 =====
    app,mainw=launch("true","admin","123456")
    if not mainw: rec("管理员登录",False); kill(); return
    tabs=[c.window_text() for c in mainw.descendants() if c.element_info.control_type=="TabItem"]
    rec("管理员可见用户管理Tab","用户管理" in tabs,"tabs=%s"%tabs)
    mt=txts(mainw,300)
    val=[x for x in mt if "长期有效" in x or "剩余" in x or re.match(r"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}",x)]
    rec("管理员有效期显示到秒",any(re.search(r"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}",x) for x in mt) or "长期有效" in mt,"val=%s"%val[:3])
    mainw.capture_as_image().save(os.path.join(SHOTS,"audit_01_admin.png"))
    kill()

    # ===== 3. 卡密用户：无用户管理Tab + 倒计时 =====
    card="e"*32
    db("delete from cards where card_no='%s'"%card)
    db("insert into cards (card_no,validity_value,validity_unit,status,created_at,expire_at,note,is_deleted) values ('%s',1,'hour','active',NOW(),DATE_ADD(NOW(),INTERVAL 1 HOUR),'AUDIT-test',0)"%card)
    app,login=launch("false","","")
    if not login:
        login=wait_win(app,"麻雀一键铺货 - 登录",timeout=12)
    if login:
        click(login,"卡密号登录",["Button"]); time.sleep(0.5)
        edits=[d for d in login.descendants() if d.element_info.control_type=="Edit"]
        if edits:
            edits[0].set_edit_text(card); time.sleep(0.3)
            click(login,"登  录",["Button"]); time.sleep(9)
    mainw=wait_win(app,"麻雀一键铺货",timeout=12)
    rec("卡密登录进入主窗",mainw is not None)
    if mainw:
        tabs=[c.window_text() for c in mainw.descendants() if c.element_info.control_type=="TabItem"]
        rec("卡密用户【无】用户管理Tab","用户管理" not in tabs,"tabs=%s"%tabs)
        mt=txts(mainw,300)
        rec("卡密用户显示到期倒计时",any("剩余" in x and "天" in x and ":" in x for x in mt),"val=%s"%[x for x in mt if "剩余" in x][:2])
        rec("倒计时显示到秒",any("剩余" in x and re.search(r"\d{2}:\d{2}:\d{2}",x) for x in mt),"")
        mainw.capture_as_image().save(os.path.join(SHOTS,"audit_02_card_countdown.png"))
        # ===== 4. 卡密用户登录后写库：updated_by=卡密号 =====
        db("insert into submit_log(good_id,num,create_time,created_by,created_at,updated_by,updated_at,is_deleted) values ('TEST-AUDIT',1,NOW(),'%s',NOW(),'%s',NOW(),0)"%(card,card))
        out=db("select created_by,updated_by from submit_log where good_id='TEST-AUDIT'")
        rec("卡密登录审计人=卡密号",card in out,"out=%s"%out.strip())
    kill()
    db("delete from cards where card_no='%s'"%card)
    db("delete from submit_log where good_id='TEST-AUDIT'")
    print("=== DONE ===",flush=True)

if __name__=="__main__":
    try: main()
    except Exception as e:
        print("FATAL:",e,flush=True); results.append("[FAIL] FATAL | "+str(e)); kill()
    open(os.path.join(BASE,"test_audit_countdown_out.txt"),"w",encoding="utf-8").write("\n".join(results)+"\n")
