# 时钟锁屏 0.8.8

离开电脑一会儿，AI Agent 还在跑、下载还没完、脚本还没结束 —— 既不能让电脑休眠，又不想让别人动你的屏幕。

## 下载

`ClockLockScreen_v0.8.8_win_x64.zip` —— 解压后直接运行 `ClockLockScreen.exe`。

- 需要 [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- 需要 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Win11 通常已内置）
- 首次运行请求管理员权限（锁屏与全局快捷键需要）
- `fonts/`、`wwwroot/`、`WebView2Loader.dll` 必须和 exe 放在同一目录
- 换版本时重新拨一次「开机启动」开关，计划任务才会指向新路径

## 这一版的改动

- **新增：屏幕模糊（毛玻璃）** —— 锁屏背后不再是纯色，而是屏幕本身的模糊影像。强度 0–100% 可调，和遮罩、时钟互不干扰
  - **定格**：锁屏那一刻截一张，之后画面不动。隐私最好、最省 CPU，模糊半径随便调
  - **实时**：按 1–10fps 重新取景，背后有日志在滚、有进度条在走，都能隐约看出来 —— 适合「人在旁边但想确认任务还在跑」
- **锁屏画面改成三层叠加**：从下到上是「模糊 → 遮罩 → 时钟」，三层各自独立开关，可以任意组合，全关就是完全透明。原来的「完全透明 / 黑色遮罩 / 翻页时钟」三选一下拉框已移除
- **遮罩可以指定任意颜色** —— 原来的「时钟背景」本质上就是一层全屏颜色，现在归到遮罩名下，纯色 / 网格渐变、预设配色、流动、角度、不透明度都在这里调，老配置的配色会自动迁移过来
- **新增：恢复默认外观** —— 一键把模糊、遮罩、时钟、状态提示恢复出厂值；密码、快捷键、自动锁定不受影响
- **修复：预览里模糊不生效** —— 预览不经过锁屏流程，从来没有截过底图，导致模糊层是空的。现在没有底图会现场补截一张；同时预览期间把设置窗口也排除出取景，看到的画面和真实锁屏一致

### 关于实时模糊的实现

Windows 自带的 DWM 亚克力/云母材质**必然带一层半透明着色**：`GradientColor` 的 alpha 同时就是模糊浓度，压到 0 就直接不模糊了，强度也不可调，所以没有采用。

这里走的是另一条路：把锁屏窗口标记成「不参与截屏」（`WDA_EXCLUDEFROMCAPTURE`），于是全屏截取拿到的是窗口背后的真实画面，每帧缩到 1/4 再做盒式模糊。好处是没有底色、强度真可调；代价是每次取屏约 20–30ms，默认 2fps，整机 CPU 占用约 0.5%。

## 其余功能

- **翻页动画**：四层裁剪结构，上半翻落、下半翻起，压扁过程不叠影；可显示秒；数字间距与时分秒间距分开调
- **7 款开源字体**：Bebas Neue（默认）/ Barlow Condensed / Oswald / Archivo Narrow / Anton / Orbitron / Share Tech Mono，均为 SIL OFL 授权；也可以导入自己的 `.ttf` / `.otf`
- **全部可调**：模糊强度、遮罩不透明度、时钟不透明度、大小、垂直位置、数字间距、组间距、翻页速度、帧率（10–240fps）、圆角、牌面浓度、边框、中缝线 —— 都是滑块，支持两位小数直接输入
- **配色**：遮罩默认黑色 35%；另有 8 套网格渐变配色（参考 WLED）、12 套光条配色，支持动态流动
- **预览模式**：不锁屏，把效果叠在桌面最上层，边调边看
- **解锁后自动回收内存**

---

# Clock Lock Screen 0.8.8

Your AI agent is still running, a download is halfway done, a script hasn't finished — you step away, but the PC must not sleep and nobody should touch your screen.

## Download

`ClockLockScreen_v0.8.8_win_x64.zip` — unzip and run `ClockLockScreen.exe`.

- Requires [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- Requires [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on most Windows 11)
- Administrator rights are requested on first run (locking and global hotkeys)
- Keep `fonts/`, `wwwroot/` and `WebView2Loader.dll` next to the executable
- Toggle autostart off and on again after upgrading, so the scheduled task points at the new path

## Changes in this release

- **New: screen blur (frosted glass)** — instead of a flat colour, the lock screen now shows a blurred image of the screen itself. Strength 0–100%, independent of the mask and the clock
  - **Frozen**: one snapshot at lock time, then it stays still. Best privacy, lowest CPU, blur radius fully adjustable
  - **Live**: re-captures at 1–10 fps, so scrolling logs or a moving progress bar behind stay faintly visible — useful when you want to confirm a job is still running
- **Three stacked layers**: bottom to top — blur → mask → clock. Each has its own switch and they combine freely; all off means fully transparent. The old "fully transparent / black overlay / flip clock" dropdown is gone
- **The mask takes any colour** — the old "clock background" was really just a full-screen colour layer, so it now lives under the mask: solid or mesh gradient, preset palettes, flow, angle and opacity. Existing colours are migrated automatically
- **New: restore defaults** — one click to reset blur, mask, clock and status indicator; password, hotkeys and auto-lock are untouched
- **Fixed: blur did nothing in preview** — preview never goes through the lock flow, so no snapshot had ever been taken and the blur layer stayed empty. It now captures on demand when no snapshot exists, and the settings window itself is excluded from the capture so preview matches the real lock screen

### Why not DWM acrylic

The built-in DWM acrylic/mica material always carries a translucent tint: the alpha of `GradientColor` doubles as the blur strength, so setting it to zero simply disables the blur — and the strength is not adjustable. That rules it out here.

Instead the lock window is marked `WDA_EXCLUDEFROMCAPTURE`, so a full-screen grab returns whatever is *behind* the window; each frame is downscaled to 1/4 and box-blurred. No tint, truly adjustable strength. The cost is roughly 20–30 ms per capture — 2 fps by default, about 0.5% CPU overall.

## Features

- **Flip animation**: four-layer clipping — top half falls, bottom half rises, no ghosting while squashing; optional seconds; separate digit gap and group gap
- **7 bundled fonts**: Bebas Neue (default) / Barlow Condensed / Oswald / Archivo Narrow / Anton / Orbitron / Share Tech Mono, all SIL OFL; import your own `.ttf` / `.otf` too
- **Everything adjustable**: blur strength, mask opacity, clock opacity, size, vertical position, digit gap, group gap, flip speed, frame rate (10–240 fps), corner radius, card fill, border, seam line — all sliders, with two-decimal direct input
- **Colours**: black mask at 35% by default; 8 mesh-gradient presets (inspired by WLED) and 12 indicator palettes, with optional flow
- **Preview mode**: overlays the effect on your desktop without locking, so you can tune while watching
- **Memory is reclaimed automatically after unlocking**

---

完整说明见 [README](https://github.com/ruilin365/ClockLockScreen#readme) ｜ Full documentation in the [README](https://github.com/ruilin365/ClockLockScreen#readme)
