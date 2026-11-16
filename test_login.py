# -*- coding: utf-8 -*-
"""登录系统专项测试：新UI + MySQL账号验证（错误密码/账号不存在/正确登录）"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')
from pywinauto import Desktop
from pywinauto.keyboard import send_keys
from PIL import ImageGrab
import pymysql

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'
R = []
def rec(n, ok, note=''):
    R.append((n, ok, note)); print('[%s] %s %s' % ('PASS' if ok else 'FAIL', n, note), flush=True)
def shot(n):
    try: ImageGrab.grab().save(os.path.join(OUT, n + '.png')); print('[shot]', n, flush=True)
    except Exception as e: print('shot fail', repr(e), flush=True)
SYSTEM_PAT = re.compile(r'麻雀一键铺货|任务栏|Program Manager|WorkBuddy|Chrome|Edge|Microsoft|文件资源管理器|拼多多|豆包|输入体验|Agent|Typora|设置')
POPUP_PAT = re.compile(r'提示|注意|警告')
_u32 = __import__('ctypes').windll.user32
_PROC = __import__('ctypes').WINFUNCTYPE(__import__('ctypes').c_int, __import__('ctypes').c_void_p, __import__('ctypes').c_void_p)
def enum_top():
    out = []
    @_PROC
    def cb(hwnd, _):
        if _u32.IsWindowVisible(hwnd):
            n = _u32.GetWindowTextLengthW(hwnd)
            buf = __import__('ctypes').create_unicode_buffer(n + 1)
            _u32.GetWindowTextW(hwnd, buf, n + 1)
            out.append((int(hwnd), buf.value))
        return True
    _u32.EnumWindows(cb, 0)
    return out
def close_popups(max_wait=5):
    t0 = time.time()
    while time.time() - t0 < max_wait:
        for hwnd, t in enum_top():
            if t and POPUP_PAT.search(t):
                try:
                    w = Desktop(backend='uia').window(handle=hwnd)
                    for b in w.descendants(control_type='Button'):
                        if '确定' in (b.window_text() or ''): b.click_input(); break
                    time.sleep(1); return t
                except Exception: pass
        time.sleep(0.6)
    return None

def find_by_ai(win, ai, timeout=20):
    t0 = time.time()
    while time.time() - t0 < timeout:
        for el in win.descendants():
            try:
                if el.element_info.automation_id == ai:
                    return el
            except Exception:
                pass
        time.sleep(1)
    raise RuntimeError('element not found: ' + ai)

def set_text(edit_el, text):
    edit_el.click_input(); time.sleep(0.3)
    send_keys('^a{DEL}'); time.sleep(0.2)
    send_keys(text, with_spaces=True); time.sleep(0.3)

def read_err(win):
    try:
        t = win.child_window(auto_id='errTipText')
        return t.element_info.name or ''
    except Exception:
        return ''

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe')], cwd=DIST)
desktop = Desktop(backend='uia')
login = None
t0 = time.time()
while time.time() - t0 < 60:
    try:
        login = desktop.window(title_re=r'麻雀一键铺货 - 登录')
        login.set_focus(); time.sleep(2); break
    except Exception:
        time.sleep(1.2)
assert login, 'login window timeout'
rec('新登录窗口显示', True)
shot('70_login_new_ui')

acc_e = find_by_ai(login, 'textAccount')
pwd_e = find_by_ai(login, 'textPwd')
login_btn = None
t0 = time.time()
while time.time() - t0 < 15 and login_btn is None:
    for b in login.descendants(control_type='Button'):
        if '登' in (b.window_text() or ''): login_btn = b; break
    if login_btn is None: time.sleep(1)
assert login_btn, 'login button not found'

# 1) 错误密码
set_text(acc_e, 'admin')
set_text(pwd_e, 'wrong_password')
login_btn.click_input()
time.sleep(2)
err = read_err(login)
rec('错误密码-窗内提示(密码错误)', '密码错误' in err, 'err=%r' % err)
shot('71_login_wrong_pwd')
rec('错误密码-无弹窗打扰', close_popups() is None, '')

# 2) 账号不存在
set_text(acc_e, 'nosuchuser999')
set_text(pwd_e, 'whatever123')
login_btn.click_input()
time.sleep(2)
err = read_err(login)
rec('账号不存在-窗内提示', '账号不存在' in err, 'err=%r' % err)
shot('72_login_no_such_user')

# 3) 正确账号密码
set_text(acc_e, 'admin')
set_text(pwd_e, '123456')
login_btn.click_input()
time.sleep(6)
main = None
try:
    main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
    main.set_focus(); time.sleep(2)
except Exception: pass
rec('正确账号密码登录进入主窗', main is not None and proc.poll() is None)
shot('73_login_success_main')

conn = pymysql.connect(host='127.0.0.1', port=3306, user='root', password='root123!@#', database='maque', charset='utf8mb4')
cur = conn.cursor(); cur.execute("SELECT username, role FROM users"); rows = cur.fetchall(); conn.close()
rec('MySQL users 表(默认管理员存在)', any(r[0] == 'admin' for r in rows), 'rows=%r' % rows)

rec('应用全程存活', proc.poll() is None, '')
if proc.poll() is None:
    try: main.close()
    except Exception: pass
    time.sleep(2)
    if proc.poll() is None: proc.terminate()

print('\n===== SUMMARY =====', flush=True)
np = sum(1 for _, ok, _ in R if ok)
for name, ok, note in R:
    print('%-4s | %-34s | %s' % ('PASS' if ok else 'FAIL', name, note), flush=True)
print('RESULT: %d/%d' % (np, len(R)), flush=True)
