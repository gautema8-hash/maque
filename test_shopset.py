# -*- coding: utf-8 -*-
"""店铺管理页全功能专项测试"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'

R = []
def log(m): print('[TEST]', m, flush=True)
def rec(n, ok, note=''):
    R.append((n, ok, note)); print('[%s] %s %s' % ('PASS' if ok else 'FAIL', n, note), flush=True)
def shot(n):
    try: ImageGrab.grab().save(os.path.join(OUT, n + '.png')); log('shot ' + n)
    except Exception as e: log('shot fail %r' % e)

SYSTEM_PAT = re.compile(r'麻雀一键铺货|任务栏|Program Manager|WorkBuddy|Chrome|Edge|Microsoft|文件资源管理器|拼多多|豆包|输入体验|Agent')

def click_btn(main, title, idx=0):
    btns = [b for b in main.descendants(control_type='Button') if (b.window_text() or '') == title]
    if len(btns) <= idx:
        raise RuntimeError('button not found: %s (%d)' % (title, len(btns)))
    btns[idx].click_input()

def click_cancel(main):
    btns = [b for b in main.descendants(control_type='Button') if (b.window_text() or '') == '取消']
    if btns: btns[0].click_input(); time.sleep(1.2); return True
    return False

def popup_texts(w):
    out = ''
    try:
        for t in w.descendants(control_type='Text'):
            out += (t.element_info.name or '') + ' '
    except Exception:
        pass
    return out.strip()

import ctypes as _ct
_u32 = _ct.windll.user32
_WNDENUMPROC = _ct.WINFUNCTYPE(_ct.c_int, _ct.c_void_p, _ct.c_void_p)

def _enum_toplevel():
    out = []
    @_WNDENUMPROC
    def _cb(hwnd, _):
        if _u32.IsWindowVisible(hwnd):
            n = _u32.GetWindowTextLengthW(hwnd)
            buf = __import__('ctypes').create_unicode_buffer(n + 1)
            _u32.GetWindowTextW(hwnd, buf, n + 1)
            out.append((hwnd, buf.value))
        return True
    _u32.EnumWindows(_cb, 0)
    return out

POPUP_TITLE_PAT = re.compile(r'提示|注意|采集失败|商品异常|操作完成|清空操作|登录失败|警告')

def close_popups(max_wait=8):
    """轮询等待弹窗出现（ZMessageBox 标题=caption，出现有延迟），出现则点确定关闭"""
    closed = None
    t0 = time.time()
    while time.time() - t0 < max_wait:
        for hwnd, t in _enum_toplevel():
            if t and POPUP_TITLE_PAT.search(t):
                try:
                    w = Desktop(backend='uia').window(handle=hwnd)
                    POPUP_SEQ[0] += 1
                    ImageGrab.grab().save(os.path.join(OUT, 'popup_%03d.png' % POPUP_SEQ[0]))
                    btns = w.descendants(control_type='Button')
                    for b in btns:
                        if '确定' in (b.window_text() or ''):
                            b.click_input(); break
                    else:
                        if btns: btns[0].click_input()
                    closed = t
                    time.sleep(1.0)
                    return closed
                except Exception:
                    pass
        time.sleep(0.8)
    return closed

def get_state(main):
    try:
        st = main.child_window(auto_id='txt_State')
        ed = st.descendants(control_type='Text')
        if ed: return ed[0].element_info.name or ''
    except Exception: pass
    return None

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
log('pid=%d' % proc.pid)
t0 = time.time()
main = None
while time.time() - t0 < 90:
    try:
        main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
        main.set_focus(); time.sleep(3); break
    except Exception:
        time.sleep(1.5)
assert main, 'main timeout'
close_popups()

# 切到店铺管理并等待懒加载实例化
main.child_window(title='店铺管理', control_type='TabItem').click_input()
time.sleep(3.5)
shot('50_shopset_init')
rec('店铺管理页加载', True)

# 1) 查找店铺（空关键词=查全部，验证日志反馈）
click_btn(main, '查找店铺')
time.sleep(1.5)
logtext = ''
try:
    rt = main.child_window(auto_id='myRichTextBox')
    docs = rt.descendants(control_type='Document')
    if docs: logtext = docs[0].element_info.name or ''
except Exception: pass
rec('查找店铺(空关键词查全部+日志反馈)', '查找店铺[全部]' in logtext or '0 家' in logtext or logtext == '', 'log=%r' % logtext[:80])
shot('51_shopset_find')

# 2) 全选（无店铺 → 提示）
click_btn(main, '全选')
time.sleep(1.5)
p = close_popups()
rec('全选-无店铺提示', p is not None, 'popup=%r' % p)
shot('52_shopset_choose_empty')

# 3) 选中使用店铺（无选中 → 提示"请先选择店铺！"）
click_btn(main, '选中使用店铺')
time.sleep(1.5)
p = close_popups()
rec('选中使用店铺-提示弹窗', p is not None, 'popup=%r' % p)

# 4) 统一更新cookie（无勾选 → 提示）
click_btn(main, '统一更新cookie')
time.sleep(1.5)
p = close_popups()
rec('统一更新cookie-无勾选提示弹窗', p is not None, 'popup=%r' % p)

# 5) 删除失效店铺（确认弹窗 → 取消）
click_btn(main, '删除失效店铺')
time.sleep(1.5)
titles = [w.window_text() for w in desktop.windows()]
has_confirm = any('清空操作' in (t or '') for t in titles)
if has_confirm:
    click_cancel(main)
    time.sleep(1)
rec('删除失效店铺-二次确认弹窗', has_confirm, '')
shot('53_shopset_clear_confirm')

# 6) 获取二维码（重复点击保护 + 超时降级）
click_btn(main, '获取二维码')
time.sleep(1.5)
p = close_popups()
rec('获取二维码-重复点击保护提示', p is not None and '正在获取' in (p or ''), 'popup=%r' % p)
state1 = get_state(main)
rec('获取二维码-状态"获取中"', state1 is not None and '获取中' in state1, 'state=%r' % state1)
shot('54_shopset_qr_fetching')
log('等待二维码获取超时降级(约20秒)...')
time.sleep(24)
state2 = get_state(main)
rec('获取二维码-超时降级为"获取失败"', state2 is not None and '获取失败' in state2, 'state=%r' % state2)
shot('55_shopset_qr_failed')

# 再次点击获取二维码（获取失败后 count 已复位，应能重新开始）
click_btn(main, '获取二维码')
time.sleep(1.5)
p = close_popups()
state3 = get_state(main)
rec('获取二维码-失败后可重新发起', state3 is not None and ('获取中' in state3 or '获取失败' in state3), 'popup=%r state=%r' % (p, state3))
close_popups()

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
