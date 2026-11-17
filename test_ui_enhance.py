# -*- coding: utf-8 -*-
"""UI增强测试：对比色按钮 / 复制-编辑-删除 / 查询 / 二级窗关闭"""
import os, time, subprocess, re
from pywinauto import Application, Desktop
import win32clipboard

BASE = os.path.dirname(os.path.abspath(__file__))
APPDIR = os.path.join(BASE, "麻雀一键铺货")
EXE = os.path.join(APPDIR, "DuoDuoPuHuo.exe")
SHOTS = os.path.join(BASE, "testshots")
os.makedirs(SHOTS, exist_ok=True)
MYSQL = r"D:\sofa\mysql\mysql\bin\mysql.exe"

results=[]
def rec(n,ok,d=""):
    line="[%s] %s %s"%("PASS" if ok else "FAIL",n,("| "+d) if d else "")
    results.append(line); print(line,flush=True)

def shot(win,n):
    try: win.capture_as_image().save(os.path.join(SHOTS,n+".png")); print("  shot:",n,flush=True)
    except Exception as e: print("  shot fail:",e,flush=True)

def db(sql):
    p=subprocess.run('"%s" -uroot -proot123!@# maque -e "%s"'%(MYSQL,sql),shell=True,capture_output=True,text=True,encoding="utf-8",errors="ignore")
    return p.stdout or ""

def set_cfg(rem,u,p):
    cfg=os.path.join(APPDIR,"DuoDuoPuHuo.exe.config")
    s=open(cfg,"r",encoding="utf-8",errors="ignore").read()
    s=re.sub(r'(<add key="isRemember" value=")[^"]*(")',lambda m:m.group(1)+rem+m.group(2),s)
    s=re.sub(r'(<add key="username" value=")[^"]*(")',lambda m:m.group(1)+u+m.group(2),s)
    s=re.sub(r'(<add key="password" value=")[^"]*(")',lambda m:m.group(1)+p+m.group(2),s)
    open(cfg,"w",encoding="utf-8").write(s)

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

def find_desc_win(win,title,retries=8,delay=0.5):
    end=time.time()+retries*delay
    while time.time()<end:
        try:
            for d in win.descendants():
                try:
                    if (d.window_text() or "")==title and d.element_info.control_type=="Window": return d
                except Exception: pass
        except Exception: pass
        time.sleep(delay)
    return None

