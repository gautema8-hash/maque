# -*- coding: utf-8 -*-
"""文案还原验证：启动 → 登录 → 主窗口任务设置页滚动截图"""
import io, os, sys, time, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop, mouse
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'

def log(m): print('[TEST]', m, flush=True)
def shot(name):
    ImageGrab.grab().save(os.path.join(OUT, name + '.png'))
    log('shot ' + name)

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
log('pid=%d' % proc.pid)

t0 = time.time()
main = None
while time.time() - t0 < 90:
    try:
        main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
        main.set_focus()
        time.sleep(2)
        break
    except Exception:
        time.sleep(1.5)
assert main, 'main window timeout'
log('main ok')

# 任务设置页顶部
shot('21_labels_top')
# 滚动到中部
mouse.scroll(coords=(900, 1000), wheel_dist=-5)
time.sleep(1)
shot('22_labels_mid')
mouse.scroll(coords=(900, 1000), wheel_dist=-5)
time.sleep(1)
shot('23_labels_bottom')
mouse.scroll(coords=(900, 1000), wheel_dist=-5)
time.sleep(1)
shot('24_labels_end')

print('ALIVE:', proc.poll() is None, flush=True)
if proc.poll() is None:
    try:
        main.close()
    except Exception:
        pass
    time.sleep(2)
    if proc.poll() is None:
        proc.terminate()
print('DONE', flush=True)
