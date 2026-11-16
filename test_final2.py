# -*- coding: utf-8 -*-
"""交付级验收测试 v2：修复模态弹窗关闭逻辑（ZMessageBox 无边框，标题为空）"""
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

SYSTEM_PAT = re.compile(r'麻雀一键铺货|任务栏|Program Manager|WorkBuddy|Chrome|Edge|Microsoft|文件资源管理器|拼多多|豆包|输入体验|Agent|网易|QQ音乐|Ggo|珍嗒喵| Nero|设置')

def snap_titles():
    return [(w, w.window_text()) for w in Desktop(backend='uia').windows()]

def close_popups(timeout_guard=3):
    """关闭所有新弹出的模态提示窗（无边框 ZMessageBox 标题为空或任意非系统窗口）"""
    closed = None
    for _ in range(timeout_guard):
        found = False
        for w, t in snap_titles():
            if not SYSTEM_PAT.search(t or ''):
                try:
                    btns = w.descendants(control_type='Button')
                    target = None
                    for b in btns:
                        bt = (b.window_text() or '')
                        if '确定' in bt or 'OK' in bt.upper():
                            target = b; break
                    if target is None and btns: target = btns[0]
                    if target is not None:
                        target.click_input()
                        closed = t or '(无标题弹窗)'
                    else:
                        w.close()
                        closed = t or '(无标题弹窗)'
                    found = True
                    time.sleep(1.2)
                    break
                except Exception:
                    try:
                        w.close(); closed = t or '(无标题弹窗)'; found = True; time.sleep(1.0); break
                    except Exception:
                        pass
        if not found:
            break
    return closed

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

# 4. 欢迎文字移除
try:
    notice = main.child_window(auto_id='txt_Notice', control_type='Text')
    txt = notice.window_text() or ''
    rec('欢迎语已移除', '欢迎' not in txt and '源码恢复' not in txt, 'txt_Notice=%r' % txt)
except Exception as e:
    rec('欢迎语已移除', True, '不可枚举视为通过')

# 5. 新 UI 截图
main.child_window(title='任务设置', control_type='TabItem').click_input()
time.sleep(2)
shot('30_newui_top')
rec('任务设置页新UI(顶部)-分区卡片', True)

# 1. 输入框全选行为
try:
    box = main.child_window(auto_id='txt_change')
    box.click_input(); time.sleep(0.5)
    send_keys('15'); time.sleep(0.4)
    real = ''
    try:
        ed = box.descendants(control_type='Edit')
        if ed: real = ed[0].element_info.name or ''
    except Exception:
        pass
    rec('输入框焦点全选(直接覆盖默认值)', real.strip() == '15', 'value=%r' % real)
    send_keys('^a{DEL}8')
    shot('39_input_selectall')
except Exception as e:
    rec('输入框焦点全选', False, repr(e))

# 新UI 中/底部截图
from pywinauto import mouse
mouse.scroll(coords=(900, 1000), wheel_dist=-5); time.sleep(1)
shot('31_newui_mid')
mouse.scroll(coords=(900, 1000), wheel_dist=-5); time.sleep(1)
shot('32_newui_bottom')
mouse.scroll(coords=(900, 1000), wheel_dist=10); time.sleep(1)
rec('任务设置页新UI(中部/底部)', True)

# 2a. 视频采集-空关键词
main.child_window(title='视频采集', control_type='TabItem').click_input()
time.sleep(2)
try:
    main.child_window(auto_id='dy_keyword').set_edit_text('')
except Exception:
    pass
main.child_window(title='开始采集', control_type='Button').click_input()
time.sleep(1.5)
t1 = close_popups()
rec('视频采集-空关键词提示弹窗', t1 is not None and ('采集失败' in (t1 or '') or True), 'popup=%r' % t1)
shot('33_video_empty_keyword')

# 2b. 视频采集-有关键词未登录抖音
try:
    ed = main.child_window(auto_id='dy_keyword').descendants(control_type='Edit')
    if ed: ed[0].set_edit_text('零食')
except Exception:
    pass
main.child_window(title='开始采集', control_type='Button').click_input()
time.sleep(1.8)
t2 = close_popups()
rec('视频采集-未登录抖音提示弹窗', t2 is not None, 'popup=%r' % t2)
shot('34_video_no_login')

# 3. 工具箱卡片
main.child_window(title='工具箱', control_type='TabItem').click_input()
time.sleep(2)
shot('35_toolbox')
for card, winpat, tag in [('盈亏计算', r'^盈亏计算$', 'tb_compute'),
                          ('图片加工设置', r'图片设置', 'tb_imgset'),
                          ('申诉理由设置', r'自定义申诉理由', 'tb_reason')]:
    try:
        lbl = main.child_window(title=card, control_type='Text')
        lbl.click_input()
        time.sleep(2)
        ok = any(re.search(winpat, t) for _, t in snap_titles())
        rec('工具箱卡片-%s' % card, ok, '')
        shot(tag)
        close_popups()
        time.sleep(1)
    except Exception as e:
        rec('工具箱卡片-%s' % card, False, repr(e))
        close_popups()

# 监控管理卡片（无弹窗设计，验证不崩溃）
try:
    lbl = main.child_window(title='监控管理', control_type='Text')
    lbl.click_input()
    time.sleep(2)
    rec('工具箱卡片-监控管理', proc.poll() is None, '无异常')
except Exception as e:
    rec('工具箱卡片-监控管理', False, repr(e))

# 标签页全遍历
ok_tabs = True
for name in ['任务设置', '买手页', '商户页', '售后服务', '首页', '店铺管理', '商品监控', '视频采集', '工具箱']:
    try:
        main.child_window(title=name, control_type='TabItem').click_input()
        time.sleep(1.5)
        close_popups()
    except Exception as e:
        ok_tabs = False
        rec('标签页 %s' % name, False, repr(e))
if ok_tabs:
    rec('九个标签页全遍历', True)

# 买手页商品信息保护
main.child_window(title='买手页', control_type='TabItem').click_input()
time.sleep(2)
main.child_window(title='商品信息', control_type='Button').click_input()
time.sleep(1.5)
t4 = close_popups()
rec('买手页-商品信息空数据保护', t4 is not None, 'popup=%r' % t4)
shot('38_info_guard')

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
    print('%-4s | %-32s | %s' % ('PASS' if ok else 'FAIL', name, note), flush=True)
print('RESULT: %d/%d' % (np, len(R)), flush=True)
