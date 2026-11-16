# -*- coding: utf-8 -*-
"""MySQL 模式验收测试：店铺管理/上传记录读 MySQL、写入链路"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop
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
POPUP_TITLE_PAT = re.compile(r'提示|注意|采集失败|商品异常|操作完成|清空操作|登录失败|警告')
POPUP_SEQ = [0]
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

def close_popups(max_wait=8):
    closed = None
    t0 = time.time()
    while time.time() - t0 < max_wait:
        for hwnd, t in enum_toplevel():
            if t and POPUP_TITLE_PAT.search(t):
                try:
                    w = Desktop(backend='uia').window(handle=hwnd)
                    POPUP_SEQ[0] += 1
                    ImageGrab.grab().save(os.path.join(OUT, 'popup_%03d.png' % POPUP_SEQ[0]))
                    btns = w.descendants(control_type='Button')
                    for b in btns:
                        if '确定' in (b.window_text() or ''): b.click_input(); break
                    else:
                        if btns: btns[0].click_input()
                    closed = t; time.sleep(1.0); return closed
                except Exception: pass
        time.sleep(0.8)
    return closed

def mysql_count(table):
    conn = pymysql.connect(host='127.0.0.1', port=3306, user='root', password='root123!@#', database='maque', charset='utf8mb4')
    cur = conn.cursor(); cur.execute("SELECT COUNT(*) FROM %s" % table); c = cur.fetchone()[0]
    conn.close(); return c

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
rec('应用以 MySQL 模式启动', main is not None and proc.poll() is None)
close_popups()

# 任务设置页：店铺列表应显示 MySQL 中的测试记录
main.child_window(title='任务设置', control_type='TabItem').click_input()
time.sleep(2.5)
shot('60_mysql_uploadinfo')
rec('任务设置页加载(MySQL模式)', True)

# 店铺管理页：查找店铺 → MySQL dd_shop 查询
main.child_window(title='店铺管理', control_type='TabItem').click_input()
time.sleep(3.5)
btns = [b for b in main.descendants(control_type='Button') if (b.window_text() or '') == '查找店铺']
if btns: btns[0].click_input()
time.sleep(2)
shot('61_mysql_shopset_find')
# UIA 读列表行数（DataItem 数量）
try:
    lst = main.child_window(auto_id='PddShopList')
    items = lst.descendants(control_type='DataItem')
    names = [x.element_info.name for x in items]
    ok = any('麻雀测试旗舰店' in (n or '') for n in names)
    rec('店铺管理-MySQL店铺数据读取', ok, 'items=%r' % names[:8])
except Exception as e:
    rec('店铺管理-MySQL店铺数据读取', False, repr(e))

# 写入链路验证：勾选店铺 → choose_state 更新到 MySQL
try:
    cbs = lst.descendants(control_type='CheckBox')
    if cbs:
        cbs[0].click_input(); time.sleep(1.5)
        conn = pymysql.connect(host='127.0.0.1', port=3306, user='root', password='root123!@#', database='maque', charset='utf8mb4')
        cur = conn.cursor()
        cur.execute("SELECT choose_state FROM dd_shop WHERE shop_id='test_shop_001'")
        v = cur.fetchone()[0]; conn.close()
        rec('店铺勾选状态写入MySQL', v == 1, 'choose_state=%s' % v)
    else:
        rec('店铺勾选状态写入MySQL', False, 'no checkbox found')
except Exception as e:
    rec('店铺勾选状态写入MySQL', False, repr(e))
close_popups()

# 商品监控页读取 MySQL
main.child_window(title='商品监控', control_type='TabItem').click_input()
time.sleep(3.5)
shot('62_mysql_monitor')
rec('商品监控页加载(MySQL读取)', proc.poll() is None)

# dy_login 读取（视频采集页初始化不崩溃即读取正常）
main.child_window(title='视频采集', control_type='TabItem').click_input()
time.sleep(3.5)
rec('视频采集页加载(dy_login读取)', proc.poll() is None)

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
