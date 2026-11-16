# -*- coding: utf-8 -*-
"""创建 麻雀一键铺货 桌面快捷方式"""
import os
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import win32com.client

DESKTOP = r"C:\Users\xuyan\Desktop"
EXE = r"D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货\麻雀一键铺货.exe"
DIST = r"D:\sofa\aiproject\workBuddyWork\feiying\pinxixipuhuo\麻雀一键铺货"
ICON = os.path.join(DIST, "logo.ico")
LNK = os.path.join(DESKTOP, "麻雀一键铺货.lnk")

if not os.path.isfile(EXE):
    raise SystemExit("exe not found: " + EXE)

shell = win32com.client.Dispatch("WScript.Shell")
sc = shell.CreateShortCut(LNK)
sc.TargetPath = EXE
sc.WorkingDirectory = DIST
if os.path.isfile(ICON):
    sc.IconLocation = ICON
sc.Description = "麻雀一键铺货 - 拼多多商品一键采集/一键铺货工具"
sc.WindowStyle = 1
sc.save()

print("created:", LNK)
print("exists:", os.path.isfile(LNK), "size:", os.path.getsize(LNK))
