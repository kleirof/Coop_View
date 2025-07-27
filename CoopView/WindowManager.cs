using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Screen = UnityEngine.Screen;

namespace CoopView
{
    static class WindowManager
    {
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint EVENT_SYSTEM_RESTORE = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint WM_SETICON = 0x0080;
        private const int GCLP_HICON = -14;
        private const int GCLP_HICONSM = -34;
        private const int SM_CMONITORS = 80;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SPI_GETWORKAREA = 0x0030;

        private static IntPtr ICON_SMALL = new IntPtr(0);
        private static IntPtr ICON_BIG = new IntPtr(1);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadIcon(IntPtr hWnd, IntPtr lpIconName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplayMonitors(IntPtr hdcMonitor, IntPtr lprcMonitor, MonitorEnumProc lpEnumFunc, IntPtr dwData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, ref RECT lpvParam, int fuWinIni);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static WinEventDelegate winEventDelegate;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private static IntPtr m_SecondWindow;
        private static IntPtr m_MainWindow;

        internal static int referenceSecondWindowWidth;
        internal static int referenceSecondWindowHeight;

        private static RECT secondWindowMonitorRect;
        private static int secondWindowMonitorIndex = 0;
        private static int destSecondWindowMonitorIndex = 0;
        private static int currentSecondWindowMonitorIndex = 2;

        private static RECT mainWindowMonitorRect;
        private static int mainWindowMonitorIndex = 0;
        private static int destMainWindowMonitorIndex = 0;
        private static int currentMainWindowMonitorIndex = 1;

        private static IntPtr[] windowHookIDs = new IntPtr[4];
        private static RECT lastSecondWindowRect;
        private static RECT lastMainWindowRect;

        private static int presetStatus = 1;
        private static bool secondWindowFullscreenStatus = true;
        private static bool mainWindowFullscreenStatus = true;

        internal static IntPtr SecondWindow
        {
            get
            {
                if(m_SecondWindow == IntPtr.Zero || !IsWindow(m_SecondWindow))
                    m_SecondWindow = FindWindow("UnityWndClass", "Coop View Second Window");
                return m_SecondWindow;
            }
        }

        internal static IntPtr MainWindow
        {
            get
            {
                if(m_MainWindow == IntPtr.Zero || !IsWindow(m_MainWindow))
                    m_MainWindow = FindWindow("UnityWndClass", "Enter the Gungeon");
                return m_MainWindow;
            }
        }

        internal static int SecondWindowWidth
        {
            get
            {
                if (IsWindowMinimized(SecondWindow))
                {
                    return lastSecondWindowRect.right - lastSecondWindowRect.left;
                }
                else
                {
                    RECT windowRect = new RECT();
                    GetClientRect(SecondWindow, ref windowRect);
                    return windowRect.right - windowRect.left;
                }
            }
        }

        internal static int SecondWindowHeight
        {
            get
            {
                if (IsWindowMinimized(SecondWindow))
                {
                    return lastSecondWindowRect.bottom - lastSecondWindowRect.top;
                }
                else
                {
                    RECT windowRect = new RECT();
                    GetClientRect(SecondWindow, ref windowRect);
                    return windowRect.bottom - windowRect.top;
                }
            }
        }

        private static int MainMonotorScreenWidth
        {
            get
            {
                RECT workArea = new RECT();
                SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);
                return workArea.right - workArea.left;
            }
        }

        private static int MainMonotorScreenHeight
        {
            get
            {
                RECT workArea = new RECT();
                SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);
                return workArea.bottom - workArea.top;
            }
        }

        private static int MonitorNumber
        {
            get
            {
                return GetSystemMetrics(SM_CMONITORS);
            }
        }

