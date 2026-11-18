# 时钟锁屏 0.8.7

离开电脑一会儿，AI Agent 还在跑、下载还没完、脚本还没结束 —— 既不能让电脑休眠，又不想让别人动你的屏幕。

## 下载

`ClockLockScreen_v0.8.7_win_x64.zip` —— 解压后直接运行 `ClockLockScreen.exe`。

- 需要 [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- 需要 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Win11 通常已内置）
- 首次运行请求管理员权限（锁屏与全局快捷键需要）
- `fonts/`、`wwwroot/`、`WebView2Loader.dll` 必须和 exe 放在同一目录
- 换版本时重新拨一次「开机启动」开关，计划任务才会指向新路径

## 这一版的改动

- **新增：数字间距与时分秒间距分开可调** —— 「数字间距」控制同一组里两位数之间的距离，「时分秒间距」控制 hh / mm / ss 三组之间的距离，范围 0~80 / 0~160 像素，拖动立即生效
- **修复：主窗口被压成一条细条** —— 某些情况下窗口恢复时只拿到系统最小尺寸（160×39），标题栏文字竖排、内容出现滚动条。现在启动和每次恢复都会检查尺寸，异常时撑回 800×600 并居中；同时给窗口设了最小尺寸兜底

## 其余功能

- **三种锁屏外观**：完全透明 / 黑色遮罩（透明度可调）/ 翻页时钟，背景与时钟的不透明度分开调
- **翻页动画**：四层裁剪结构，上半翻落、下半翻起，压扁过程不叠影；可显示秒
- **7 款开源字体**：Bebas Neue（默认）/ Barlow Condensed / Oswald / Archivo Narrow / Anton / Orbitron / Share Tech Mono，均为 SIL OFL 授权；也可以导入自己的 `.ttf` / `.otf`
- **全部可调**：不透明度、大小、垂直位置、数字间距、组间距、翻页速度、帧率、圆角、牌面浓度、边框、中缝线 —— 都是滑块，支持两位小数直接输入
- **默认纯黑单色背景**：最省资源；想更好看可以切网格渐变（8 套预设，参考 WLED），设置页会提示开销
- **解锁后自动回收内存**

---

# Clock Lock Screen 0.8.7

Your AI agent is still running, a download is halfway done, a script hasn't finished — you step away, but the PC must not sleep and nobody should touch your screen.

## Download

`ClockLockScreen_v0.8.7_win_x64.zip` — unzip and run `ClockLockScreen.exe`.

- Requires [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- Requires [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on most Windows 11)
- Administrator rights are requested on first run (locking and global hotkeys)
- Keep `fonts/`, `wwwroot/` and `WebView2Loader.dll` next to the executable
- Toggle autostart off and on again after upgrading, so the scheduled task points at the new path

## Changes in this release

- **New: separate digit gap and group gap** — one controls the space between the two digits of a group, the other the space between hh / mm / ss; both are sliders (0–80 / 0–160 px) and apply live
- **Fixed: main window collapsing into a thin strip** — in some cases the window was restored at the system minimum size (160×39), leaving the title bar text stacked vertically and the content with a scrollbar. Size is now validated on startup and on every restore, and snapped back to 800×600 centred; a minimum size also guards against it

## Features

- **Three lock appearances**: fully transparent / adjustable black overlay / flip clock, with separate opacity for background and clock
- **Flip animation**: four-layer clipping — top half falls, bottom half rises, no ghosting while squashing; optional seconds
- **7 bundled fonts**: Bebas Neue (default) / Barlow Condensed / Oswald / Archivo Narrow / Anton / Orbitron / Share Tech Mono, all SIL OFL; import your own `.ttf` / `.otf` too
- **Everything adjustable**: opacity, size, vertical position, digit gap, group gap, flip speed, frame rate, corner radius, card fill, border, seam line — all sliders, with two-decimal direct input
- **Solid black by default**: the lightest option; switch to mesh gradients (8 presets inspired by WLED) if you want more, the settings page warns about the extra cost
- **Memory is reclaimed automatically after unlocking**

---

完整说明见 [README](https://github.com/ruilin365/ClockLockScreen#readme) ｜ Full documentation in the [README](https://github.com/ruilin365/ClockLockScreen#readme)
