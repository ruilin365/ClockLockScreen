# 时钟锁屏 0.8.11

离开电脑一会儿，AI Agent 还在跑、下载还没完、脚本还没结束 —— 既不能让电脑休眠，又不想让别人动你的屏幕。

## 下载

`ClockLockScreen_v0.8.11_win_x64.zip` —— 解压后直接运行 `ClockLockScreen.exe`。

- 需要 [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- 需要 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Win11 通常已内置）
- 首次运行请求管理员权限（锁屏与全局快捷键需要）
- `fonts/`、`wwwroot/`、`WebView2Loader.dll` 必须和 exe 放在同一目录
- 换版本时重新拨一次「开机启动」开关，计划任务才会指向新路径

> 从 0.8.7 直接升级即可。曾短暂出现的 0.8.8 / 0.8.9 有启动闪退问题，已撤下，不用管。
> 老配置会自动迁移：原来的「时钟背景」会变成「全屏遮罩」的配色，观感不变。

---

## 相对 0.8.10 的改动

- **实时取景分辨率可调** —— 以前实时模糊固定按 1/4 分辨率取屏，现在「屏幕模糊 → 模糊方式：实时」下面多了一个滑块，10%–100% 可调，默认 50%
  - 调高更清晰、更跟手，代价是每帧耗时会涨：1920×1080 下 50% 约 50ms/帧，100% 约 160ms/帧
  - 不建议低于 40%：降采样时缩放会丢掉高频细节，窗口边缘会出现彩色镶边。默认值定在 50% 就是为了避开这一点
- **README 增加真实桌面效果图** —— 不再只有纯色背景的示意图，能直接看到壁纸被模糊后的样子

---

## 相对 0.8.7 升级了什么

### 1. 新增屏幕模糊（毛玻璃）

锁屏背后不再是纯色，而是屏幕本身的模糊影像，强度 0–100% 可调，和遮罩、时钟互不干扰。两种方式：

- **定格** —— 锁屏那一刻截一张，之后画面不动。隐私最好、最省 CPU，模糊半径随便调。默认就是这个。
- **实时** —— 按 1–10fps 重新取景，背后有日志在滚、有进度条在走都能隐约看出来。适合「人在旁边，但想确认任务还在跑」。

**为什么不直接用 Windows 自带的亚克力/云母材质**：DWM 那套材质必然带一层半透明着色 —— `GradientColor` 的 alpha 同时就是模糊浓度，压到 0 就直接不模糊了，强度也不可调，等于做不到「纯模糊」。

这里走的是另一条路：把锁屏窗口标记成「不参与截屏」（`WDA_EXCLUDEFROMCAPTURE`），于是全屏截取拿到的就是窗口背后的真实画面，每帧降采样（比例可调）后再做盒式模糊。好处是没有底色、强度真可调；代价是每次取屏要 20–30ms 加模糊时间，默认 3fps，对整机 CPU 影响很小。

### 2. 锁屏画面从「三选一」改成三层叠加

以前是下拉框选一个：完全透明 / 黑色遮罩 / 翻页时钟。现在拆成三层，各自独立开关，可以任意组合：

```
时钟（最上）   ← 只剩数字和牌面
遮罩（中间）   ← 铺满整屏的一层颜色，半透明时透出下面的模糊
模糊（最底）   ← 屏幕取景的模糊影像
```

全关就是完全透明。典型组合：**模糊 + 无遮罩 + 半透明时钟**（隐私最好看）；**模糊 + 黑色 20% + 时钟**（压一点亮度又不遮住模糊）。

### 3. 遮罩可以指定任意颜色

原来的「时钟背景」本质上就是一层铺满整屏的颜色，现在归到遮罩名下 —— 纯色 / 网格渐变、8 套预设配色、自定义两个颜色、一键互换、渐变角度与流动速度、不透明度，全在遮罩里调。时钟自己不再带任何背景。

### 4. 新增：首次引导设置锁屏快捷键

快捷键一直没有默认值（避免和你别的软件抢按键），但以前不给默认值也不提示，重置设置或全新安装的人会以为程序坏了：既锁不了屏，也不知道该干什么。现在：

- 没有快捷键时，程序**不再缩到托盘**，直接把窗口露出来
- 弹一个「先设一个锁屏快捷键」对话框，可以直接录组合键，设完立刻生效
- 对话框里同时带上「锁屏时显示什么」四个开关（屏幕模糊 / 全屏遮罩 / 翻页时钟 / 屏蔽任务管理器），首次使用就能看清会怎么盖住屏幕
- 关掉后界面顶部留一条提示，点「现在设置」随时能补；自动弹窗只出现一次

### 5. 屏蔽任务管理器改成开关

以前写死在锁屏流程里，一锁屏必然禁用任务管理器。现在是一项开关，默认开启（行为和以前一致）。走的是 Windows 注册表策略 `DisableTaskMgr`，**部分杀毒软件会把它当成恶意改动拦截**，设置页和引导页都标注了这一点；万一被拦，锁屏照样生效，只是任务管理器还能打开。

### 6. 新增：恢复默认外观

一键把模糊、遮罩、时钟、状态提示恢复出厂值。范围刻意限定，密码、锁屏快捷键、自动锁定、开机启动、语言、主题、自定义字体都不动。

### 7. 稳定性与其他

- **崩溃日志** —— 未捕获异常会写到配置目录的 `crash.log`，闪退至少能查了
- **「重置程序设置」会一并关掉开机启动** —— 自启存在计划任务里，不在配置文件里，以前只删配置，计划任务还留着
- **预览里的模糊正确显示** —— 预览不走锁屏流程，以前没有底图导致模糊层是空的；现在没有底图会现场补截，并且预览期间把设置窗口也排除出取景，看到的画面和真实锁屏一致
- **默认值调整** —— 模糊强度 8、遮罩黑色 35%、帧率 90fps、时钟缩放 130%、显示秒、牌面圆角 20、牌面浓度 90

## 其余功能（0.8.7 已有）

- **翻页动画**：四层裁剪结构，上半翻落、下半翻起，压扁过程不叠影；数字间距与时分秒间距分开调
- **7 款开源字体**：Bebas Neue（默认）/ Barlow Condensed / Oswald / Archivo Narrow / Anton / Orbitron / Share Tech Mono，均为 SIL OFL 授权；也可以导入自己的 `.ttf` / `.otf`
- **全部可调**：模糊强度、遮罩不透明度、时钟不透明度、大小、垂直位置、数字间距、组间距、翻页速度、帧率（10–240fps）、圆角、牌面浓度、边框、中缝线 —— 都是滑块，支持两位小数直接输入
- **配色**：8 套网格渐变配色（参考 WLED）、12 套光条配色，支持动态流动
- **预览模式**：不锁屏，把效果叠在桌面最上层，边调边看
- **解锁后自动回收内存**

---

# Clock Lock Screen 0.8.11

Your AI agent is still running, a download is halfway done, a script hasn't finished — you step away, but the PC must not sleep and nobody should touch your screen.

## Download

`ClockLockScreen_v0.8.11_win_x64.zip` — unzip and run `ClockLockScreen.exe`.

- Requires [.NET 8 Desktop Runtime](https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe)
- Requires [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on most Windows 11)
- Administrator rights are requested on first run (locking and global hotkeys)
- Keep `fonts/`, `wwwroot/` and `WebView2Loader.dll` next to the executable
- Toggle autostart off and on again after upgrading, so the scheduled task points at the new path

> Upgrading straight from 0.8.7 is fine. Versions 0.8.8 and 0.8.9 crashed on launch and have been withdrawn — ignore them.
> Existing settings migrate automatically: the old "clock background" becomes the "full-screen mask" colour, so nothing looks different.

---

## Changes since 0.8.10

- **Live capture resolution is adjustable** — live blur used to always capture at 1/4 resolution. There is now a slider under "Screen blur → mode: live", 10%–100%, 50% by default
  - Higher means sharper and more responsive, at the cost of per-frame time: at 1920x1080 about 50 ms per frame at 50%, about 160 ms at 100%
  - Going below ~40% is not advisable: downscaling drops high-frequency detail and coloured fringing appears along window edges — which is exactly why the default sits at 50%
- **A real-desktop blur screenshot in the README** — no longer only flat-colour mockups; you can see what a blurred wallpaper actually looks like

---

## What changed since 0.8.7

### 1. New: screen blur (frosted glass)

The lock screen now shows a blurred image of the screen itself instead of a flat colour. Strength 0–100%, independent of the mask and the clock. Two modes:

- **Frozen** — one snapshot at lock time, then it stays still. Best privacy, lowest CPU, blur radius fully adjustable. This is the default.
- **Live** — re-captures at 1–10 fps, so scrolling logs or a moving progress bar behind stay faintly visible. For "I'm nearby but want to confirm the job is still running".

**Why not use DWM acrylic/mica**: the built-in material always carries a translucent tint — the alpha of `GradientColor` doubles as the blur strength, so setting it to zero simply disables the blur, and the strength is not adjustable. That rules out a pure, tunable blur.

Instead the lock window is marked `WDA_EXCLUDEFROMCAPTURE`, so a full-screen grab returns what is actually *behind* it; each frame is downscaled (ratio configurable) and box-blurred. No tint, truly adjustable strength. The cost is 20–30 ms per capture plus the blur — 3 fps by default, negligible on a multi-core machine.

### 2. From "pick one of three" to three stacked layers

The old dropdown chose one of: fully transparent / black overlay / flip clock. It is now three layers with independent switches that combine freely:

```
Clock (top)   ← digits and cards only
Mask  (mid)   ← one colour layer covering the screen; translucent means the blur shows through
Blur  (base)  ← the blurred screen capture
```

All off means fully transparent. Typical combinations: **blur + no mask + translucent clock** (best looking privacy), or **blur + 20% black + clock** (a little contrast without killing the frost).

### 3. The mask takes any colour

The old "clock background" was really just a full-screen colour layer, so it now lives under the mask: solid or mesh gradient, 8 preset palettes, two custom colours with a swap button, gradient angle and flow speed, opacity. The clock no longer carries any background of its own.

### 4. New: first-run guide for the lock hotkey

The hotkey has never had a default (so it won't fight your other apps), but without one and without a hint, a fresh install or a settings reset looked broken: you couldn't lock, and had no idea what to do. Now:

- With no hotkey, the app **no longer hides in the tray** — the window comes up
- A "Set a lock hotkey" dialog opens where you can record the combination; it applies immediately
- The dialog also carries the four "what the lock screen shows" switches (blur / mask / flip clock / block Task Manager), so a first-time user sees what will cover the screen
- A banner stays at the top afterwards, with a "Set it up" button; the dialog auto-opens only once

### 5. Blocking Task Manager is now a switch

Previously hard-coded, so locking always killed Task Manager. Now it is a switch, on by default (same behaviour as before). It flips a Windows registry policy (`DisableTaskMgr`), which **some antivirus products flag as malicious** — both the settings page and the guide say so. If it gets blocked, locking still works; Task Manager just stays available.

### 6. New: restore defaults

One click resets blur, mask, clock and status indicator to factory values. Deliberately scoped: your password, lock hotkey, auto-lock, autostart, language, theme and imported fonts are untouched.

### 7. Stability and the rest

- **Crash log** — unhandled exceptions are appended to `crash.log` in the config directory, so a crash is at least diagnosable
- **"Reset settings" also turns autostart off** — autostart lives in a scheduled task, not the config file; reset used to leave the task behind
- **Blur now shows in preview** — preview doesn't go through the lock flow, so no snapshot existed and the blur layer stayed empty. It now captures on demand, and the settings window is excluded from the capture so preview matches the real lock screen
- **New defaults** — blur strength 8, black mask 35%, 90 fps, clock scale 130%, seconds shown, corner radius 20, card fill 90

## Already in 0.8.7

- **Flip animation**: four-layer clipping — top half falls, bottom half rises, no ghosting while squashing; separate digit gap and group gap
- **7 bundled fonts**: Bebas Neue (default) / Barlow Condensed / Oswald / Archivo Narrow / Anton / Orbitron / Share Tech Mono, all SIL OFL; import your own `.ttf` / `.otf`
- **Everything adjustable**: blur strength, mask opacity, clock opacity, size, vertical position, digit gap, group gap, flip speed, frame rate (10–240 fps), corner radius, card fill, border, seam line — all sliders, with two-decimal direct input
- **Colours**: 8 mesh-gradient presets (inspired by WLED), 12 indicator palettes, with optional flow
- **Preview mode**: overlays the effect on your desktop without locking, so you can tune while watching
- **Memory is reclaimed automatically after unlocking**

---

完整说明见 [README](https://github.com/ruilin365/ClockLockScreen#readme) ｜ Full documentation in the [README](https://github.com/ruilin365/ClockLockScreen#readme)
