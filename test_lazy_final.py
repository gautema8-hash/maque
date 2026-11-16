# -*- coding: utf-8 -*-
"""懒加载确认后的最终交互验证：视频采集弹窗 + 工具箱卡片 + 搜索跳转"""
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

SYSTEM_PAT = re.compile(r'麻雀一键铺货|任务栏|Program Manager|WorkBuddy|Chrome|Edge|Microsoft|文件资源管理器|拼多多|豆包|输入体验|Agent')

def close_popups():
    closed = None
    for _ in range(3):
        found = False
        for w in Desktop(backend='uia').windows():
            t = w.window_text() or ''
            if not SYSTEM_PAT.search(t):
                try:
                    btns = w.descendants(control_type='Button')
                    if btns:
                        btns[0].click_input(); closed = t or '(无标题)'; found = True
                    else:
                        w.close(); closed = t or '(无标题)'; found = True
                    time.sleep(1.2); break
                except Exception:
                    pass
        if not found: break
    return closed

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

# 注意：autotest 会自动执行一次搜索并跳转买手页（已验证），先关闭可能的登录页弹窗
close_popups()

# 1) 搜索自动跳转：当前 tab 应为买手页
try:
    sel = main.descendants(control_type='TabItem')
    cur = [t.window_text() for t in sel if t.automation_id and 'IsSelected' in str(t.element_info)]
except Exception:
    pass
# 通过地址栏验证
url = ''
try:
    tb = main.child_window(auto_id='txtBrowser')
    for _ in range(3):
        try:
            ed = tb.descendants(control_type='Edit')
            if ed: url = ed[0].element_info.name or ''; break
        except Exception: time.sleep(2)
except Exception: pass
rec('自动搜索跳转买手页(URL含search_key=snacks)', 'search_key%3Dsnacks' in url or 'search_key=snacks' in url, 'url=%r' % url[:120])
shot('45_autosearch_buyer')

# 2) 视频采集：切 tab 等实例化 → 空关键词 → 弹窗
main.child_window(title='视频采集', control_type='TabItem').click_input()
time.sleep(3)
try:
    main.child_window(title='开始采集', control_type='Button').click_input()
    time.sleep(1.5)
    p1 = close_popups()
    rec('视频采集-空关键词提示', p1 is not None, 'popup=%r' % p1)
except Exception as e:
    rec('视频采集-空关键词提示', False, repr(e))
    close_popups()

# 3) 工具箱：盈亏计算卡片 → 弹窗
main.child_window(title='工具箱', control_type='TabItem').click_input()
time.sleep(3)
try:
    main.child_window(title='盈亏计算', control_type='Text').click_input()
    time.sleep(2)
    titles = [w.window_text() for w in desktop.windows()]
    ok = any(re.search(r'^盈亏计算$', t or '') for t in titles)
    rec('工具箱-盈亏计算卡片弹窗', ok, '')
    shot('46_toolbox_compute')
except Exception as e:
    rec('工具箱-盈亏计算卡片弹窗', False, repr(e))
    close_popups()
try:
    main.child_window(title=re.compile(r'^盈亏计算$'), found_index=0).close()
except Exception:
    pass
close_popups()

rec('应用全程存活', proc.poll() is None, '')
if proc.poll() is None:
    try: main.close()
    except Exception: pass
    time.sleep(2)
    if proc.poll() is None: proc.terminate()

print('RESULT: %d/%d' % (sum(1 for _, ok, _ in R if ok), len(R)), flush=True)