        internal static int startupWidth
        {
            get
            {
                if (OptionsManager.secondWindowStartupResolution != 0)
                {
                    int width = OptionsManager.secondWindowStartupResolution * 480;
                    int height = OptionsManager.secondWindowStartupResolution * 270;
                    if (width <= Display.displays[1].systemWidth && height <= Display.displays[1].systemHeight)
                        return width;
                }

                if (Display.displays[1].systemWidth >= 1920 && Display.displays[1].systemHeight >= 1080)
                    return 1920;
                else if (Display.displays[1].systemWidth >= 1440 && Display.displays[1].systemHeight >= 810)
                    return 1440;
                else if (Display.displays[1].systemWidth >= 960 && Display.displays[1].systemHeight >= 540)
                    return 960;
                else
                    return 480;
            }
        }

        internal static int startupHeight
        {
            get
            {
                if (OptionsManager.secondWindowStartupResolution != 0)
                {
                    int width = OptionsManager.secondWindowStartupResolution * 480;
                    int height = OptionsManager.secondWindowStartupResolution * 270;
                    if (width <= Display.displays[1].systemWidth && height <= Display.displays[1].systemHeight)
                        return height;
                }

                if (Display.displays[1].systemWidth >= 1920 && Display.displays[1].systemHeight >= 1080)
                    return 1080;
                else if (Display.displays[1].systemWidth >= 1440 && Display.displays[1].systemHeight >= 810)
                    return 810;
                else if (Display.displays[1].systemWidth >= 960 && Display.displays[1].systemHeight >= 540)
                    return 540;
                else
                    return 270;
            }
        }

        internal static bool IsCaptionMissing(IntPtr hWnd)
        {
            IntPtr style = GetWindowLongPtr(hWnd, GWL_STYLE);
            return ((int)style & WS_CAPTION) == 0;
        }

        private static bool IsWindowMinimized(IntPtr hWnd)
        {
            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(wp);
            GetWindowPlacement(hWnd, ref wp);
            return wp.showCmd == SW_SHOWMINIMIZED;
        }

        internal static void SetSecondWindowText()
        {
            IntPtr window = FindWindow("UnityWndClass", "Unity Secondary Display");
            SetWindowText(window, "Coop View Second Window");
        }

        internal static void SwitchSecondWindowToFullscreen(string[] args)
        {
            if (args.Length > 0)
            {
                destSecondWindowMonitorIndex = int.Parse(args[0]);
                if (destSecondWindowMonitorIndex <= 0 || destSecondWindowMonitorIndex > MonitorNumber)
                    destSecondWindowMonitorIndex = MonitorNumber;
            }
            else
                destSecondWindowMonitorIndex = MonitorNumber;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, SecondWindowMonitorEnumCallback, IntPtr.Zero);

