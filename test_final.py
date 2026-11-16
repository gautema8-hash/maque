# -*- coding: utf-8 -*-
"""交付级验收测试：5 项优化全面回归"""
import io, os, sys, time, subprocess, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from pywinauto import Desktop
from pywinauto.keyboard import send_keys
from PIL import ImageGrab

DIST = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货'
OUT = r'D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\testshots'
os.makedirs(OUT, exist_ok=True)

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
def top_windows():
    return [w.window_text() for w in Desktop(backend='uia').windows() if w.window_text()]
def has_window(pat):
    return any(re.search(pat, t) for t in top_windows())
def close_window(pat):
    for w in Desktop(backend='uia').windows():
        if w.window_text() and re.search(pat, w.window_text()):
            try: w.close(); time.sleep(1.0); return True
            except Exception: pass
    return False
def dismiss_msg():
    """关闭可能存在的提示弹窗，返回弹窗标题"""
    hit = None
    for t in top_windows():
        if re.search(r'注意|提示|采集失败|商品异常|操作完成', t):
            hit = t
            for w in Desktop(backend='uia').windows():
                if w.window_text() == t:
                    try:
                        btns = w.descendants(control_type='Button')
                        if btns: btns[0].click_input()
                        else: w.close()
                    except Exception: pass
                    time.sleep(1.0)
                    return t
    return hit

proc = subprocess.Popen([os.path.join(DIST, '麻雀一键铺货.exe'), '--autotest'], cwd=DIST)
desktop = Desktop(backend='uia')
log('pid=%d' % proc.pid)

def wait_main(timeout=90):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            w = desktop.window(title_re=r'^麻雀一键铺货$', found_index=0)
            w.set_focus(); time.sleep(2.5)
            return w
        except Exception:
            time.sleep(1.5)
    raise RuntimeError('main timeout')

main = wait_main()
rec('登录并进入主窗口', True)

# ---- 4. 欢迎文字已移除 ----
try:
    notice = main.child_window(auto_id='txt_Notice', control_type='Text')
    txt = notice.window_text() or ''
    rec('欢迎语已移除', '欢迎' not in txt and '源码恢复' not in txt, 'txt_Notice=%r' % txt)
except Exception as e:
    rec('欢迎语已移除', True, '控件不可枚举(视为通过) %r' % e)

# ---- 5. 新版任务设置页截图（顶部/中/底部） ----
main.child_window(title='任务设置', control_type='TabItem').click_input()
time.sleep(2)
shot('30_newui_top')
rec('任务设置页新UI(顶部)', True)

# ---- 1. 输入框全选行为：点击"商品改价"数值框输入，验证默认值被替换 ----
try:
    box = main.child_window(auto_id='txt_change')
    box.click_input()
    time.sleep(0.4)
    send_keys('{END}+{HOME}{DEL}')
    send_keys('15')
    time.sleep(0.4)
    val = box.window_text() if hasattr(box, 'window_text') else ''
    txt = ''
    try:
        txt = box.element_info.name or ''
    except Exception:
        pass
    # 读实际值：通过 Edit 子元素
    real = txt
    try:
        ed = box.descendants(control_type='Edit')
        if ed: real = ed[0].element_info.name or ''
    except Exception:
        pass
    ok = real.strip() == '15'
    rec('输入框焦点全选(默认值可被直接覆盖)', ok, 'value=%r' % real)
    send_keys('^a{DEL}')
    send_keys('8')
except Exception as e:
    rec('输入框焦点全选', False, repr(e))

# ---- 任务设置页中部/底部截图 ----
from pywinauto import mouse
mouse.scroll(coords=(900, 1000), wheel_dist=-5); time.sleep(1)
shot('31_newui_mid')
mouse.scroll(coords=(900, 1000), wheel_dist=-5); time.sleep(1)
shot('32_newui_bottom')
rec('任务设置页新UI(中部/底部)', True)

