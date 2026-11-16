# -*- coding: utf-8 -*-
"""弹窗重测 3：滚动到设置区底部后点击 ROI计算器 / 加速补流量"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop, mouse
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'

def log(m): print('[TEST]', m, flush=True)
def shot(name):
    try:
        ImageGrab.grab().save(os.path.join(OUT, name + '.png'))
        log('shot ' + name)
    except Exception as e:
        log('shot fail %r' % e)

def top_windows():
    return [w.window_text() for w in desktop.windows() if w.window_text()]

def close_top(title_re):
    for w in desktop.windows():
        if w.window_text() and re.search(title_re, w.window_text()):
            try:
                w.close(); time.sleep(1.2); return True
            except Exception:
                pass
    return False

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
log('pid=%d' % proc.pid)

def wait_main(timeout=90):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            w = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
            w.set_focus(); time.sleep(2)
            return w
        except Exception:
            time.sleep(1.5)
    raise RuntimeError('main timeout')

main = wait_main()
main.child_window(title='任务设置', control_type='TabItem').click_input()
time.sleep(2)

# 在左侧设置面板区域滚动鼠标滚轮到页底
for _ in range(3):
    mouse.scroll(coords=(900, 1300), wheel_dist=-5)
    time.sleep(0.8)
shot('20_scrolled_bottom')

def test_dialog(btn_title, dlg_re, shotname, tries=3):
    for i in range(tries):
        try:
            btn = main.child_window(title=btn_title, control_type='Button')
            rect = btn.rectangle()
            log('%s rect=%s try=%d' % (btn_title, (rect.left, rect.top, rect.right, rect.bottom), i + 1))
            btn.click_input()
        except Exception as e:
            log('click fail %r' % e)
            time.sleep(1)
            continue
        time.sleep(2.5)
        titles = top_windows()
        hit = [t for t in titles if re.search(dlg_re, t)]
        log('hit: %r' % hit)
        if hit:
            shot(shotname)
            try:
                d = desktop.window(title=hit[0]); d.set_focus(); time.sleep(0.8); shot(shotname + '_focus')
            except Exception:
                pass
            close_top(dlg_re)
            log('DIALOG OK %s -> %s' % (btn_title, hit[0]))
            return True
        shot(shotname + '_noopen')
    return False

r1 = test_dialog('ROI计算器', r'ROI', '16_roi')
r2 = test_dialog('加速/补流量', r'申诉理由', '18_lyset')

print('ALIVE:', proc.poll() is None, flush=True)
if proc.poll() is None:
    try:
        main.close()
    except Exception:
        pass
    time.sleep(2)
    if proc.poll() is None:
        proc.terminate()
print('SUMMARY: ROI=%s LYSET=%s' % (r1, r2), flush=True)