def wait_win(app,title,timeout=15):
    end=time.time()+timeout
    while time.time()<end:
        try:
            w=app.window(title=title)
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
    db("delete from cards where note like 'UI-%'")
    db("insert into cards (card_no,validity_value,validity_unit,status,created_at,expire_at,note) values "
       "('11111111111111111111111111111111',2,'day','active',NOW(),DATE_ADD(NOW(),INTERVAL 2 DAY),'UI-test1'),"
       "('22222222222222222222222222222222',1,'hour','active',NOW(),DATE_ADD(NOW(),INTERVAL 1 HOUR),'UI-test2')")
    set_cfg("true","admin","123456")
    proc=subprocess.Popen([EXE],cwd=APPDIR)
    time.sleep(9)
    app=Application(backend="uia").connect(process=proc.pid)
    main=wait_win(app,"麻雀一键铺货",timeout=12)
    if not main: rec("管理员登录",False); kill(); return
    rec("管理员登录",True)

    # 1) 个人信息按钮对比色（检查是Button且背景非透明）
    btn=[d for d in main.descendants() if (d.window_text() or "")=="个人登录信息" and d.element_info.control_type=="Button"]
    rec("个人信息是按钮",len(btn)>0)
    # 从XAML确认对比色
    xaml=open(os.path.join(BASE,"src","MaoDuoDuo","MainWindow.xaml"),encoding="utf-8").read()
    rec("按钮使用对比色背景(#FFD166)", 'Background="#FFD166"' in xaml and 'btnPersonal' in xaml, "")
    shot(main,"ui_01_contrast_button")

    # 2) 切到用户管理
    click(main,"用户管理",["TabItem"]); time.sleep(2)
    mt=txts(main,800)
    rec("每行有复制按钮", mt.count("复制")>=2, "copy_count=%d"%mt.count("复制"))
    rec("每行有编辑按钮", mt.count("编辑")>=2, "edit_count=%d"%mt.count("编辑"))
    rec("每行有删除按钮", mt.count("删除")>=2, "delete_count=%d"%mt.count("删除"))
    rec("查询区存在", "查询卡密" in mt and "查询" in mt and "全部" in mt, "")
    shot(main,"ui_02_user_manage_buttons")

    # 3) 查询功能
    search=[d for d in main.descendants() if d.element_info.control_type=="Edit" and "查询" in (d.element_info.name or "")]
    edits=[d for d in main.descendants() if d.element_info.control_type=="Edit"]
    # 找查询输入框（通常在用户管理页）
    sbox=None
    for e in edits:
        try:
            if e.element_info.name and ("卡密号" in e.element_info.name or "关键字" in e.element_info.name): sbox=e; break
        except Exception: pass
    if sbox is None and len(edits)>1: sbox=edits[-1]
    if sbox:
        sbox.set_edit_text("UI-test1"); time.sleep(0.3)
        click(main,"查询",["Button"]); time.sleep(1.5)
        mt2=txts(main,800)
        rec("查询过滤生效", "UI-test1" in mt2 and "UI-test2" not in mt2, "")
        click(main,"全部",["Button"]); time.sleep(1)
        mt3=txts(main,800)
        rec("重置显示全部", "UI-test2" in mt3, "")
    else:
        rec("查询输入框定位",False)

    # 4) 复制卡密
    try:
        win32clipboard.OpenClipboard(); win32clipboard.EmptyClipboard(); win32clipboard.CloseClipboard()
        copys=[d for d in main.descendants() if (d.window_text() or "")=="复制" and d.element_info.control_type=="Button"]
        if copys:
            copys[0].click_input(); time.sleep(1)
            win32clipboard.OpenClipboard();
            try: clip=win32clipboard.GetClipboardData()
            except Exception: clip=""
            win32clipboard.CloseClipboard()
            rec("复制卡密到剪贴板", len(clip)==32 and clip.isalnum(), "clip=%s"%clip)
            dlg=None
            for w in Desktop(backend="uia").windows():
                if "复制成功" in w.window_text(): dlg=w; break
            if dlg: click(dlg,"确定",["Button"]); time.sleep(0.5)
        else:
            rec("复制按钮定位",False)
    except Exception as e:
        rec("复制卡密到剪贴板",False,str(e))

    # 5) 编辑卡密
    try:
        edits_btn=[d for d in main.descendants() if (d.window_text() or "")=="编辑" and d.element_info.control_type=="Button"]
        if edits_btn:
            edits_btn[0].click_input(); time.sleep(1.5)
            ed=None
            for d in main.descendants():
                try:
                    if d.element_info.control_type=="Window" and "编辑卡密" in "|".join(txts(d,20)):
                        ed=d; break
                except Exception: pass
            if ed is None:
                for w in Desktop(backend="uia").windows():
                    if "编辑卡密" in w.window_text(): ed=w; break
            rec("编辑窗口打开",ed is not None)
            if ed:
                shot(ed,"ui_03_edit_dialog")
                # 改备注并保存
                ede=[d for d in ed.descendants() if d.element_info.control_type=="Edit"]
                if len(ede)>=3: ede[-1].set_edit_text("UI-edited")
                click(ed,"保存",["Button"]); time.sleep(1.5)
                out=db("select note from cards where note='UI-edited'")
                rec("编辑保存到数据库", "UI-edited" in out, out.strip())
        else: rec("编辑按钮定位",False)
    except Exception as e: rec("编辑卡密",False,str(e))

    # 6) 删除卡密
    try:
        dels=[d for d in main.descendants() if (d.window_text() or "")=="删除" and d.element_info.control_type=="Button"]
        if dels:
            dels[0].click_input(); time.sleep(1)
            # 确认弹窗
            dlg=None
            # 先从 main 后代找确认弹窗（可能是子Window或含确认文本的元素）
            for d in main.descendants():
                try:
                    tt="|".join(txts(d,30))
                    if d.element_info.control_type=="Window" and ("确认删除" in tt or "删除后不可恢复" in tt):
                        dlg=d; break
                except Exception: pass
            if dlg is None:
                for w in Desktop(backend="uia").windows():
                    tt="|".join(txts(w,50))
                    if "确认删除" in tt or "删除后不可恢复" in tt: dlg=w; break
            if dlg:
                ok=click(dlg,"是",["Button"]) or click(dlg,"Yes",["Button"]) or click(dlg,"确定",["Button"])
                print("  delete confirm clicked:",ok,flush=True)
            else:
                import pywinauto.keyboard as kb
                kb.send_keys("{ENTER}")
            time.sleep(2)
            out=db("select count(*) from cards where note like 'UI-%'")
            cnt=int(out.strip().splitlines()[-1]) if out.strip().splitlines()[-1].strip().isdigit() else -1
            rec("删除卡密生效", cnt==1, "remain=%d"%cnt)
        else: rec("删除按钮定位",False)
    except Exception as e: rec("删除卡密",False,str(e))

    # 7) 二级窗关闭按钮：源码验证 + 尝试打开销量走势
    src_dir=os.path.join(BASE,"src","MaoDuoDuo.Views")
    for f in ["DownWindow.xaml","GoodSalesShow.xaml"]:
        s=open(os.path.join(src_dir,f),encoding="utf-8").read()
        rec("%s 有关闭按钮"%f, "win_close.png" in s and "CloseWindow_Click" in s, "")
    kill()
    print("=== DONE ===",flush=True)

if __name__=="__main__":
    try: main()
    except Exception as e:
        print("FATAL:",e,flush=True); results.append("[FAIL] FATAL | "+str(e)); kill()
    open(os.path.join(BASE,"test_ui_enhance_out.txt"),"w",encoding="utf-8").write("\n".join(results)+"\n")
