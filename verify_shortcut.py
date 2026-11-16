# -*- coding: utf-8 -*-
"""验证桌面快捷方式指向"""
import os
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import win32com.client

shell = win32com.client.Dispatch("WScript.Shell")
sc = shell.CreateShortCut(r"C:\Users\xuyan\Desktop\麻雀一键铺货.lnk")
print("TargetPath:", sc.TargetPath)
print("WorkDir:", sc.WorkingDirectory)
print("Icon:", sc.IconLocation)
print("Target exists:", os.path.isfile(sc.TargetPath))
