# -*- coding: utf-8 -*-
"""隔离验证 复制/编辑/删除（每步独立重启应用，避免模态阻塞）"""
import os, time, subprocess, re, sys
from pywinauto import Application, Desktop
import win32clipboard
BASE=os.path.dirname(os.path.abspath(__file__)); APPDIR=os.path.join(BASE,"麻雀一键铺货"); EXE=os.path.join(APPDIR,"DuoDuoPuHuo.exe")
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
def launch():
    kill(); cfg("true","admin","123456"); proc=subprocess.Popen([EXE],cwd=APPDIR); time.sleep(9)
    app=Application(backend="uia").connect(process=proc.pid)
    main=None
    end=time.time()+12
    while time.time()<end:
        try:
            w=app.window(title="麻雀一键铺货")
            if w.exists(timeout=0.5): main=w; break
        except Exception: pass
        time.sleep(0.5)
    if main:
        for d in main.descendants():
            if (d.window_text() or "")=="用户管理" and d.element_info.control_type=="TabItem": d.click_input(); break
        time.sleep(2)
    return app,main

def test_copy():
    app,main=launch()
    if not main: rec("复制-应用启动",False); return
    try:
        win32clipboard.OpenClipboard(); win32clipboard.EmptyClipboard(); win32clipboard.CloseClipboard()
    except Exception: pass
    copys=[d for d in main.descendants() if (d.window_text() or "")=="复制" and d.element_info.control_type=="Button"]
    if not copys: rec("复制按钮存在",False); kill(); return
    copys[0].click_input(); time.sleep(1.5)
    clip=""
    try:
        win32clipboard.OpenClipboard(); clip=win32clipboard.GetClipboardData(); win32clipboard.CloseClipboard()
    except Exception as e: clip=""
    rec("复制卡密到剪贴板",len(clip)==32 and clip.isalnum(),"clip=%s"%clip)
    kill()

def test_edit():
    app,main=launch()
    if not main: rec("编辑-应用启动",False); return
    edits=[d for d in main.descendants() if (d.window_text() or "")=="编辑" and d.element_info.control_type=="Button"]
    if not edits: rec("编辑按钮存在",False); kill(); return
    edits[0].click_input(); time.sleep(2)
    ed=None
    # 模态窗可能嵌套在 main 下，遍历 main 后代找含"保存"按钮的 Window 控件
    for d in main.descendants():
        try:
            if d.element_info.control_type=="Window":
                btns=[x.window_text() or "" for x in d.descendants() if x.element_info.control_type=="Button"]
                if "保存" in btns and "取消" in btns: ed=d; break
        except Exception: pass
    if ed is None:
        for w in Desktop(backend="uia").windows():
            wt="|".join(x.window_text() or "" for x in w.descendants()[:60])
            if "编辑卡密" in wt and "保存" in wt: ed=w; break
    rec("编辑窗口打开",ed is not None, "")
    if ed:
        try: ed.capture_as_image().save(os.path.join(BASE,"testshots","ui_03_edit_dialog.png"))
        except Exception: pass
        # 获取卡密号
        card=""
        for d in ed.descendants():
            try:
                if d.element_info.control_type=="Edit" and len(d.window_text())==32: card=d.window_text(); break
            except Exception: pass
        ed_texts=[d.window_text() or "" for d in ed.descendants()]
        # 找备注输入框（最后一个Edit，排除card/value）
        eds=[d for d in ed.descendants() if d.element_info.control_type=="Edit"]
        if eds:
            eds[-1].set_edit_text("UI-EDITED-OK")
        # 保存
        saved=False
        for d in ed.descendants():
            try:
                if (d.window_text() or "")=="保存" and d.element_info.control_type=="Button": d.click_input(); saved=True; break
            except Exception: pass
        time.sleep(1.5)
        out=db("select count(*) from cards where note='UI-EDITED-OK'")
        cnt=int(out.strip().splitlines()[-1]) if out.strip().splitlines()[-1].strip().isdigit() else 0
        rec("编辑保存生效",cnt>=1,"note=UI-EDITED-OK, count=%d"%cnt)
    kill()

def test_delete():
    app,main=launch()
    if not main: rec("删除-应用启动",False); return
    before=db("select count(*) from cards").strip().splitlines()[-1]
    dels=[d for d in main.descendants() if (d.window_text() or "")=="删除" and d.element_info.control_type=="Button"]
    if not dels: rec("删除按钮存在",False); kill(); return
    dels[0].click_input(); time.sleep(1.5)
    # 找确认弹窗：遍历所有顶层窗口含"是"/"Yes"按钮
    confirmed=False
    for w in Desktop(backend="uia").windows():
        for d in w.descendants():
            try:
                t=d.window_text() or ""
                if t in ("是","Yes","确定") and d.element_info.control_type=="Button":
                    # 确认是本程序的弹窗（含"确认删除"文本）
                    wt="|".join(x.window_text() or "" for x in w.descendants()[:60])
                    if "确认删除" in wt or "删除后不可恢复" in wt:
                        d.click_input(); confirmed=True; break
            except Exception: pass
        if confirmed: break
    if not confirmed:
        import pywinauto.keyboard as kb; kb.send_keys("{ENTER}")
    time.sleep(2)
    after=db("select count(*) from cards").strip().splitlines()[-1]
    rec("删除卡密生效",int(after)<int(before),"before=%s after=%s confirmed=%s"%(before,after,confirmed))
    kill()

if __name__=="__main__":
    try:
        db("insert ignore into cards (card_no,validity_value,validity_unit,status,created_at,expire_at,note) values ('33333333333333333333333333333333',1,'day','active',NOW(),DATE_ADD(NOW(),INTERVAL 1 DAY),'UI-delete-me')")
        test_copy(); test_edit(); test_delete()
    except Exception as e:
        print("FATAL:",e,flush=True); results.append("[FAIL] FATAL | "+str(e)); kill()
    open(os.path.join(BASE,"test_ui_ops_out.txt"),"w",encoding="utf-8").write("\n".join(results)+"\n")
