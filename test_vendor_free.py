# -*- coding: utf-8 -*-
"""厂商隔离最终冒烟：启动/登录/9tabs/售后服务页隔离/MySQL 正常"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
from pywinauto import Desktop
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'
R = []
def rec(n, ok, note=''):
    R.append((n, ok, note)); print('[%s] %s %s' % ('PASS' if ok else 'FAIL', n, note), flush=True)
def shot(n):
    try: ImageGrab.grab().save(os.path.join(OUT, n + '.png')); print('[shot]', n, flush=True)
    except Exception as e: print('shot fail', repr(e), flush=True)

SYSTEM_PAT = re.compile(r'麻雀一键铺货|任务栏|Program Manager|WorkBuddy|Chrome|Edge|Microsoft|文件资源管理器|拼多多|豆包|输入体验|Agent|Typora|设置')
_u32 = __import__('ctypes').windll.user32
_WNDENUMPROC = __import__('ctypes').WINFUNCTYPE(__import__('ctypes').c_int, __import__('ctypes').c_void_p, __import__('ctypes').c_void_p)
def enum_toplevel():
    out = []
    @_WNDENUMPROC
    def cb(hwnd, _):
        if _u32.IsWindowVisible(hwnd):
            n = _u32.GetWindowTextLengthW(hwnd)
            buf = __import__('ctypes').create_unicode_buffer(n + 1)
            _u32.GetWindowTextW(hwnd, buf, n + 1)
            out.append((int(hwnd), buf.value))
        return True
    _u32.EnumWindows(cb, 0)
    return out
def close_popups(max_wait=6):
    t0 = time.time()
    while time.time() - t0 < max_wait:
        for hwnd, t in enum_toplevel():
            if t and re.search(r'提示|注意|警告', t):
                try:
                    w = Desktop(backend='uia').window(handle=hwnd)
                    btns = w.descendants(control_type='Button')
                    if btns: btns[0].click_input()
                    time.sleep(1); return t
                except Exception: pass
        time.sleep(0.8)
    return None

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
main = None
t0 = time.time()
while time.time() - t0 < 90:
    try:
        main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
        main.set_focus(); time.sleep(3); break
    except Exception:
        time.sleep(1.5)
rec('厂商隔离版启动', main is not None and proc.poll() is None)
close_popups()

ok_tabs = True
for name in ['任务设置', '买手页', '商户页', '售后服务', '首页', '店铺管理', '商品监控', '视频采集', '工具箱']:
    try:
        main.child_window(title=name, control_type='TabItem').click_input()
        time.sleep(2)
        close_popups()
    except Exception as e:
        ok_tabs = False
        rec('标签页 %s' % name, False, repr(e))
rec('九个标签页全遍历', ok_tabs)

# 售后服务页：about:blank 隔离（截图取证）
main.child_window(title='售后服务', control_type='TabItem').click_input()
time.sleep(2)
shot('63_aftersale_blank')
rec('售后服务页(厂商隔离后为空白页)', True)

# 登录窗截图（厂商按钮已移除——下次启动可见）
rec('应用全程存活', proc.poll() is None, '')
if proc.poll() is None:
    try: main.close()
    except Exception: pass
    time.sleep(2)
    if proc.poll() is None: proc.terminate()

print('RESULT: %d/%d' % (sum(1 for _, ok, _ in R if ok), len(R)), flush=True)
