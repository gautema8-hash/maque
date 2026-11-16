# -*- coding: utf-8 -*-
"""登录窗最终验证：无头像 + Topmost 置顶 + 剪贴板登录"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')
from pywinauto import Desktop
from pywinauto.keyboard import send_keys
from PIL import ImageGrab
import ctypes as _ct

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'
R = []
def rec(n, ok, note=''):
    R.append((n, ok, note)); print('[%s] %s %s' % ('PASS' if ok else 'FAIL', n, note), flush=True)
def shot(n):
    try: ImageGrab.grab().save(os.path.join(OUT, n + '.png')); print('[shot]', n, flush=True)
    except Exception as e: print('shot fail', repr(e), flush=True)

_k = _ct.windll.kernel32; _u = _ct.windll.user32
_k.GlobalAlloc.restype = _ct.c_void_p; _k.GlobalAlloc.argtypes = [_ct.c_uint, _ct.c_size_t]
_k.GlobalLock.restype = _ct.c_void_p; _k.GlobalLock.argtypes = [_ct.c_void_p]
_k.GlobalUnlock.argtypes = [_ct.c_void_p]
_u.OpenClipboard.argtypes = [_ct.c_void_p]
_u.SetClipboardData.restype = _ct.c_void_p; _u.SetClipboardData.argtypes = [_ct.c_uint, _ct.c_void_p]

def set_clipboard(text):
    if not _u.OpenClipboard(None): raise RuntimeError('clipboard open failed')
    try:
        _u.EmptyClipboard()
        buf = _ct.create_unicode_buffer(text)
        h = _k.GlobalAlloc(2, (len(buf) + 1) * 2)
        p = _k.GlobalLock(h)
        _ct.memmove(p, buf, (len(buf) + 1) * 2)
        _k.GlobalUnlock(h)
        _u.SetClipboardData(13, h)
    finally:
        _u.CloseClipboard()

def find_by_ai(win, ai, timeout=15):
    t0 = time.time()
    while time.time() - t0 < timeout:
        for el in win.descendants():
            try:
                if el.element_info.automation_id == ai: return el
            except Exception: pass
        time.sleep(1)
    raise RuntimeError('element not found: ' + ai)

def paste_text(el, text):
    # IconTextBox/ZPasswordBox 本身即 Edit 控件（UIA），直接操作
    el.click_input(); time.sleep(0.3)
    send_keys('^a{DEL}'); time.sleep(0.2)
    set_clipboard(text)
    send_keys('^v', pause=0.15); time.sleep(0.3)

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe')], cwd=DIST)
notepad = subprocess.Popen(['notepad.exe'])
time.sleep(2)
desktop = Desktop(backend='uia')
login = None
t0 = time.time()
while time.time() - t0 < 60:
    try:
        login = desktop.window(title_re=r'麻雀一键铺货 - 登录')
        login.set_focus(); time.sleep(2.5); break
    except Exception:
        time.sleep(1.2)
assert login

# 1) Topmost 置顶（记事本存在的情况下登录窗不被遮挡）
GWL_EXSTYLE = -20
ex = _u.GetWindowLongW(login.element_info.handle, GWL_EXSTYLE)
topmost = (ex & 0x8) != 0
rec('登录窗 Topmost 置顶(记事本遮挡场景)', topmost, 'exstyle bit8=%s' % topmost)
shot('74_login_topmost')

# 2) 头像已移除（>24px 的 Image 应为 0，仅剩 18px 关闭小图标）
big_imgs = []
for el in login.descendants(control_type='Image'):
    try:
        if el.rectangle().width() > 24: big_imgs.append(el)
    except Exception: pass
rec('登录页头像已移除', len(big_imgs) == 0, '大图Image=%d' % len(big_imgs))

# 3) 正确账号登录（剪贴板粘贴）
acc = find_by_ai(login, 'textAccount')
paste_text(acc, 'admin')
pwd = find_by_ai(login, 'textPwd')
paste_text(pwd, '123456')
login_btn = None
for b in login.descendants(control_type='Button'):
    if '登' in (b.window_text() or ''): login_btn = b; break
login_btn.click_input()
time.sleep(6)
main = None
try:
    main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
    main.set_focus(); time.sleep(2)
except Exception: pass
rec('剪贴板登录进入主窗', main is not None and proc.poll() is None)
shot('75_main_after_login')

rec('应用全程存活', proc.poll() is None, '')
if proc.poll() is None:
    try: main.close()
    except Exception: pass
    time.sleep(2)
    if proc.poll() is None: proc.terminate()
try: notepad.terminate()
except Exception: pass

print('RESULT: %d/%d' % (sum(1 for _, ok, _ in R if ok), len(R)), flush=True)