            if (secondWindowMonitorIndex == destSecondWindowMonitorIndex)
            {
                IntPtr window = SecondWindow;
                if (window != IntPtr.Zero)
                {
                    bool unpause = GameManager.HasInstance ? !GameManager.Instance.IsPaused : false;
                    ShowWindow(window, SW_RESTORE);
                    SetWindowLongPtr(window, GWL_STYLE, (IntPtr)WS_POPUP);
                    SetWindowPos(window, IntPtr.Zero, secondWindowMonitorRect.left, secondWindowMonitorRect.top,
                                  secondWindowMonitorRect.right - secondWindowMonitorRect.left,
                                  secondWindowMonitorRect.bottom - secondWindowMonitorRect.top,
                                  SWP_NOACTIVATE | SWP_NOZORDER | SWP_SHOWWINDOW);
                    currentSecondWindowMonitorIndex = destSecondWindowMonitorIndex;
                    secondWindowFullscreenStatus = true;
                    if (unpause)
                    {
                        try
                        {
                            GameManager.Instance.Unpause();
                        }
                        catch { }
                    }
                    ETGModConsole.Log($"Successfully switched second window to fullscreen, target monitor = {destSecondWindowMonitorIndex}.");
                }
                else
                {
                    Debug.LogError("Second window not found.");
                }
            }
            else
            {
                Debug.LogError("Target monitor not found.");
            }
            secondWindowMonitorIndex = 0;
        }

        internal static void SwitchSecondWindowToWindowed(string[] args)
        {
            IntPtr window = SecondWindow;
            if (window != IntPtr.Zero)
            {
                bool unpause = GameManager.HasInstance ? !GameManager.Instance.IsPaused : false;
                uint style = (uint)GetWindowLongPtr(window, GWL_STYLE);
                style |= WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                int exStyle = (int)GetWindowLongPtr(window, GWL_EXSTYLE);

                int height;
                int width;
                if (args.Length == 2)
                {
                    height = int.Parse(args[1]);
                    width = int.Parse(args[0]);
                }
                else
                {
                    height = SecondWindowHeight;
                    width = SecondWindowWidth;
                    if ((float)9 / 16 < ((float)height / (float)width))
                    {
                        height = (int)(width * (float)9 / 16);
                    }
                    else
                    {
                        width = (int)(height * (float)16 / 9);
                    }
                }

                RECT rect = new RECT { left = 0, top = 0, right = width, bottom = height };

                int fullWidth;
                int fullHeight;
                if (AdjustWindowRectEx(ref rect, (int)style, false, exStyle))
                {
                    fullWidth = rect.right - rect.left;
                    fullHeight = rect.bottom - rect.top;
                }
                else
                {
                    fullWidth = width;
                    fullHeight = height;
                }

                SetWindowLongPtr(window, GWL_STYLE, (IntPtr)style);
                CopyWindowIcon();

                SetWindowPos(window, IntPtr.Zero, 0, 0, fullWidth, fullHeight, SWP_FRAMECHANGED | SWP_NOMOVE);
                secondWindowFullscreenStatus = false;
                if (unpause)
                {
                    try
                    {
                        GameManager.Instance.Unpause();
                    }
                    catch { }
                }
                ETGModConsole.Log($"Successfully switched second window to windowed, window size = {width}x{height}.");
            }
            else
            {
                Debug.LogError("Second window not found.");
            }
        }

        internal static void SwitchSecondWindowFullScreenMonitor()
        {
            currentSecondWindowMonitorIndex++;
            if (currentSecondWindowMonitorIndex > MonitorNumber)
                currentSecondWindowMonitorIndex = 1;

            string[] args = { $"{currentSecondWindowMonitorIndex}" };
            SwitchSecondWindowToFullscreen(args);
        }

        internal static void SwitchSecondWindowFullscreenOrWindowed()
        {
            if (!secondWindowFullscreenStatus)
            {
                if (currentSecondWindowMonitorIndex > MonitorNumber)
                    currentSecondWindowMonitorIndex = 1;

                string[] args = { $"{currentSecondWindowMonitorIndex}" };
                SwitchSecondWindowToFullscreen(args);
            }
            else
            {
                string[] args = { };
                SwitchSecondWindowToWindowed(args);
            }
        }

        private static bool SecondWindowMonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            secondWindowMonitorIndex++;

            if (secondWindowMonitorIndex == destSecondWindowMonitorIndex)
            {
                secondWindowMonitorRect = lprcMonitor;
                return false;
            }

            return true;
        }

        internal static void RestoreMainWindowCaption()
        {
            IntPtr window = MainWindow;
            int style = (int)GetWindowLongPtr(window, GWL_STYLE);
            SetWindowLongPtr(window, GWL_STYLE, (IntPtr)(style | WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
        }

        internal static void SwitchMainWindowToWindowed(string[] args)
        {
            IntPtr window = MainWindow;
            if (window != IntPtr.Zero)
            {
                uint style = (uint)GetWindowLongPtr(window, GWL_STYLE);
                style |= WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                int exStyle = (int)GetWindowLongPtr(window, GWL_EXSTYLE);

                int height;
                int width;
                if (args.Length == 2)
                {
                    height = int.Parse(args[1]);
                    width = int.Parse(args[0]);
                }
                else
                {
                    height = Screen.height;
                    width = Screen.width;
                    if ((float)9 / 16 < ((float)height / (float)width))
                    {
                        height = (int)(width * (float)9 / 16);
                    }
                    else
                    {
                        width = (int)(height * (float)16 / 9);
                    }
                }

                RECT rect = new RECT { left = 0, top = 0, right = width, bottom = height };

                int fullWidth;
                int fullHeight;
                if (AdjustWindowRectEx(ref rect, (int)style, false, exStyle))
                {
                    fullWidth = rect.right - rect.left;
                    fullHeight = rect.bottom - rect.top;
                }
                else
                {
                    fullWidth = width;
                    fullHeight = height;
                }

                SetWindowLongPtr(window, GWL_STYLE, (IntPtr)style);

                SetWindowPos(window, IntPtr.Zero, 0, 0, fullWidth, fullHeight, SWP_FRAMECHANGED | SWP_NOMOVE);

                SetScreenMode(FullscreenMode.WINDOWED);
                RestoreMainWindowCaption();
                mainWindowFullscreenStatus = false;
                ETGModConsole.Log($"Successfully switched main window to windowed, window size = {width}x{height}.");
            }
            else
            {
                Debug.LogError("Main window not found.");
            }
        }

        internal static void SwitchMainWindowToFullscreen(string[] args)
        {
            if (args.Length > 0)
            {
                destMainWindowMonitorIndex = int.Parse(args[0]);
                if (destMainWindowMonitorIndex <= 0 || destMainWindowMonitorIndex > MonitorNumber)
                    destMainWindowMonitorIndex = MonitorNumber;
            }
            else
                destMainWindowMonitorIndex = MonitorNumber;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MainWindowMonitorEnumCallback, IntPtr.Zero);

            if (mainWindowMonitorIndex == destMainWindowMonitorIndex)
            {
                IntPtr window = MainWindow;
                if (window != IntPtr.Zero)
                {
                    ShowWindow(window, SW_RESTORE);
                    SetWindowLongPtr(window, GWL_STYLE, (IntPtr)WS_POPUP);
                    SetWindowPos(window, IntPtr.Zero, mainWindowMonitorRect.left, mainWindowMonitorRect.top,
                                  mainWindowMonitorRect.right - mainWindowMonitorRect.left,
                                  mainWindowMonitorRect.bottom - mainWindowMonitorRect.top,
                                  SWP_NOACTIVATE | SWP_NOZORDER | SWP_SHOWWINDOW);
                    currentMainWindowMonitorIndex = destMainWindowMonitorIndex;

                    Screen.fullScreen = false;
                    Resolution resolution = default(Resolution);
                    resolution.width = mainWindowMonitorRect.right - mainWindowMonitorRect.left;
                    resolution.height = mainWindowMonitorRect.bottom - mainWindowMonitorRect.top;
                    resolution.refreshRate = Screen.currentResolution.refreshRate;
                    BraveOptionsMenuItem.ResolutionManagerWin.TrySetDisplay(BraveOptionsMenuItem.WindowsResolutionManager.DisplayModes.Borderless, resolution, false, null);
                    mainWindowFullscreenStatus = true;

                    SetScreenMode(FullscreenMode.BORDERLESS);
                    ETGModConsole.Log($"Successfully switched main window to fullscreen, target monitor = {destMainWindowMonitorIndex}.");
                }
                else
                {
                    Debug.LogError("Main window not found.");
                }
            }
            else
            {
                Debug.LogError("Target monitor not found.");
            }
            mainWindowMonitorIndex = 0;
        }

        internal static void SwitchMainWindowFullScreenMonitor()
        {
            currentMainWindowMonitorIndex++;
            if (currentMainWindowMonitorIndex > MonitorNumber)
                currentMainWindowMonitorIndex = 1;

            string[] args = { $"{currentMainWindowMonitorIndex}" };
            SwitchMainWindowToFullscreen(args);
        }

        internal static void SwitchMainWindowFullscreenOrWindowed()
        {
            if (!mainWindowFullscreenStatus)
            {
                if (currentMainWindowMonitorIndex > MonitorNumber)
                    currentMainWindowMonitorIndex = 1;

                string[] args = { $"{currentMainWindowMonitorIndex}" };
                SwitchMainWindowToFullscreen(args);
            }
            else
            {
                string[] args = { };
                SwitchMainWindowToWindowed(args);
            }
        }

        internal static void CopyWindowIcon()
        {
            IntPtr mainWindow = MainWindow;
            IntPtr window = SecondWindow;
            IntPtr mainIconBig = GetClassLongPtr(mainWindow, GCLP_HICON);
            if (mainIconBig != IntPtr.Zero)
            {
                SendMessage(window, WM_SETICON, ICON_BIG, mainIconBig);
            }

            IntPtr mainIconSmall = GetClassLongPtr(mainWindow, GCLP_HICONSM);
            if (mainIconSmall != IntPtr.Zero)
            {
                SendMessage(window, WM_SETICON, ICON_SMALL, mainIconSmall);
            }
        }

        private static bool MainWindowMonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            mainWindowMonitorIndex++;

            if (mainWindowMonitorIndex == destMainWindowMonitorIndex)
            {
                mainWindowMonitorRect = lprcMonitor;
                return false;
            }

            return true;
        }

        internal static void InitWindowHook()
        {
            IntPtr window = SecondWindow;

            if (window == IntPtr.Zero)
            {
                Debug.LogError("Second window not found.");
                return;
            }

            uint processId = GetWindowProcessId(window);
            winEventDelegate = new WinEventDelegate(WinEventProc);
            windowHookIDs[0] = SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, winEventDelegate, processId, 0, WINEVENT_OUTOFCONTEXT);
            windowHookIDs[1] = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, winEventDelegate, processId, 0, WINEVENT_OUTOFCONTEXT);
            windowHookIDs[2] = SetWinEventHook(EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, winEventDelegate, processId, 0, WINEVENT_OUTOFCONTEXT);
            windowHookIDs[3] = SetWinEventHook(EVENT_SYSTEM_RESTORE, EVENT_SYSTEM_RESTORE, IntPtr.Zero, winEventDelegate, processId, 0, WINEVENT_OUTOFCONTEXT);
            ShowWindow(window, SW_RESTORE);
            GetClientRect(window, ref lastSecondWindowRect);

            window = MainWindow;
            ShowWindow(window, SW_RESTORE);
            GetClientRect(window, ref lastMainWindowRect);
        }

        internal static void ReleaseWindowHook()
        {
            foreach (var id in windowHookIDs)
            {
                if (id != IntPtr.Zero)
                {
                    UnhookWinEvent(id);
                }
            }
        }

        private static uint GetWindowProcessId(IntPtr hWnd)
        {
            uint processId;
            GetWindowThreadProcessId(hWnd, out processId);
            return processId;
        }

        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            RECT rect = new RECT();
            if (GetClientRect(hwnd, ref rect))
            {
                if(hwnd == MainWindow && !IsWindowMinimized(MainWindow))
                {
                    if (eventType == EVENT_OBJECT_LOCATIONCHANGE && IsCaptionMissing(hwnd) && GameManager.Options.CurrentPreferredFullscreenMode == GameOptions.PreferredFullscreenMode.WINDOWED)
                        ViewController.waitingForRestoringCaption = true;
                    if (rect.right - rect.left != lastMainWindowRect.right - lastMainWindowRect.left || rect.bottom - rect.top != lastMainWindowRect.bottom - lastMainWindowRect.top)
                    {
                        lastMainWindowRect = rect;
                        if (ViewController.resolutionOptionsMenuItem != null)
                            ViewController.resolutionOptionsMenuItem.HandleResolutionChanged(null, new Vector3(), new Vector3());
                        GameManager.Instance.StartCoroutine(ViewController.OnUpdateResolution());
                    }
                }

                if (hwnd == SecondWindow && !IsWindowMinimized(SecondWindow))
                {
                    if (rect.right - rect.left != lastSecondWindowRect.right - lastSecondWindowRect.left || rect.bottom - rect.top != lastSecondWindowRect.bottom - lastSecondWindowRect.top)
                    {
                        lastSecondWindowRect = rect;
                        GameManager.Instance.StartCoroutine(ViewController.OnUpdateResolution());
                    }
                }
            }
        }

        internal static void SwitchPreset(string[] args)
        {
            if (args.Length == 1)
            {
                int index = int.Parse(args[0]);
                if (index < 0 || index > 2)
                    presetStatus = (presetStatus + 1) % 3;
                else
                    presetStatus = index;
            }
            else
                presetStatus = (presetStatus + 1) % 3;
            ETGModConsole.Log($"Switched to preset {presetStatus}.");
            if (presetStatus == 2)
            {
                DisplaySideBySide();
            }
            else if (presetStatus == 0)
            {
                string[] args1 = { "1" };
                SwitchMainWindowToFullscreen(args1);

                string[] args2 = { "2" };
                SwitchSecondWindowToFullscreen(args2);
            }
            else if (presetStatus == 1)
            {
                string[] args1 = { "1" };
                SwitchSecondWindowToFullscreen(args1);

                string[] args2 = { "2" };
                SwitchMainWindowToFullscreen(args2);
            }
        }

        private static void DisplaySideBySide()
        {
            bool unpause = GameManager.HasInstance ? !GameManager.Instance.IsPaused : false;
            IntPtr window = SecondWindow;
            if (window != IntPtr.Zero)
            {
                ShowWindow(window, SW_RESTORE);
                uint style = (uint)GetWindowLongPtr(window, GWL_STYLE);
                style |= WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                int exStyle = (int)GetWindowLongPtr(window, GWL_EXSTYLE);

                int height;
                int width;
                
                width = MainMonotorScreenWidth / 2;
                height = (int)(width * 9f / 16);

                RECT rect = new RECT { left = 0, top = 0, right = width, bottom = height };

                int fullWidth;
                int fullHeight;
                if (AdjustWindowRectEx(ref rect, (int)style, false, exStyle))
                {
                    fullWidth = rect.right - rect.left;
                    fullHeight = rect.bottom - rect.top;
                }
                else
                {
                    fullWidth = width;
                    fullHeight = height;
                }
                int left = MainMonotorScreenWidth / 2;
                int top = (MainMonotorScreenHeight - fullHeight) / 2;

                if (top < 0)
                {
                    int additionalHeight = fullHeight - height;
                    height = MainMonotorScreenHeight - additionalHeight;
                    width = (int)(height * 16f / 9);

                    rect = new RECT { left = 0, top = 0, right = width, bottom = height };

                    if (AdjustWindowRectEx(ref rect, (int)style, false, exStyle))
                    {
                        fullWidth = rect.right - rect.left;
                        fullHeight = rect.bottom - rect.top;
                    }
                    else
                    {
                        fullWidth = width;
                        fullHeight = height;
                    }
                    left = MainMonotorScreenWidth / 2;
                    top = 0;
                }

                SetWindowLongPtr(window, GWL_STYLE, (IntPtr)style);
                CopyWindowIcon();

                SetWindowPos(window, IntPtr.Zero, left, top, fullWidth, fullHeight, SWP_FRAMECHANGED);
                SetWindowPos(window, IntPtr.Zero, left, top, fullWidth, fullHeight, SWP_FRAMECHANGED);
                secondWindowFullscreenStatus = false;

                SetScreenMode(FullscreenMode.WINDOWED);
                RestoreMainWindowCaption();
                
                ETGModConsole.Log($"Successfully switched second window to windowed, window size = {width}x{height}.");
            }
            else
            {
                Debug.LogError("Second window not found.");
            }

            window = MainWindow;
            if (window != IntPtr.Zero)
            {
                ShowWindow(window, SW_RESTORE);
                uint style = (uint)GetWindowLongPtr(window, GWL_STYLE);
                style |= WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                int exStyle = (int)GetWindowLongPtr(window, GWL_EXSTYLE);

                int height;
                int width;

                width = MainMonotorScreenWidth / 2;
                height = (int)(width * 9f / 16);

                RECT rect = new RECT { left = 0, top = 0, right = width, bottom = height };

                int fullWidth;
                int fullHeight;
                if (AdjustWindowRectEx(ref rect, (int)style, false, exStyle))
                {
                    fullWidth = rect.right - rect.left;
                    fullHeight = rect.bottom - rect.top;
                }
                else
                {
                    fullWidth = width;
                    fullHeight = height;
                }
                int left = 0;
                int top = (MainMonotorScreenHeight - fullHeight) / 2;

                if (top < 0)
                {
                    int additionalHeight = fullHeight - height;
                    height = MainMonotorScreenHeight - additionalHeight;
                    width = (int)(height * 16f / 9);

                    rect = new RECT { left = 0, top = 0, right = width, bottom = height };

                    if (AdjustWindowRectEx(ref rect, (int)style, false, exStyle))
                    {
                        fullWidth = rect.right - rect.left;
                        fullHeight = rect.bottom - rect.top;
                    }
                    else
                    {
                        fullWidth = width;
                        fullHeight = height;
                    }
                    left = MainMonotorScreenWidth / 2 - fullWidth;
                    top = 0;
                }

                SetWindowLongPtr(window, GWL_STYLE, (IntPtr)style);

                SetWindowPos(window, IntPtr.Zero, left, top, fullWidth, fullHeight, SWP_FRAMECHANGED);
                SetWindowPos(window, IntPtr.Zero, left, top, fullWidth, fullHeight, SWP_FRAMECHANGED);
                mainWindowFullscreenStatus = false;
                if (unpause)
                {
                    try
                    {
                        GameManager.Instance.Unpause();
                    }
                    catch { }
                }

                ETGModConsole.Log($"Successfully switched main window to windowed, window size = {width}x{height}.");
            }
            else
            {
                Debug.LogError("Main window not found.");
            }
        }

        private static void SetScreenMode(FullscreenMode mode)
        {
            if (ViewController.screenModeOptionsMenuItem != null && ViewController.resolutionOptionsMenuItem != null)
            {
                ViewController.screenModeOptionsMenuItem.optionType = BraveOptionsMenuItem.BraveOptionsOptionType.FULLSCREEN;
                ViewController.screenModeOptionsMenuItem.m_selectedIndex = (int)mode;
                ViewController.screenModeOptionsMenuItem.UpdateSelectedLabelText();
                GameManager.Options.CurrentVisualPreset = GameOptions.VisualPresetMode.CUSTOM;
                GameManager.Options.CurrentPreferredFullscreenMode = ((ViewController.screenModeOptionsMenuItem.m_selectedIndex != 0) ? ((ViewController.screenModeOptionsMenuItem.m_selectedIndex != 1) ? GameOptions.PreferredFullscreenMode.WINDOWED : GameOptions.PreferredFullscreenMode.BORDERLESS) : GameOptions.PreferredFullscreenMode.FULLSCREEN);
                ViewController.resolutionOptionsMenuItem.HandleResolutionDetermination();
            }
        }

        public enum FullscreenMode
        {
            FULLSCREEN = 0,
            BORDERLESS = 1,
            WINDOWED = 2
        }
    }
}
