# -*- coding: utf-8 -*-
"""弹窗专项重测：ROI计算器 / 裂变图设置 / 评价下载"""
import io, os, sys, time, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop
from pywinauto.keyboard import send_keys
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'

def log(m): print('[TEST]', m, flush=True)
def shot(name):
    try:
        ImageGrab.grab().save(os.path.join(OUT, name + '.png'))
        log('shot ' + name)
    except Exception as e:
        log('shot fail %s %r' % (name, e))

def all_titles():
    return [w.window_text() for w in Desktop(backend='uia').windows()]

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
log('pid=%d' % proc.pid)
desktop = Desktop(backend='uia')

def wait_main(timeout=90):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            w = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
            w.set_focus()
            time.sleep(2)
            return w
        except Exception:
            time.sleep(1.5)
    raise RuntimeError('main window timeout')

main = wait_main()
log('main window ok')
shot('03b_main_top')

def click_and_check(btn_title, dlg_title_re, shotname):
    try:
        btn = main.child_window(title=btn_title, control_type='Button')
        btn.wait('visible', 10)
        btn.click_input()
    except Exception as e:
        log('btn %s click fail %r' % (btn_title, e))
        return False
    time.sleep(2.5)
    titles = all_titles()
    log('windows after click: %r' % [t for t in titles if t])
    found = [t for t in titles if t and __import__('re').search(dlg_title_re, t)]
    shot(shotname)
    if found:
        dlg = desktop.window(title=found[0])
        try:
            dlg.set_focus(); time.sleep(1); shot(shotname + '_focused')
        except Exception:
            pass
        dlg.close()
        time.sleep(1.5)
        log('DIALOG OK: %s' % found[0])
        return True
    log('DIALOG NOT FOUND for %s' % btn_title)
    return False

r1 = click_and_check('ROI计算器', r'ROI', '16_roi')
r2 = click_and_check('裂变图设置', r'申诉理由', '17_lyset')
r3 = click_and_check('商品信息', r'注意', '18_info_warn')

if proc.poll() is None:
    print('ALIVE at end', flush=True)
    try:
        main.close()
    except Exception:
        pass
    time.sleep(2)
    if proc.poll() is None:
        proc.terminate()

print('SUMMARY: ROI=%s LYSET=%s INFOGUARD=%s' % (r1, r2, r3), flush=True)
