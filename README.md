# 合作视角 Coop View
允许在合作模式中在第二显示器给另一个角色一个独立的视角。反馈至GitHub。

#### 如果没有两个显示器，可以用Parsec-vdd（不是Parsec。只有Parsec不算两个显示器！）创建虚拟显示器，并将画面通过Parsec或Sunshine传输给你的伙伴。

#### 对于远程串流，不支持Steam远程同乐（永远不会支持！），支持Parsec、Sunshine等可以切换传输屏幕的串流软件（自发布起就支持）。

#### 先连接显示器，并设置为扩展模式再开游戏。推荐以16:9显示，尤其是1920x1080、1440x810、960x540。

#### 选项包括：
- 第二窗口启动分辨率
- 1P 相机： 主相机 / 第二相机 / 自动

#### 兼容Coop KB&M，系统光标在游戏中禁用，使用快捷键 Alt + Shift + X 或 Win 键来暂时启用系统光标，详见[Coop KB&M](https://thunderstore.io/c/enter-the-gungeon/p/KLEIROF/Coop_KBnM/)。

#### 对于低配设备，可以在设置里设置第二窗口启动分辨率来增加流畅度。需要重启游戏以生效。

远程串流需要进行**传输屏幕的选择**。

Parsec：Switch Display - 选择显示器

![Parsec_setting](https://i.ibb.co/jvv3Sfz/Parsec-setting.jpg)

Sunshine：配置 - Audio/Video - 输出名称（如果是Parsec-vdd，输出名称可以在该软件中查看）

![Sunshine_setting](https://i.ibb.co/Cn3frNM/Sunshine-setting.png)

![Sunshine_setting_2](https://i.ibb.co/zNXww0C/Sunshine-setting-2.png)

### 默认快捷键：

####	Ctrl + Shift + P   ： 切换成下一个窗口位置预设（共3个）。
####	Ctrl + Shift + \[   ： 将第二窗口在全屏和窗口化之间切换。
####	Ctrl + Shift + \]   ： 将第二窗口全屏显示在下一个显示器上。
####	Ctrl + Shift + -   ： 将主窗口在全屏和窗口化之间切换。
####	Ctrl + Shift + =   ： 将主窗口全屏显示在下一个显示器上。


### 命令说明：
### \[方括号中的内容可以省略。\]
####	shortcutkey [shortcut_key_ID]
#####		- 设置自定义快捷键。
#####		- 快捷键ID应在参数中从1到5指定，如“shortcutkey 1”.
#####		- 当前快捷键及其快捷键ID会被显示作为提示。

####	switchpreset \[preset_index\]
#####		- 切换成窗口位置预设。
#####		- 预设索引可以在参数中指定，从0到2，如“switchpreset 2”。
######			- preset_index = 0 ： 主窗口在显示器1全屏，第二窗口在显示器2全屏。
######			- preset_index = 1 ： 主窗口在显示器2全屏，第二窗口在显示器1全屏。
######			- preset_index = 2 ： 主窗口在显示器1窗口化，第二窗口在显示器1窗口化。

####	secondwindow fullscreen [monitor_index]
#####		- 将第二窗口切换成全屏模式。
#####		- 目标显示器可以在参数中指定，如“secondwindow fullscreen 1”。

####	secondwindow windowed [width] [height]
#####		- 将第二窗口切换成窗口模式。
#####		- 窗口大小可以在参数中指定，如“secondwindow windowed 1920 1080”。

####	mainwindow fullscreen [monitor_index]
#####		- 将主窗口切换成全屏模式。
#####		- 目标显示器可以在参数中指定，如“mainwindow fullscreen 1”。

####	mainwindow windowed [width] [height]
#####		- 将主窗口切换成窗口模式。
#####		- 窗口大小可以在参数中指定，如“mainwindow windowed 1920 1080”。



---

 

Allows another player to have an independent camera on the second monitor in coop mode. Submit issues on GitHub. 

#### If you don't have two display devices, you can create a virtual display device using Parsec-vdd (Not Parsec! Only Parsec doesn't count as two monitors!) and stream the screen to your partner through Parsec or Sunshine.

#### For remote streaming, Steam Remote Play is not supported (it will never be supported!), and streaming software such as Parsec and Sunshine that can switch streaming screens are supported (supported since released). 

#### Connect your monitor and set it to expand mode before starting the game. Recommend displaying at 16:9, especially 1920x1080, 1440x810, 960x540.

#### The options include:
- Second Window Startup Resolution
- 1P Camera:  Main Camera / Second Camera / Auto

#### Compatible with Coop KB&M. The system cursor is disabled in the game, Use shortcut key Alt + Shift + X or Win to temporarily enable the system cursor. See [Coop KB&M](https://thunderstore.io/c/enter-the-gungeon/p/KLEIROF/Coop_KBnM/) for details.

#### For low-end devices, the second window startup resolution can be set in the settings to increase smoothness. The game needs to be restarted to take effect.

#### Need to **select the streaming screen** for remote streaming.

Parsec: Switch Display - Choose your display

![Parsec_setting](https://i.ibb.co/jvv3Sfz/Parsec-setting.jpg)

Sunshine: Configuration - Audio/Video - Output Name (if Parsec vdd, the output name can be viewed in the software)

![Sunshine_setting](https://i.ibb.co/Cn3frNM/Sunshine-setting.png)

![Sunshine_setting_2](https://i.ibb.co/zNXww0C/Sunshine-setting-2.png)

### Default Shortcut keys:

####	Ctrl + Shift + P   :  Switch to the next window position preset (3 in total).
####	Ctrl + Shift + \[   :  Switch the second window between fullscreen and windowed mode.
####	Ctrl + Shift + \]   :  Fullscreen the second window on the next monitor.
####	Ctrl + Shift + -   :  Switch the main window between fullscreen and windowed mode.
####	Ctrl + Shift + =   :  Fullscreen the main window on the next monitor.


### Command list:
### \[The content in square brackets can be omitted. \]
####	shortcutkey [shortcut_key_ID]
#####		- Set custom shortcut keys.
#####		- The shortcut key ID should be specified from 1 to 5 in the parameters, like 'shortcutkey 1'.
#####		- Current shortcut keys and its shortcut key ID will be displayed to give you a hint.

####	switchpreset \[preset_index\]
#####		- Switch to window position presets.
#####		- The preset index can be specified from 0 to 2 in the parameters, like 'switchpreset 2'.
######			- preset_index = 0 ： The main window fullscreen on monitor 1, the second window fullscreen on monitor 2.
######			- preset_index = 1 ： The main window fullscreen on monitor 2, the second window fullscreen on monitor 1.
######			- preset_index = 2 ： The main window windowed on monitor 1, the second window windowed on monitor 1.

####	secondwindow fullscreen [monitor_index]
#####		- Switch the second window to fullscreen mode.
#####		- The target monitor can be specified in the parameters, like 'secondwindow fullscreen 1'.

####	secondwindow windowed [width] [height]
#####		- Switch the second window to windowed mode.
#####		- The window size can be specified in the parameters, like 'secondwindow windowed 1920 1080'.

####	mainwindow fullscreen [monitor_index]
#####		- Switch the main window to fullscreen mode.
#####		- The target monitor can be specified in the parameters, like 'mainwindow fullscreen 1'.

####	mainwindow windowed [width] [height]
#####		- Switch the main window to windowed mode.
#####		- The window size can be specified in the parameters, like 'mainwindow windowed 1920 1080'.



