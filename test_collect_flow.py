# -*- coding: utf-8 -*-
"""采集全流程测试：goodid 963049451730，登录→任务设置→拼夕夕→商品页→提取管线"""
import os, time, subprocess
BASE = os.path.dirname(os.path.abspath(__file__))
APPDIR = os.path.join(BASE, "麻雀一键铺货")
EXE = os.path.join(APPDIR, "DuoDuoPuHuo.exe")
SHOTS = os.path.join(BASE, "testshots")
os.makedirs(SHOTS, exist_ok=True)
GOODID = "963049451730"
RESULT = os.path.join(APPDIR, "collecttest_result.txt")

os.system('taskkill /F /IM DuoDuoPuHuo.exe >nul 2>&1')
time.sleep(1)
if os.path.exists(RESULT): os.remove(RESULT)
proc = subprocess.Popen([EXE, "--collecttest=" + GOODID], cwd=APPDIR)
print("launched", proc.pid, flush=True)

# 保存主窗口截图
try:
    from pywinauto import Application
    time.sleep(10)
    app = Application(backend="uia").connect(process=proc.pid)
    end = time.time() + 30
    main = None
    while time.time() < end and not main:
        try:
            w = app.window(title="麻雀一键铺货")
            if w.exists(timeout=0.5): main = w
        except Exception: pass
        time.sleep(0.5)
    if main:
        main.capture_as_image().save(os.path.join(SHOTS, "98_collect_main.png"))
        print("main screenshot saved", flush=True)
except Exception as e:
    print("screenshot skip:", e, flush=True)

# 等待结果文件
end = time.time() + 75
while time.time() < end and not os.path.exists(RESULT):
    time.sleep(1)

if os.path.exists(RESULT):
    content = open(RESULT, "r", encoding="utf-8", errors="ignore").read()
    print("=== 采集全流程测试结果 ===\n" + content, flush=True)
    open(os.path.join(BASE, "test_collect_flow_out.txt"), "w", encoding="utf-8").write(content)
else:
    print("FAIL: 结果文件未生成", flush=True)
    open(os.path.join(BASE, "test_collect_flow_out.txt"), "w", encoding="utf-8").write("FAIL: 结果文件未生成\n")

os.system('taskkill /F /IM DuoDuoPuHuo.exe >nul 2>&1')
