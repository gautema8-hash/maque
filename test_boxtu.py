# -*- coding: utf-8 -*-
"""运行时检查 box_tu 下拉框状态"""
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
try:
    c = main.child_window(auto_id='box_tu')
    r = c.rectangle()
    print('box_tu rect:', r.left, r.top, r.right, r.bottom, 'visible=', c.visible, 'enabled=', c.enabled, flush=True)
    print('children:', [ch.element_info.control_type for ch in c.children()], flush=True)
    texts = [ch.window_text() for ch in c.descendants()]
    print('descendant texts:', texts[:10], flush=True)
except Exception as e:
    print('box_tu error:', repr(e), flush=True)
try:
    c2 = main.child_window(auto_id='box_alter')
    r2 = c2.rectangle()
    print('box_alter rect:', r2.left, r2.top, r2.right, r2.bottom, 'visible=', c2.visible, flush=True)
except Exception as e:
    print('box_alter error:', repr(e), flush=True)
if proc.poll() is None:
    try:
        main.close()
    except Exception:
        pass
    time.sleep(2)
    if proc.poll() is None:
        proc.terminate()
print('DONE', flush=True)
