# -*- coding: utf-8 -*-
"""麻雀一键铺货 真机端到端测试（单进程内：启动→UI自动化→截图→关闭）"""
import io, os, sys, time, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop, Application
from pywinauto.keyboard import send_keys
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'
os.makedirs(OUT, exist_ok=True)

results = []
def log(msg):
    print('[TEST]', msg, flush=True)

def shot(name):
    try:
        ImageGrab.grab().save(os.path.join(OUT, name + '.png'))
        log('screenshot: ' + name)
    except Exception as e:
        log('screenshot failed %s: %s' % (name, e))

def step(name, fn):
    try:
        fn()
        results.append(('PASS', name))
        log('PASS: ' + name)
        return True
    except Exception as e:
        results.append(('FAIL', name + ' -> ' + repr(e)[:200]))
        log('FAIL: %s -> %r' % (name, e))
        shot('FAIL_' + name.replace(' ', '_'))
        return False

def alive():
    return proc.poll() is None

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe')], cwd=DIST)
log('app pid=%d' % proc.pid)
desktop = Desktop(backend='uia')

def wait_window(title_re, timeout=40):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            w = desktop.window(title_re=title_re)
            w.set_focus()
            return w
        except Exception:
            time.sleep(1)
    raise RuntimeError('window not found: ' + title_re)

# ===== 1. 登录窗口 =====
def t_login_window():
    global login_win
    login_win = wait_window(r'麻雀一键铺货 - 登录', 40)
    time.sleep(1)
    shot('01_login_window')
step('登录窗口显示', t_login_window)

def t_dump():
    with open(os.path.join(OUT, 'login_dump.txt'), 'w', encoding='utf-8') as f:
        login_win.print_control_identifiers(filename=f)
step('控件树导出', t_dump)

# 找输入框与登录按钮（多策略）
def find_edit(win, auto_id):
    try:
        return win.child_window(auto_id=auto_id, control_type='Edit')
    except Exception:
        pass
    # 自定义控件：找子 Edit
    try:
        c = win.child_window(auto_id=auto_id)
        for e in c.descendants(control_type='Edit'):
            return e
    except Exception:
        pass
    raise RuntimeError('edit not found: ' + auto_id)

def t_fill_login():
    acc = find_edit(login_win, 'textAccount')
    acc.click_input()
    time.sleep(0.3)
    send_keys('^a{DEL}')
    send_keys('test123')
    time.sleep(0.3)
    pwd = find_edit(login_win, 'textPwd')
    pwd.click_input()
    time.sleep(0.3)
    send_keys('^a{DEL}')
    send_keys('abc654321')
    time.sleep(0.5)
    shot('02_login_filled')
step('输入账号密码', t_fill_login)

def t_click_login():
    try:
        btn = login_win.child_window(auto_id='btnLogin')
        btn.click_input()
    except Exception:
        login_win.child_window(title='登 录', control_type='Button').click_input()
    time.sleep(3)
step('点击登录', t_click_login)

# ===== 2. 主窗口 =====
def t_main_window():
    global main_win
    main_win = wait_window(r'^麻雀一键铺货$', 60)
    time.sleep(4)
    shot('03_main_window')
step('主窗口加载', t_main_window)

TAB_NAMES = ['任务设置', '前端采集页', '商家发布页', '售后服务', '首页', '店铺管理', '商品监控', '视频采集', '工具箱']
tab_index = {'任务设置': '04', '前端采集页': '05', '商家发布页': '06', '售后服务': '07',
             '首页': '08', '店铺管理': '09', '商品监控': '10', '视频采集': '11', '工具箱': '12'}

def t_tabs():
    for name in TAB_NAMES:
        if not alive():
            raise RuntimeError('app exited during tab: ' + name)
        tab = main_win.child_window(title=name, control_type='TabItem')
        tab.click_input()
        time.sleep(3)
        shot(tab_index[name] + '_tab_' + name)
        log('tab ok: ' + name)
step('九个标签页遍历', t_tabs)

def t_roi_dialog():
    main_win.child_window(title='任务设置', control_type='TabItem').click_input()
    time.sleep(1.5)
    main_win.child_window(title='ROI计算器', control_type='Button').click_input()
    time.sleep(2)
    dlg = wait_window(r'ROI 计算器', 15)
    shot('13_roi_dialog')
    dlg.close()
    time.sleep(1.5)
step('ROI计算器弹窗', t_roi_dialog)

def t_lyset_dialog():
    main_win.child_window(title='裂变图设置', control_type='Button').click_input()
    time.sleep(2)
    dlg = wait_window(r'申诉理由设置', 15)
    shot('14_lyset_dialog')
    dlg.close()
    time.sleep(1.5)
step('裂变图设置弹窗', t_lyset_dialog)

def t_product_info_guard():
    main_win.child_window(title='前端采集页', control_type='TabItem').click_input()
    time.sleep(2)
    main_win.child_window(title='商品信息', control_type='Button').click_input()
    time.sleep(1.5)
    shot('15_product_info_warn')
    # 关闭警告弹窗（找最新顶层窗口）
    for w in desktop.windows():
        t = w.window_text()
        if '注意' in t or '提示' in t:
            try:
                w.child_window(control_type='Button').click_input()
            except Exception:
                w.close()
            break
    time.sleep(1)
step('商品信息空数据保护', t_product_info_guard)

# ===== 收尾 =====
if alive():
    try:
        main_win.close()
    except Exception:
        pass
    time.sleep(2)
    if alive():
        proc.terminate()

print('\n===== SUMMARY =====', flush=True)
for st, name in results:
    print(st, '|', name, flush=True)
print('app final exit code:', proc.poll(), flush=True)
passed = sum(1 for s, _ in results if s == 'PASS')
print('RESULT: %d/%d passed' % (passed, len(results)), flush=True)
