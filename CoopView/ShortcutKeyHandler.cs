using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Keys = CoopView.KeyCodeMaps.Keys;

namespace CoopView
{
    public static class ShortcutKeyHandler
    {
        public const string TEXT_COLOR = "#CCFF33";

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const uint WS_POPUP = 0x80000000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WM_DESTROY = 0x0002;
        private const uint WM_ShortcutKey = 0x0312;
        private const int SW_HIDE = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASS
        {
            public uint style;
            public WNDPROC lpfn;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public IntPtr lpszMenuName;
            public string lpszClassName;
        }

        private delegate IntPtr WNDPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr _windowHandle;
        private static WNDPROC _windowProc;

        private static List<string> currentModifierList = new List<string>();
        private static KeyCode? currentNormalKey = null;

        private static uint modifierKey = 0;
        private static uint normalKey = 0;

        internal static string[] shortcutKeyString =
        {
            "Ctrl + Shift + P",
            "Ctrl + Shift + [",
            "Ctrl + Shift + ]",
            "Ctrl + Shift + -",
            "Ctrl + Shift + =",
        };

        private static bool hotkeyCapturing = false;

        internal static void SetShortcutKey(string[] args)
        {
            int shortcutKeyId;
            if (args.Length == 1)
            {
                shortcutKeyId = int.Parse(args[0]);
                if (shortcutKeyId < 0 || shortcutKeyId > 5)
                    shortcutKeyId = 0;
            }
            else
                shortcutKeyId = 0;
            
            if (hotkeyCapturing == false)
            {
                LogShortcutKeys();
                if (shortcutKeyId != 0)
                {
                    GameManager.Instance.StartCoroutine(SetShortcutKeyCrt(shortcutKeyId));
                }
                else
                {
                    ETGModConsole.Log($"");
                    ETGModConsole.Log($" - Usage:  <color={TEXT_COLOR}>shortcutkey [shortcut_key_ID]</color>");
                    ETGModConsole.Log($" - Set custom shortcut keys.");
                    ETGModConsole.Log($" - The shortcut key ID should be specified from 1 to 5 in the parameters, like 'shortcutkey 1'.");
                }
            }
        }

        private static IEnumerator SetShortcutKeyCrt(int shortcutKeyId)
        {
            hotkeyCapturing = true;
            ETGModConsole.Log($"");
            ETGModConsole.Log($"Please enter the shortcut key for shortcut_key_ID = <color={TEXT_COLOR}>{shortcutKeyId}</color>: ");
            while (hotkeyCapturing)
            {
                currentModifierList.Clear();
                currentNormalKey = null;

                modifierKey = 0;
                normalKey = 0;

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    currentModifierList.Add(KeyCodeToString(KeyCode.LeftControl));
                    modifierKey |= MOD_CONTROL;
                }

                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                {
                    currentModifierList.Add(KeyCodeToString(KeyCode.LeftAlt));
                    modifierKey |= MOD_ALT;
                }

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    currentModifierList.Add(KeyCodeToString(KeyCode.LeftShift));
                    modifierKey |= MOD_SHIFT;
                }

                if (modifierKey != 0)
                {
                    foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        if (!IsModifierKey(keyCode) && Input.GetKeyDown(keyCode))
                        {
                            if (currentNormalKey == null)
                            {
                                currentNormalKey = keyCode;
                                normalKey = (uint)KeyCodeToKeys(keyCode);
                            }
                        }
                    }
                }

