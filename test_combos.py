# -*- coding: utf-8 -*-
"""枚举主窗口全部 ComboBox 的 AutomationId 与位置"""
import io, os, sys, time, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
from pywinauto import Desktop

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
t0 = time.time()
main = None
while time.time() - t0 < 90:
    try:
        main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
        main.set_focus(); time.sleep(2); break
    except Exception:
        time.sleep(1.5)
found = 0
for c in main.descendants(control_type='ComboBox'):
    try:
        ai = c.element_info.automation_id
        r = c.rectangle()
        print('%-14s rect=(%d,%d,%d,%d) w=%d' % (ai, r.left, r.top, r.right, r.bottom, r.width()), flush=True)
        found += 1
    except Exception as e:
        print('err', repr(e), flush=True)
print('total combos:', found, flush=True)
if proc.poll() is None:
    try:
        main.close()
    except Exception:
        pass
    time.sleep(2)
    if proc.poll() is None:
        proc.terminate()
print('DONE', flush=True)
