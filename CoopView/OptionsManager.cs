using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using CoopKBnM;
using System.IO;
using System.Reflection;

namespace CoopView
{
    public static class OptionsManager
    {
        internal static bool isInitializingOptions = false;

        internal static GameObject videoOptionsScrollablePanelObject;
        private static GameObject optionsMenuPanelDaveObject;
        private static GameObject secondWindowStartupResolutionSelectorPanelObject;
        private static GameObject secondWindowResolutionTipObject;
        private static GameObject playerOneCameraArrowSelectorPanelObject;

        internal static int secondWindowStartupResolution = 0;
        internal static int playerOneCamera = 0;

        public enum BraveOptionsOptionType
        {
            SECOND_WINDOW_STARTING_RESOLUTION = 0x200,
            PLEYER_ONE_CAMERA = 0x201,
        }

        internal static void OnStart()
        {
            try
            {
                CoopViewPreferences.LoadPreferences();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load Coop View Preferences." + e);
            }
        }

        internal static IEnumerator InitializeOptions()
        {
            isInitializingOptions = true;

            while (true)
            {
                if (optionsMenuPanelDaveObject == null)
                    optionsMenuPanelDaveObject = GameObject.Find("OptionsMenuPanelDave");
                if (optionsMenuPanelDaveObject == null)
                {
                    yield return null;
                    continue;
                }
                FullOptionsMenuController fullOptionsMenuController = optionsMenuPanelDaveObject.GetComponent<FullOptionsMenuController>();
                if (fullOptionsMenuController == null)
                {
                    yield return null;
                    continue;
                }
                if (!fullOptionsMenuController.IsVisible)
                {
                    yield return null;
                    continue;
                }
                if (videoOptionsScrollablePanelObject == null)
                    videoOptionsScrollablePanelObject = GameObject.Find("VideoOptionsScrollablePanel");
                if (videoOptionsScrollablePanelObject != null)
                    break;
                yield return null;
            }

            while (secondWindowResolutionTipObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("VisualPresetArrowSelectorPanel");
                GameObject playerOneLabelPanelObject = GameObject.Find("PlayerOneLabelPanel");
                if (playerOneLabelPanelObject != null)
                {
                    secondWindowResolutionTipObject = UnityEngine.Object.Instantiate(playerOneLabelPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    secondWindowResolutionTipObject.name = "ShareOneKeyBoardWarningPanel";
                    GameObject labelObject = secondWindowResolutionTipObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/Label").gameObject;

                    labelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "重启以生效第二窗口启动分辨率。\n大于实际分辨率无效。" : "Restart to activate second window startup \nresolution. Invalid if greater than the actual one.";
                    labelObject.GetComponent<dfLabel>().Color = new Color32(255, 0, 0, 255);
                    GameObject panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject = secondWindowResolutionTipObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy").gameObject;
                    Vector3 position = panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject.transform.localPosition;
                    panelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasyObject.transform.localPosition = new Vector3(position.x, position.y + 0.12f * Camera.main.pixelWidth / 1920, position.z);
                    secondWindowResolutionTipObject.GetComponent<dfPanel>().ZOrder = 0;
                }
                if (secondWindowResolutionTipObject == null)
                    yield return null;
            }

            while (secondWindowStartupResolutionSelectorPanelObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("VisualPresetArrowSelectorPanel");
                if (controllerTypeArrowSelectorPanelObject != null)
                {
                    secondWindowStartupResolutionSelectorPanelObject = UnityEngine.Object.Instantiate(controllerTypeArrowSelectorPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    secondWindowStartupResolutionSelectorPanelObject.name = "SecondWindowStartupResolutionSelectorPanel";
                    GameObject optionsArrowSelectorLabelObject = secondWindowStartupResolutionSelectorPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/OptionsArrowSelectorLabel").gameObject;

                    optionsArrowSelectorLabelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "第二窗口启动分辨率" : "Second Window Startup Resolution";
                    secondWindowStartupResolutionSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.SECOND_WINDOW_STARTING_RESOLUTION;
                    secondWindowStartupResolutionSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().DetermineAvailableOptions();
                    secondWindowStartupResolutionSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().m_selectedIndex = secondWindowStartupResolution;
                    secondWindowStartupResolutionSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();
                    secondWindowStartupResolutionSelectorPanelObject.GetComponent<dfPanel>().ZOrder = 1;
                }
                if (secondWindowStartupResolutionSelectorPanelObject == null)
                    yield return null;
            }

            while (playerOneCameraArrowSelectorPanelObject == null)
            {
                GameObject controllerTypeArrowSelectorPanelObject = GameObject.Find("VisualPresetArrowSelectorPanel");
                if (controllerTypeArrowSelectorPanelObject != null)
                {
                    playerOneCameraArrowSelectorPanelObject = UnityEngine.Object.Instantiate(controllerTypeArrowSelectorPanelObject, controllerTypeArrowSelectorPanelObject.transform.parent);
                    playerOneCameraArrowSelectorPanelObject.name = "PlayerOneCameraArrowSelectorPanelObject";
                    GameObject optionsArrowSelectorLabelObject = playerOneCameraArrowSelectorPanelObject.transform.Find("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy/OptionsArrowSelectorLabel").gameObject;

                    optionsArrowSelectorLabelObject.GetComponent<dfLabel>().Text = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "1P Coop View 相机" : "1P Coop View Camera";
                    playerOneCameraArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().optionType = (BraveOptionsMenuItem.BraveOptionsOptionType)BraveOptionsOptionType.PLEYER_ONE_CAMERA;
                    playerOneCameraArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().DetermineAvailableOptions();
                    playerOneCameraArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().m_selectedIndex = playerOneCamera;
                    playerOneCameraArrowSelectorPanelObject.GetComponent<BraveOptionsMenuItem>().HandleValueChanged();
                    playerOneCameraArrowSelectorPanelObject.GetComponent<dfPanel>().ZOrder = 2;
                }
                if (playerOneCameraArrowSelectorPanelObject == null)
                    yield return null;
            }

            dfList<dfControl> controls = GameObject.Find("VideoOptionsScrollablePanel").GetComponent<dfScrollPanel>().controls;

            for (int i = 0; i < controls.Count - 1; ++i)
            {
                if (!controls[i].gameObject.GetComponent<dfPanel>().CanFocus)
                    continue;
                int j = i + 1;
                for (; j < controls.Count - 1; ++j)
                {
                    if (controls[j].gameObject.GetComponent<dfPanel>().CanFocus)
                        break;
                }
                if (j < controls.Count - 1)
                    controls[i].gameObject.GetComponent<BraveOptionsMenuItem>().down = controls[j];
            }
            for (int i = controls.Count - 1; i > 0; --i)
            {
                if (!controls[i].gameObject.GetComponent<dfPanel>().CanFocus)
                    continue;
                int j = i - 1;
                for (; j > 0; --j)
                {
                    if (controls[j].gameObject.GetComponent<dfPanel>().CanFocus)
                        break;
                }
                if (j > 0)
                    controls[i].gameObject.GetComponent<BraveOptionsMenuItem>().up = controls[j];
            }

            int indexCanSelect = 0;
            for (; indexCanSelect < controls.Count; ++indexCanSelect)
            {
                if (controls[indexCanSelect].CanFocus)
                    break;
            }

            if (controls.Count > 0 && indexCanSelect < controls.Count && optionsMenuPanelDaveObject.GetComponent<FullOptionsMenuController>().TabVideo.IsVisible)
            {
                GameObject.Find("ConfirmButton").GetComponent<UIKeyControls>().down = controls[indexCanSelect];
                GameObject.Find("CancelButton").GetComponent<UIKeyControls>().down = controls[indexCanSelect];
                GameObject.Find("ResetDefaultsButton").GetComponent<UIKeyControls>().down = controls[indexCanSelect];
                controls[indexCanSelect].gameObject.GetComponent<BraveOptionsMenuItem>().up = GameObject.Find("ConfirmButton").GetComponent<dfButton>();
                controls[indexCanSelect].Focus(true);
            }

            isInitializingOptions = false;
        }
    }
}