                if (Input.anyKeyDown)
                {
                    if (currentModifierList.Count > 0 && currentNormalKey != null)
                    {
                        currentModifierList.Add(KeyCodeToString((KeyCode)currentNormalKey));

                        UnregisterHotKey(_windowHandle, shortcutKeyId);

                        if (!RegisterHotKey(_windowHandle, shortcutKeyId, modifierKey, normalKey))
                            ETGModConsole.Log($"Failed to register shortcut key, shortcut_key_ID = {shortcutKeyId}.");
                        else
                        {
                            string keyString = string.Join(" + ", currentModifierList.ToArray());
                            shortcutKeyString[shortcutKeyId - 1] = keyString;
                            ETGModConsole.Log($"Register shortcut key <color={TEXT_COLOR}>{keyString}</color>, shortcut_key_ID = <color={TEXT_COLOR}>{shortcutKeyId}</color>.");
                        }

                        hotkeyCapturing = false;
                        yield break;
                    }
                    currentModifierList.Clear();
                    currentNormalKey = null;

                    modifierKey = 0;
                    normalKey = 0;
                }
                yield return null;
            }
        }

        private static bool IsModifierKey(KeyCode key)
        {
            return key == KeyCode.LeftControl || key == KeyCode.RightControl ||
                   key == KeyCode.LeftShift || key == KeyCode.RightShift ||
                   key == KeyCode.LeftAlt || key == KeyCode.RightAlt || key == KeyCode.AltGr;
        }
        private static Keys KeyCodeToKeys(KeyCode keyCode)
        {
            return KeyCodeMaps.KeyCodeToKeysMap.TryGetValue(keyCode, out Keys keys) ? keys : Keys.None;
        }

        private static string KeyCodeToString(KeyCode keyCode)
        {
            return KeyCodeMaps.KeyCodeToStringMap.TryGetValue(keyCode, out string str) ? str : "";
        }

        internal static void LogShortcutKeys()
        {
            ETGModConsole.Log($"");
            ETGModConsole.Log($"Current shortcut keys:");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>{shortcutKeyString[0]}</color> , shortcut_key_ID = <color={TEXT_COLOR}>1</color> :  Switch to the next window position preset (3 in total).");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>{shortcutKeyString[1]}</color> , shortcut_key_ID = <color={TEXT_COLOR}>2</color> :  Switch the second window between fullscreen and windowed mode.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>{shortcutKeyString[2]}</color> , shortcut_key_ID = <color={TEXT_COLOR}>3</color> :  Fullscreen the second window on the next monitor.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>{shortcutKeyString[3]}</color> , shortcut_key_ID = <color={TEXT_COLOR}>4</color> :  Switch the main window between fullscreen and windowed mode.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>{shortcutKeyString[4]}</color> , shortcut_key_ID = <color={TEXT_COLOR}>5</color> :  Fullscreen the main window on the next monitor.");
        }

        internal static void InitMessageHandler()
        {
            _windowProc = new WNDPROC(WindowProc);

            WNDCLASS wndClass = new WNDCLASS
            {
                style = 0,
                lpfn = _windowProc,
                lpszClassName = "UnityMessageWindowClass",
                hInstance = GetModuleHandle(null)
            };

            RegisterClass(ref wndClass);

            _windowHandle = CreateWindowEx(
                WS_EX_TOOLWINDOW,
                "UnityMessageWindowClass",
                "UnityMessageWindow",
                WS_POPUP,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                GetModuleHandle(null),
                IntPtr.Zero);

            if(_windowHandle != IntPtr.Zero)
            {
                ShowWindow(_windowHandle, SW_HIDE);
            }

            if (_windowHandle == IntPtr.Zero)
            {
                ETGModConsole.Log("Failed to create message window.");
            }

            SetGlobalShortcutKey();
        }

        internal static void ReleaseMessageHandler()
        {
            if (_windowHandle != IntPtr.Zero)
                DestroyWindow(_windowHandle);

            UnregisterGlobalShortcutKey();
        }

        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    return IntPtr.Zero;

                case WM_ShortcutKey:
                    int shortcutKeyId = wParam.ToInt32();
                    HandleShortcutKeyPressed(shortcutKeyId);
                    return IntPtr.Zero;

                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        internal static void SetGlobalShortcutKey()
        {
            uint modifiers = MOD_CONTROL | MOD_SHIFT;
            uint key = (uint)Keys.P;

            if (!RegisterHotKey(_windowHandle, 1, modifiers, key))
            {
                ETGModConsole.Log($"Failed to register shortcut key {shortcutKeyString[0]}, shortcut_key_ID = 1.");
                shortcutKeyString[0] = "None";
            }

            modifiers = MOD_CONTROL | MOD_SHIFT;
            key = (uint)Keys.Oem4;

            if (!RegisterHotKey(_windowHandle, 2, modifiers, key))
            {
                ETGModConsole.Log($"Failed to register shortcut key {shortcutKeyString[1]}, shortcut_key_ID = 2.");
                shortcutKeyString[1] = "None";
            }

            modifiers = MOD_CONTROL | MOD_SHIFT;
            key = (uint)Keys.Oem6;

            if (!RegisterHotKey(_windowHandle, 3, modifiers, key))
            {
                ETGModConsole.Log($"Failed to register shortcut key {shortcutKeyString[2]}, shortcut_key_ID = 3.");
                shortcutKeyString[2] = "None";
            }

            modifiers = MOD_CONTROL | MOD_SHIFT;
            key = (uint)Keys.OemMinus;

            if (!RegisterHotKey(_windowHandle, 4, modifiers, key))
            {
                ETGModConsole.Log($"Failed to register shortcut key {shortcutKeyString[3]}, shortcut_key_ID = 4.");
                shortcutKeyString[3] = "None";
            }

            modifiers = MOD_CONTROL | MOD_SHIFT;
            key = (uint)Keys.Oemplus;

            if (!RegisterHotKey(_windowHandle, 5, modifiers, key))
            {
                ETGModConsole.Log($"Failed to register shortcut key {shortcutKeyString[4]}, shortcut_key_ID = 5.");
                shortcutKeyString[4] = "None";
            }
        }

        internal static void UnregisterGlobalShortcutKey()
        {
            for (int i = 1; i <= 5; i++)
                UnregisterHotKey(_windowHandle, i);
        }

        private static void HandleShortcutKeyPressed(int shortcutKeyId)
        {
            switch(shortcutKeyId)
            {
                case 1:
                    string[] args = { };
                    WindowManager.SwitchPreset(args);
                    break;
                case 2:
                    WindowManager.SwitchSecondWindowFullscreenOrWindowed();
                    break;
                case 3:
                    WindowManager.SwitchSecondWindowFullScreenMonitor();
                    break;
                case 4:
                    WindowManager.SwitchMainWindowFullscreenOrWindowed();
                    break;
                case 5:
                    WindowManager.SwitchMainWindowFullScreenMonitor();
                    break;
                default:
                    ETGModConsole.Log("Unknown shortcut key.");
                    break;
            }
        }
    }
}