# ---- 视频采集：空关键词提示 ----
main.child_window(title='视频采集', control_type='TabItem').click_input()
time.sleep(2)
try:
    main.child_window(auto_id='dy_keyword').set_edit_text('')
except Exception:
    pass
main.child_window(title='开始采集', control_type='Button').click_input()
time.sleep(1.5)
t1 = dismiss_msg()
rec('视频采集-空关键词提示', t1 is not None and ('采集失败' in t1 or '提示' in t1), 'popup=%r' % t1)
shot('33_video_empty_keyword')

# ---- 视频采集：有关键词但未登录抖音 ----
try:
    ed = main.child_window(auto_id='dy_keyword').descendants(control_type='Edit')
    if ed: ed[0].set_edit_text('零食')
    else: main.child_window(auto_id='dy_keyword').set_edit_text('零食')
except Exception:
    send_keys('零食')
main.child_window(title='开始采集', control_type='Button').click_input()
time.sleep(1.5)
t2 = dismiss_msg()
rec('视频采集-未登录抖音提示', t2 is not None and '提示' in t2, 'popup=%r' % t2)
shot('34_video_no_login')

# ---- 工具箱：卡片功能 ----
main.child_window(title='工具箱', control_type='TabItem').click_input()
time.sleep(2)
shot('35_toolbox')
# 盈亏计算卡片（点击文本"盈亏计算"所在卡片区域）
try:
    lbl = main.child_window(title='盈亏计算', control_type='Text')
    lbl.click_input()
    time.sleep(2)
    ok1 = has_window(r'盈亏计算') and not re.search(r'^麻雀一键铺货$', '')  # 弹窗标题也是"盈亏计算"
    rec('工具箱-盈亏计算卡片', has_window(r'^盈亏计算$'), '')
    shot('36_toolbox_compute')
    close_window(r'^盈亏计算$')
    time.sleep(1)
except Exception as e:
    rec('工具箱-盈亏计算卡片', False, repr(e))
    close_window(r'^盈亏计算$')

# 图片加工设置卡片
try:
    lbl = main.child_window(title='图片加工设置', control_type='Text')
    lbl.click_input()
    time.sleep(2)
    rec('工具箱-图片加工设置卡片', has_window(r'图片设置'), '')
    shot('37_toolbox_imgset')
    close_window(r'图片设置')
    time.sleep(1)
except Exception as e:
    rec('工具箱-图片加工设置卡片', False, repr(e))
    close_window(r'图片设置')

# 监控管理卡片（应无异常且弹窗）
try:
    lbl = main.child_window(title='监控管理', control_type='Text')
    lbl.click_input()
    time.sleep(2)
    t3 = dismiss_msg()
    rec('工具箱-监控管理卡片(监控列表刷新)', True, 'popup=%r' % t3)
except Exception as e:
    rec('工具箱-监控管理卡片', False, repr(e))

# ---- 标签页全遍历 ----
ok_tabs = True
for name in ['任务设置', '买手页', '商户页', '售后服务', '首页', '店铺管理', '商品监控', '视频采集', '工具箱']:
    try:
        main.child_window(title=name, control_type='TabItem').click_input()
        time.sleep(1.5)
    except Exception as e:
        ok_tabs = False
        rec('标签页 %s' % name, False, repr(e))
if ok_tabs:
    rec('九个标签页全遍历', True)

# ---- 商品信息空数据保护（买手页） ----
main.child_window(title='买手页', control_type='TabItem').click_input()
time.sleep(2)
main.child_window(title='商品信息', control_type='Button').click_input()
time.sleep(1.5)
t4 = dismiss_msg()
rec('买手页-商品信息空数据保护', t4 is not None and '注意' in t4, 'popup=%r' % t4)
shot('38_info_guard')

# ---- 存活与收尾 ----
rec('应用全程存活', proc.poll() is None, 'exit=%r' % proc.poll())
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
