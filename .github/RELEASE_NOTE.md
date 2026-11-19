# 时钟锁屏 0.8.10

## 下载

`ClockLockScreen_v0.8.10_win_x64.zip` —— 解压后直接运行 `ClockLockScreen.exe`。

- 需要 [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- 需要 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Win11 通常已内置）
- 首次运行请求管理员权限（锁屏与全局快捷键需要）
- `fonts/`、`wwwroot/`、`WebView2Loader.dll` 必须和 exe 放在同一目录
- 换版本时重新拨一次「开机启动」开关，计划任务才会指向新路径

## 新功能

- **首次启动引导设置锁屏快捷键** —— 快捷键一直不给默认值（避免和你别的软件抢按键），但以前不给默认值又不提示，重置设置或全新安装的人会以为程序坏了：既锁不了屏，也不知道该干什么。现在：
  - 没有快捷键时，程序**不再缩到托盘**，直接把窗口露出来
  - 弹一个「先设一个锁屏快捷键」的对话框，里面可以直接录制组合键，设置完立刻生效
  - 关掉对话框后，界面顶部会留一条提示，点「现在设置」随时能补
  - 提示只自动弹一次，之后不再打扰
  - 引导里同时带上了「锁屏时显示什么」四个开关（屏幕模糊 / 全屏遮罩 / 翻页时钟 / 屏蔽任务管理器），默认全开，第一次用就能看清这软件会怎么盖住屏幕
- **新增：锁定时屏蔽任务管理器** —— 以前是写死的，锁屏一定禁任务管理器，现在变成开关。走的是一项 Windows 注册表策略（`DisableTaskMgr`），部分杀毒软件会把它当成恶意改动拦截；设置页和引导页都标注了这一点。万一被拦，锁屏照样生效，只是任务管理器还能打开

## 修复

- 上一版（0.8.9）修好的启动闪退、重置后自启仍在、崩溃日志补齐，这版一并包含

## 其余功能

- **屏幕模糊（毛玻璃）**：定格 / 实时两种，强度可调
- **三层叠加**：模糊 → 遮罩 → 时钟，各自独立开关
- **遮罩可指定任意颜色**：纯色或网格渐变，不透明度 0–100%
- **翻页时钟**：四层裁剪结构，不叠影；7 款开源字体，可导入自己的
- **全部可调**：模糊强度、遮罩不透明度、时钟不透明度、大小、间距、翻页速度、帧率、圆角、牌面浓度等，都是滑块，支持两位小数输入

---

# Clock Lock Screen 0.8.10

## Download

`ClockLockScreen_v0.8.10_win_x64.zip` — unzip and run `ClockLockScreen.exe`.

- Requires [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- Requires [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on most Windows 11)
- Administrator rights are requested on first run (locking and global hotkeys)
- Keep `fonts/`, `wwwroot/` and `WebView2Loader.dll` next to the executable
- Toggle autostart off and on again after upgrading, so the scheduled task points at the new path

## New

- **First-run guide for the lock hotkey** — the hotkey has never had a default (so it won't fight with your other apps), but without one and without any hint, a fresh install or a settings reset looked broken: you couldn't lock, and had no idea what to do. Now:
  - With no hotkey set, the app **no longer hides in the tray** — the window comes up instead
  - A "Set a lock hotkey" dialog opens where you can record the combination; it takes effect immediately
  - A banner stays at the top afterwards, with a "Set it up" button whenever you want
  - The dialog only auto-opens once
  - The dialog also carries the four "what the lock screen shows" switches (blur / mask / flip clock / block Task Manager), all on by default, so a first-time user sees immediately what will cover the screen
- **New: blocking Task Manager is now a switch** — previously hard-coded, so locking always killed Task Manager. It flips a Windows registry policy (`DisableTaskMgr`), which some antivirus products flag as malicious; both the settings page and the guide say so. If it gets blocked, locking still works — Task Manager just stays available

## Fixes

- Includes the 0.8.9 fixes: startup crash, autostart surviving a reset, crash log

## Features

- **Screen blur (frosted glass)**: frozen or live, adjustable strength
- **Three stacked layers**: blur → mask → clock, each with its own switch
- **The mask takes any colour**: solid or mesh gradient, opacity 0–100%
- **Flip clock**: four-layer clipping, no ghosting; 7 bundled fonts plus your own
- **Everything adjustable**: blur strength, mask opacity, clock opacity, size, gaps, flip speed, frame rate, corner radius, card fill — all sliders, two-decimal input

---

完整说明见 [README](https://github.com/ruilin365/ClockLockScreen#readme) ｜ Full documentation in the [README](https://github.com/ruilin365/ClockLockScreen#readme)
