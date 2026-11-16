# -*- coding: utf-8 -*-
"""调试：弹窗出现时 EnumWindows 到底看到什么"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
import ctypes
from pywinauto import Desktop

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
_u32 = ctypes.windll.user32
_WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_int, ctypes.c_void_p, ctypes.c_void_p)

def enum_toplevel():
    out = []
    @_WNDENUMPROC
    def cb(hwnd, _):
        if _u32.IsWindowVisible(hwnd):
            n = _u32.GetWindowTextLengthW(hwnd)
            buf = ctypes.create_unicode_buffer(n + 1)
            _u32.GetWindowTextW(hwnd, buf, n + 1)
            out.append((int(hwnd), buf.value))
        return True
    _u32.EnumWindows(cb, 0)
    return out

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
t0 = time.time()
main = None
while time.time() - t0 < 90:
    try:
        main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
        main.set_focus(); time.sleep(3); break
    except Exception:
        time.sleep(1.5)

main.child_window(title='店铺管理', control_type='TabItem').click_input()
time.sleep(3.5)
print('--- before click ---', flush=True)
for h, t in enum_toplevel():
    if t: print('  %8d %r' % (h, t), flush=True)

btns = [b for b in main.descendants(control_type='Button') if (b.window_text() or '') == '全选']
print('全选 button found:', len(btns), flush=True)
if btns: btns[0].click_input()
time.sleep(2)

print('--- after click ---', flush=True)
for h, t in enum_toplevel():
    print('  %8d %r visible' % (h, t), flush=True)

# 尝试把每个非主窗口包成 UIA 对象读正文
for h, t in enum_toplevel():
    if t in ('麻雀一键铺货',) or 'WorkBuddy' in t or 'Program Manager' in t or '任务栏' in t:
        continue
    try:
        w = desktop.window(handle=h)
        txts = [x.element_info.name for x in w.descendants(control_type='Text')]
        btns2 = [b.window_text() for b in w.descendants(control_type='Button')]
        print('WINDOW %d %r texts=%r buttons=%r' % (h, t, txts[:8], btns2[:4]), flush=True)
    except Exception as e:
        print('WINDOW %d %r WRAP FAIL %r' % (h, t, e), flush=True)

if proc.poll() is None:
    proc.terminate()
print('DONE', flush=True)
