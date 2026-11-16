# -*- coding: utf-8 -*-
"""首页搜索功能专项测试（商品搜索/店铺搜索/回车触发/空值校验）"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop
from pywinauto.keyboard import send_keys
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'

R = []
def log(m): print('[TEST]', m, flush=True)
def rec(name, ok, note=''):
    R.append((name, ok, note))
    print('[%s] %s %s' % ('PASS' if ok else 'FAIL', name, note), flush=True)
def shot(name):
    try:
        ImageGrab.grab().save(os.path.join(OUT, name + '.png'))
        log('shot ' + name)
    except Exception as e:
        log('shot fail %r' % e)

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
                    target = None
                    for b in btns:
                        bt = (b.window_text() or '')
                        if '确定' in bt or 'OK' in bt.upper(): target = b; break
                    if target is None and btns: target = btns[0]
                    if target is not None:
                        target.click_input(); closed = t or '(无标题)'; found = True
                    else:
                        w.close(); closed = t or '(无标题)'; found = True
                    time.sleep(1.2)
                    break
                except Exception:
                    try: w.close(); closed = t or '(无标题)'; found = True; time.sleep(1.0); break
                    except Exception: pass
        if not found: break
    return closed

def set_text(wrapper, text):
    """真实键盘输入（ASCII 最可靠）；先点击聚焦"""
    try:
        ed = wrapper.descendants(control_type='Edit')
        if ed:
            ed[0].click_input(); time.sleep(0.4)
            send_keys('^a{DEL}')
            send_keys(text, with_spaces=True)
            time.sleep(0.4)
            return
    except Exception:
        pass
    wrapper.click_input(); time.sleep(0.4)
    send_keys('^a{DEL}')
    send_keys(text, with_spaces=True)

def read_text(wrapper):
    try:
        ed = wrapper.descendants(control_type='Edit')
        if ed: return ed[0].element_info.name or ''
    except Exception:
        pass
    try: return wrapper.element_info.name or ''
    except Exception: return ''

def switch_tab(name, tries=4):
    for i in range(tries):
        try:
            main.child_window(title=name, control_type='TabItem').click_input()
            time.sleep(2)
            # 验证内容已实例化
            try:
                main.child_window(title=name, control_type='TabItem').descendants()
                return True
            except Exception:
                time.sleep(1.5)
        except Exception:
            time.sleep(1.5)
    return False

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
log('pid=%d' % proc.pid)

t0 = time.time()
main = None
while time.time() - t0 < 90:
    try:
        main = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
        main.set_focus(); time.sleep(2.5); break
    except Exception:
        time.sleep(1.5)
assert main, 'main timeout'
rec('登录并进入主窗口', True)

# 切到首页
switch_tab('首页')
shot('40_home_page')

# 1) 空关键词点击商品搜索 → 提示
main.child_window(title='搜索', found_index=0).click_input()
time.sleep(1.5)
p1 = close_popups()
rec('商品搜索-空关键词校验', p1 is not None, 'popup=%r' % p1)
shot('41_search_empty')

# 2) 商品搜索"零食" → 跳买手页 + URL 校验
goods = main.child_window(auto_id='txtGoodsKey')
log('keyword prefilled by app: snacks')
time.sleep(0.4)
# 首页 tab 中有两个"搜索"按钮，用 index 定位第二个也行；这里直接找 商品搜索 tab 内第一个
btns = main.child_window(title='搜索', found_index=0)
main.child_window(title='搜索', found_index=0).click_input()
time.sleep(4)
# 验证当前 tab 是否为买手页：读 txtBrowser 地址
url = ''
for _ in range(3):
    try:
        tb = main.child_window(auto_id='txtBrowser')
        url = read_text(tb)
        if 'search_key=' in url: break
    except Exception:
        pass
    time.sleep(2)
ok2 = ('search_key=' in url and 'goods' in url)
rec('商品搜索-跳转买手页+URL参数', ok2, 'url=%r' % url[:110])
shot('42_search_goods_result')

# 3) 店铺搜索
switch_tab('首页')
time.sleep(1)
store = None
for _ in range(4):
    try:
        store = main.child_window(auto_id='txtStoreKey'); break
    except Exception:
        time.sleep(2)
if store is None:
    raise RuntimeError('txtStoreKey not found')
log('keyword prefilled by app: flagship')
# 第二个搜索按钮
try:
    all_btns = main.descendants(control_type='Button')
    search_btns = [b for b in all_btns if (b.window_text() or '') == '搜索']
    if len(search_btns) >= 2:
        search_btns[1].click_input()
    else:
        search_btns[0].click_input()
except Exception as e:
    log('store btn err %r' % e)
time.sleep(4)
url = ''
for _ in range(3):
    try:
        tb = main.child_window(auto_id='txtBrowser')
        url = read_text(tb)
        if 'search_key=' in url: break
    except Exception:
        pass
    time.sleep(2)
ok3 = ('search_key=' in url and 'mall' in url)
rec('店铺搜索-跳转买手页+URL参数', ok3, 'url=%r' % url[:110])
shot('43_search_store_result')

# 4) 商品关键词回车触发
switch_tab('首页')
time.sleep(1)
goods = None
for _ in range(4):
    try:
        goods = main.child_window(auto_id='txtGoodsKey'); break
    except Exception:
        time.sleep(2)
if goods is None:
    raise RuntimeError('txtGoodsKey not found')
log('keyword prefilled by app: nuts')
try:
    ed = goods.descendants(control_type='Edit')
    if ed:
        ed[0].click_input(); send_keys('{ENTER}')
    else:
        goods.set_edit_text('坚果')
except Exception:
    pass
time.sleep(4)
url = ''
for _ in range(3):
    try:
        tb = main.child_window(auto_id='txtBrowser')
        url = read_text(tb)
        if 'search_key=' in url: break
    except Exception:
        pass
    time.sleep(2)
ok4 = ('search_key=' in url and 'nuts' in url)
rec('商品搜索-回车触发', ok4, 'url=%r' % url[:110])
shot('44_search_enter')

rec('应用全程存活', proc.poll() is None, '')
if proc.poll() is None:
    try:
        main.close()
    except Exception:
        pass
    time.sleep(2)
    if proc.poll() is None:
        proc.terminate()

print('\n===== SUMMARY =====', flush=True)
np = sum(1 for _, ok, _ in R if ok)
for name, ok, note in R:
    print('%-4s | %-30s | %s' % ('PASS' if ok else 'FAIL', name, note), flush=True)
print('RESULT: %d/%d' % (np, len(R)), flush=True)
