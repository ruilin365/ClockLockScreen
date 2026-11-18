# 时钟锁屏 0.8.9

> **如果你从 0.8.8 升级后遇到「双击没反应 / 一开就闪退」，装这一版。**
> 0.8.8 在「主题 = 跟随系统」时启动必崩，而跟随系统正是默认值。

## 下载

`ClockLockScreen_v0.8.9_win_x64.zip` —— 解压后直接运行 `ClockLockScreen.exe`。

- 需要 [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- 需要 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Win11 通常已内置）
- 首次运行请求管理员权限（锁屏与全局快捷键需要）
- `fonts/`、`wwwroot/`、`WebView2Loader.dll` 必须和 exe 放在同一目录
- 换版本时重新拨一次「开机启动」开关，计划任务才会指向新路径

## 修复

- **启动闪退（严重）** —— 主题为「跟随系统」时，界面初始化阶段会去问 MudBlazor 要系统深色模式，但那时控件引用还没绑定，直接空引用崩溃。表现为程序反复启动又反复退出、不留任何日志。现在会先读注册表兜底，首次渲染后再向控件纠正一次
- **重置设置后不再自动开启开机启动** —— 开机自启是存在「计划任务」里的，不在配置文件里，以前「重置程序设置」只会删配置，计划任务还留着，重置完下次开机照样自启。现在重置时会一并删除
- **补充崩溃日志** —— 以前未捕获异常直接杀进程，什么都不留。现在会写到配置目录下的 `crash.log`，闪退至少能查了

## 0.8.8 带来的内容

- 屏幕模糊（毛玻璃）：定格 / 实时两种，强度可调
- 锁屏画面三层叠加：模糊 → 遮罩 → 时钟，各自独立开关
- 遮罩可指定任意纯色或网格渐变，「时钟背景」并入遮罩
- 恢复默认外观，只重置外观，不动密码 / 快捷键 / 自动锁定

---

# Clock Lock Screen 0.8.9

> **If 0.8.8 crashes on launch for you, install this one.**
> 0.8.8 crashes at startup whenever the theme is "follow system", which is the default.

## Download

`ClockLockScreen_v0.8.9_win_x64.zip` — unzip and run `ClockLockScreen.exe`.

- Requires [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- Requires [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on most Windows 11)
- Administrator rights are requested on first run (locking and global hotkeys)
- Keep `fonts/`, `wwwroot/` and `WebView2Loader.dll` next to the executable
- Toggle autostart off and on again after upgrading, so the scheduled task points at the new path

## Fixes

- **Startup crash (critical)** — with the theme set to "follow system", the layout asked MudBlazor for the system dark mode during initialisation, before the control reference was bound, and threw a null reference. The app restarted itself over and over without writing a single log line. It now falls back to reading the registry, then corrects itself after the first render
- **Resetting settings no longer leaves autostart on** — autostart lives in a scheduled task, not the config file, so "reset" used to delete the config and leave the task behind. Reset now removes the task too
- **Crash log added** — unhandled exceptions used to kill the process silently. They are now appended to `crash.log` in the config directory

## What 0.8.8 brought

- Screen blur (frosted glass): frozen or live, adjustable strength
- Three stacked lock layers: blur → mask → clock, each with its own switch
- The mask takes any solid colour or mesh gradient; the old "clock background" folded into it
- Restore defaults, touching only appearance — never your password, hotkeys or auto-lock

---

完整说明见 [README](https://github.com/ruilin365/ClockLockScreen#readme) ｜ Full documentation in the [README](https://github.com/ruilin365/ClockLockScreen#readme)
