using BepInEx;
using UnityEngine;
using HarmonyLib;

namespace CoopView
{
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInDependency("kleirof.etg.coopkbnm")]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CoopViewModule : BaseUnityPlugin
    {
        public const string GUID = "kleirof.etg.coopview";
        public const string NAME = "Coop View";
        public const string VERSION = "1.0.12";
        public const string TEXT_COLOR = "#CCFF33";

        private GameObject coopViewObject;

        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);

            Harmony harmony = new Harmony(GUID);
            harmony.CreateClassProcessor(typeof(CoopViewPatches.ShowTextPatchClass)).Patch();
            CoopViewPatches.ShowTextPatchClass.isPatched = true;

            if (Display.displays.Length > 1)
            {
                harmony.PatchAll();

                coopViewObject = new GameObject("Coop View Object");
                DontDestroyOnLoad(coopViewObject);
                coopViewObject.AddComponent<ViewController>();
            }
        }

        public void Update()
        {
            if (!ViewController.secondWindowActive)
                return;

            if (OptionsManager.videoOptionsScrollablePanelObject == null && !OptionsManager.isInitializingOptions)
            {
                StartCoroutine(OptionsManager.InitializeOptions());
            }
        }

        public void OnApplicationQuit()
        {
            if (!ViewController.secondWindowActive)
                return;

            WindowManager.ReleaseWindowHook();
            ShortcutKeyHandler.ReleaseMessageHandler();
        }

        internal static void Log(string text, string color = "FFFFFF")
        {
            ETGModConsole.Log($"<color={color}>{text}</color>");
        }

        private static void LogCommands()
        {
            ETGModConsole.Log($"");
            ETGModConsole.Log($"Command list:");
            ETGModConsole.Log($"[The content in square brackets can be omitted. ]");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>shortcutkey [shortcut_key_ID]</color>");
            ETGModConsole.Log($"     - Set custom shortcut keys.");
            ETGModConsole.Log($"     - The shortcut key ID should be specified from 1 to 5 in the parameters, like 'shortcutkey 1'.");
            ETGModConsole.Log($"     - Current shortcut keys and its shortcut key ID will be displayed to give you a hint.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>switchpreset [preset_index]</color>");
            ETGModConsole.Log($"     - Switch to the next window position preset (3 in total).");
            ETGModConsole.Log($"     - The preset index can be specified from 0 to 2 in the parameters, like 'switchpreset 2'.");
            ETGModConsole.Log($"        - preset_index = 0 :  Main window fullscreen on monitor 1, second window fullscreen on monitor 2.");
            ETGModConsole.Log($"        - preset_index = 1 :  Main window fullscreen on monitor 2, second window fullscreen on monitor 1.");
            ETGModConsole.Log($"        - preset_index = 2 :  Main window windowed on monitor 1, second window windowed on monitor 1.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>secondwindow fullscreen [monitor_index]</color>");
            ETGModConsole.Log($"     - Switch the second window to fullscreen mode.");
            ETGModConsole.Log($"     - The target monitor can be specified in the parameters, like 'secondwindow fullscreen 1'.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>secondwindow windowed [width] [height]</color>");
            ETGModConsole.Log($"     - Switch the second window to windowed mode.");
            ETGModConsole.Log($"     - The window size can be specified in the parameters, like 'secondwindow windowed 1920 1080'.");
            ETGModConsole.Log($"  -- <color={TEXT_COLOR}>mainwindow fullscreen [monitor_index]</color>");
            ETGModConsole.Log($"     - Switch the main window to fullscreen mode.");
            ETGModConsole.Log($"     - The target monitor can be specified in the parameters, like 'mainwindow fullscreen 1'.");
            ETGModConsole.Log($" -- <color={TEXT_COLOR}>mainwindow windowed [width] [height]</color>");
            ETGModConsole.Log($"     - Switch the main window to windowed mode.");
            ETGModConsole.Log($"     - The window size can be specified in the parameters, like 'mainwindow windowed 1920 1080'.");

        }

        private static void LogHelp(string[] args)
        {
            ShortcutKeyHandler.LogShortcutKeys();
            LogCommands();
        }

        internal void GMStart(GameManager g)
        {
            if (Display.displays.Length > 1)
            {
                Log($"{NAME} v{VERSION} started successfully.", TEXT_COLOR);
                ETGModConsole.Log($"<color={TEXT_COLOR}>Enter 'coopview help' for more options.</color>");

                ETGModConsole.Commands.AddGroup("coopview", LogHelp);
                ETGModConsole.Commands.GetGroup("coopview").AddUnit("help", LogHelp);

                ETGModConsole.Commands.AddGroup("secondwindow", LogHelp);
                ETGModConsole.Commands.GetGroup("secondwindow").AddUnit("fullscreen", WindowManager.SwitchSecondWindowToFullscreen);
                ETGModConsole.Commands.GetGroup("secondwindow").AddUnit("windowed", WindowManager.SwitchSecondWindowToWindowed);

                ETGModConsole.Commands.AddGroup("mainwindow", LogHelp);
                ETGModConsole.Commands.GetGroup("mainwindow").AddUnit("fullscreen", WindowManager.SwitchMainWindowToFullscreen);
                ETGModConsole.Commands.GetGroup("mainwindow").AddUnit("windowed", WindowManager.SwitchMainWindowToWindowed);

                ETGModConsole.Commands.AddGroup("switchpreset", WindowManager.SwitchPreset);

                ETGModConsole.Commands.AddGroup("shortcutkey", ShortcutKeyHandler.SetShortcutKey);
            }
            else
            {
                Log($"{NAME} v{VERSION} started, but an issue occurred!", TEXT_COLOR);
                ETGModConsole.Log($"Error: Second monitor not detected!\n");
                ETGModConsole.Log($"Please ensure that the second monitor is connected, whether it is a physical monitor or a virtual monitor (such as Parsec-vdd).\n");
                ETGModConsole.Log($"And ensure that the second monitor is in extend mode (shortcut key Win + P can switch).\n");
                ETGModConsole.Log($"After all of the above is ready, the game must be restarted!");
            }
        }
    }
}
