using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using CoopKBnM;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace CoopView
{
    class ViewController : MonoBehaviour
    {
        private static GameObject canvasObject;
        private static Canvas canvas;

        private static GameObject rawImageObject;
        private static RawImage rawImage;
        private static RenderTexture renderTexture;

        private static GameObject uiRawImageObject;
        private static RawImage uiRawImage;
        internal static RenderTexture uiRenderTexture;

        private static GameObject maskRawImageObject;
        private static RawImage maskRawImage;
        internal static RenderTexture maskRenderTexture;

        internal static Pixelator originCameraPixelator;
        internal static Pixelator cameraPixelator;

        private static GameObject originalCameraObject;
        private static GameObject cameraObject;
        private static GameObject minimapObject;

        internal static Camera weirdoUiCamera;

        internal static Camera camera;
        internal static Camera originalCamera;
        internal static Camera uiCamera;
        internal static Camera minimapUiCamera;
        internal static Camera minimapCamera;
        internal static Camera ammoCamera;

        internal static CameraController cameraController;
        internal static CameraController originalCameraController;

        internal static GameUIRoot uiRoot;

        private static GameUIReloadBarController reloadBarController;
        private static GameUIReloadBarController coopReloadBarController;
        private static dfGUIManager uiManager;

        private static GameCursorController gameCursorController;

        internal static BraveOptionsMenuItem resolutionOptionsMenuItem;
        internal static BraveOptionsMenuItem screenModeOptionsMenuItem;

        private static Rect rectFullscreen;
        private static Rect rectSmall;

        private bool clearable = false;

        internal static int secondWindowPixelWidth;
        internal static int secondWindowPixelHeight;

        internal static bool secondWindowActive = false;

        internal static AssetBundle coopViewAssets;
        private static Shader maskShader;
        private static Material maskMaterial;

        internal static bool waitingForRestoringCaption = true;

        internal static GameObject mainCameraUiRootObject;
        internal static GameObject secondCameraUiRootObject;

        internal static GameUIReloadBarController mainCameraReloadBar;
        internal static GameUIReloadBarController mainCameraCoopReloadBar;

        internal static GameUIReloadBarController secondCameraReloadBar;
        internal static GameUIReloadBarController secondCameraCoopReloadBar;

        internal static dfLabel mainCameraReloadLabel;
        internal static dfLabel mainCameraCoopReloadLabel;

        internal static dfLabel secondCameraReloadLabel;
        internal static dfLabel secondCameraCoopReloadLabel;

        internal static GameUIRoot mainCameraUiRoot;
        internal static GameUIRoot secondCameraUiRoot;

        internal static GameObject primaryThreatArrow;
        internal static GameObject secondaryThreatArrow;

        internal static Dictionary<object, Material> additionalRenderMaterials = new Dictionary<object, Material>();

        public void Start()
        {
            if (Display.displays.Length > 1)
            {
                OptionsManager.OnStart();

                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Display.displays[1].Activate(WindowManager.startupWidth, WindowManager.startupHeight, 0);
                WindowManager.SetSecondWindowText();

                string[] args = { "2" };
                WindowManager.SwitchSecondWindowToFullscreen(args);

                WindowManager.referenceSecondWindowWidth = WindowManager.startupWidth;
                WindowManager.referenceSecondWindowHeight = WindowManager.startupHeight;

                if (Display.displays[1].active)
                {
                    secondWindowActive = true;
                    CoopKBnMModule.secondWindowActive = true;
                }

                canvasObject = new GameObject("Overlay Canvas");
                DontDestroyOnLoad(canvasObject);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.targetDisplay = 1;
                canvas.sortingOrder = 1;

                WindowManager.InitWindowHook();
                ShortcutKeyHandler.InitMessageHandler();

                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("CoopView.coopview_assets"))
                {
                    coopViewAssets = AssetBundle.LoadFromStream(s);
                }

                maskShader = coopViewAssets.LoadAsset<Shader>("MaskRenderTextureRegion");
                maskMaterial = new Material(maskShader);

                ChangeMouseSensitivityMultipliers();
            }
            else
            {
                Debug.LogError("Target monitor not found.");
            }
        }

        public void Update()
        {
            if (!secondWindowActive)
                return;

            if (waitingForRestoringCaption)
            {
                WindowManager.RestoreMainWindowCaption();
                string[] args = { };
                WindowManager.SwitchMainWindowToWindowed(args);
                waitingForRestoringCaption = false;
            }

            if (resolutionOptionsMenuItem == null)
            {
                GameObject resolutionOptionsMenuObject = GameObject.Find("ResolutionArrowSelectorPanelWithInfoBox");
                if (resolutionOptionsMenuObject != null)
                {
                    resolutionOptionsMenuItem = resolutionOptionsMenuObject.GetComponent<BraveOptionsMenuItem>();
                }
            }

            if (screenModeOptionsMenuItem == null)
            {
                GameObject screenModeOptionsMenuObject = GameObject.Find("ScreenModeArrowSelectorPanel");
                if (screenModeOptionsMenuObject != null)
                {
                    screenModeOptionsMenuItem = screenModeOptionsMenuObject.GetComponentInChildren<BraveOptionsMenuItem>();
                }
            }

            if (GameManager.HasInstance && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                if (uiRenderTexture != null)
                {
                    RenderTexture.active = uiRenderTexture;
                    GL.Clear(true, true, (GameManager.Instance.IsPaused || GameManager.Instance.IsLoadingLevel || AmmonomiconController.Instance.IsOpen) ? Color.black : Color.clear);
                    RenderTexture.active = null;
                }

                if (GameManager.Instance.IsLoadingLevel && weirdoUiCamera == null)
                {
                    GameObject weirdoUiObject = GameObject.Find("weirdo ui camera");
                    if (weirdoUiObject != null)
                    {
                        weirdoUiCamera = weirdoUiObject.GetComponent<Camera>();
                        clearable = true;
                    }
                }

                if (uiManager == null)
                {
                    GameObject uiRootObject = GameObject.Find("UI Root");
                    if (uiRootObject != null)
                    {
                        uiRoot = uiRootObject.GetComponent<GameUIRoot>();
                        uiManager = uiRootObject.GetComponent<dfGUIManager>();
                        gameCursorController = uiRootObject.GetComponent<GameCursorController>();
                        StartCoroutine(CreateUiRoots());
                        clearable = true;
                    }
                }

                if (reloadBarController == null)
                {
                    GameObject reloadBarObject = GameObject.Find("ReloadSlider");
                    if (reloadBarObject != null)
                    {
                        reloadBarController = reloadBarObject.GetComponent<GameUIReloadBarController>();
                        clearable = true;
                    }
                }

                if (coopReloadBarController == null)
                {
                    GameObject coopReloadBarObject = GameObject.Find("ReloadSlider Coop");
                    if (coopReloadBarObject != null)
                    {
                        coopReloadBarController = coopReloadBarObject.GetComponent<GameUIReloadBarController>();
                        clearable = true;
                    }
                }

                if (originalCamera == null)
                {
                    originalCameraObject = GameObject.Find("Main Camera");
                    if (originalCameraObject != null)
                    {
                        originalCamera = originalCameraObject.GetComponentInChildren<Camera>();
                        originCameraPixelator = originalCamera.GetComponent<Pixelator>();
                        originalCameraController = originalCamera.GetComponent<CameraController>();

                        rectFullscreen = originalCamera.rect;
                        rectSmall = new Rect(0.25f * rectFullscreen.width + 0.5f, 0.1625f * rectFullscreen.height + 0.5f, 0.25f * rectFullscreen.width, 0.25f * rectFullscreen.height);

                        if (secondWindowActive)
                        {
                            if ((float)9 / 16 < ((float)WindowManager.SecondWindowHeight / (float)WindowManager.SecondWindowWidth))
                            {
                                secondWindowPixelWidth = WindowManager.SecondWindowWidth;
                                secondWindowPixelHeight = (int)(WindowManager.SecondWindowWidth * (float)9 / 16);
                            }
                            else
                            {
                                secondWindowPixelWidth = (int)(WindowManager.SecondWindowHeight * (float)16 / 9);
                                secondWindowPixelHeight = WindowManager.SecondWindowHeight;
                            }
                        }
                        clearable = true;
                    }
                }
                else
                {
                    if (camera == null)
                    {
                        cameraObject = Instantiate(originalCameraObject);
                        if (cameraObject != null)
                        {
                            camera = cameraObject.GetComponent<Camera>();
                            camera.name = "Camera Copy";
                            camera.tag = "Untagged";
                            camera.targetTexture = renderTexture;
                            cameraController = camera.GetComponent<CameraController>();
                            cameraPixelator = camera.GetComponent<Pixelator>();
                            clearable = true;
                        }
                    }
                }

                if (uiCamera == null)
                {
                    GameObject uiRootObject = GameObject.Find("UI Root");
                    if (uiRootObject != null)
                    {
                        uiCamera = uiRootObject.GetComponentInChildren<Camera>();
                        if (Minimap.HasInstance)
                            Minimap.Instance.ToggleMinimap(false, false);
                        clearable = true;
                    }
                }

                if (minimapUiCamera == null)
                {
                    GameObject minimapRootObject = GameObject.Find("Minimap UI Root");
                    if (minimapRootObject != null)
                    {
                        minimapUiCamera = minimapRootObject.GetComponentInChildren<Camera>();
                        clearable = true;
                    }
                }

                if (minimapCamera == null)
                {
                    minimapObject = GameObject.Find("_Minimap");
                    if (minimapObject != null)
                    {
                        GameObject cameraObject = minimapObject.transform.Find("Minimap Camera").gameObject;
                        minimapCamera = cameraObject.GetComponentInChildren<Camera>();
                        minimapCamera.GetComponent<MinimapRenderer>().QuadTransform.gameObject.SetLayerRecursively(LayerMask.NameToLayer("GUI_InventoryBoxes"));
                        clearable = true;
                    }
                }

                if (ammoCamera == null)
                {
                    GameObject ammoRootObject = GameObject.Find("Ammonomicon Root(Clone)");
                    if (ammoRootObject != null)
                    {
                        ammoCamera = ammoRootObject.GetComponentInChildren<Camera>();
                        clearable = true;
                    }
                }

                if (secondWindowActive && (rawImageObject == null || uiRawImageObject == null))
                {
                    if ((float)9 / 16 < ((float)WindowManager.SecondWindowHeight / (float)WindowManager.SecondWindowWidth))
                    {
                        secondWindowPixelWidth = WindowManager.SecondWindowWidth;
                        secondWindowPixelHeight = (int)(WindowManager.SecondWindowWidth * (float)9 / 16);
                    }
                    else
                    {
                        secondWindowPixelWidth = (int)(WindowManager.SecondWindowHeight * (float)16 / 9);
                        secondWindowPixelHeight = WindowManager.SecondWindowHeight;
                    }

                    renderTexture = new RenderTexture(WindowManager.startupWidth, WindowManager.startupHeight, 0, RenderTextureFormat.ARGB32);
                    renderTexture.enableRandomWrite = true;
                    renderTexture.Create();

                    rawImageObject = new GameObject("rawImage");
                    DontDestroyOnLoad(rawImageObject);
                    rawImageObject.transform.SetParent(canvasObject.transform);
                    rawImage = rawImageObject.AddComponent<RawImage>();
                    RectTransform rectTransform = rawImageObject.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2((int)(secondWindowPixelWidth * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), (int)(secondWindowPixelHeight * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
                    rectTransform.anchoredPosition = Vector2.zero;
                    rawImage.texture = renderTexture;

                    uiRenderTexture = new RenderTexture((int)((float)secondWindowPixelWidth / originalCamera.rect.width), (int)((float)secondWindowPixelHeight / originalCamera.rect.height), 0, RenderTextureFormat.ARGB32);
                    uiRenderTexture.enableRandomWrite = true;
                    uiRenderTexture.Create();

                    uiRawImageObject = new GameObject("uiRawImage");
                    DontDestroyOnLoad(uiRawImageObject);
                    uiRawImageObject.transform.SetParent(canvasObject.transform);
                    uiRawImage = uiRawImageObject.AddComponent<RawImage>();
                    RectTransform uiRectTransform = uiRawImageObject.GetComponent<RectTransform>();
                    uiRectTransform.sizeDelta = new Vector2((int)((float)secondWindowPixelWidth / originalCamera.rect.width * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), (int)((float)secondWindowPixelHeight / originalCamera.rect.height * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
                    uiRectTransform.anchoredPosition = Vector2.zero;
                    uiRawImage.texture = uiRenderTexture;

                    maskRenderTexture = new RenderTexture((int)((float)secondWindowPixelWidth / originalCamera.rect.width), (int)((float)secondWindowPixelHeight / originalCamera.rect.height), 0, RenderTextureFormat.ARGB32);
                    maskRenderTexture.enableRandomWrite = true;
                    maskRenderTexture.Create();

                    maskRawImageObject = new GameObject("maskRawImage");
                    DontDestroyOnLoad(maskRawImageObject);
                    maskRawImageObject.transform.SetParent(canvasObject.transform);
                    maskRawImage = maskRawImageObject.AddComponent<RawImage>();
                    RectTransform maskRectTransform = maskRawImageObject.GetComponent<RectTransform>();
                    maskRectTransform.sizeDelta = new Vector2((int)((float)secondWindowPixelWidth / originalCamera.rect.width * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), (int)((float)secondWindowPixelHeight / originalCamera.rect.height * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
                    maskRectTransform.anchoredPosition = Vector2.zero;
                    maskRawImage.texture = maskRenderTexture;

                    float pixelWidth = Camera.main.rect.width;
                    float pixelHeight = Camera.main.rect.height;
                    maskMaterial.SetVector("_MaskRect", new Vector4((1 - pixelWidth) / 2, (1 - pixelHeight) / 2, pixelWidth, pixelHeight));
                    Graphics.Blit(null, maskRenderTexture, maskMaterial);

                    clearable = true;

                    Debug.Log("Second camera initialized.");
                }
            }
            else if (clearable == true)
            {
                clearable = false;
                StartCoroutine(ClearCaches());
            }
        }

        public void LateUpdate()
        {
            if (!secondWindowActive)
                return;

            if (GameManager.HasInstance && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            {
                if (!GameManager.Instance.IsLoadingLevel)
                {
                    if (camera != null)
                    {
                        cameraPixelator.SetOcclusionDirty();
                        originCameraPixelator.SetOcclusionDirty();
                    }

                    if ((GameManager.Instance.IsPaused || GameManager.Instance.IsLoadingLevel || AmmonomiconController.Instance.IsOpen) && uiRenderTexture != null)
                    {
                        RenderTexture.active = uiRenderTexture;
                        GL.Clear(true, true, Color.black);
                        RenderTexture.active = null;
                    }

                    if (minimapCamera != null && Minimap.HasInstance && !Minimap.Instance.m_isFaded && !GameManager.IsBossIntro)
                    {
                        Rect tempRect = minimapCamera.rect;
                        minimapCamera.rect = Minimap.Instance.IsFullscreen ? rectFullscreen : rectSmall;
                        minimapCamera.targetTexture = uiRenderTexture;
                        minimapCamera.Render();
                        minimapCamera.targetTexture = null;
                        minimapCamera.rect = tempRect;
                    }

                    if (uiCamera != null)
                    {
                        if (mainCameraUiRootObject != null)
                            mainCameraUiRootObject.layer = LayerMask.NameToLayer("UI");
                        if (secondCameraUiRootObject != null)
                            secondCameraUiRootObject.layer = LayerMask.NameToLayer("GUI");

                        if (uiManager.isDirty && uiManager.suspendCount <= 0)
                        {
                            uiManager.Render();
                            dfMaterialCache.Reset();
                            uiManager.updateDrawCalls();
                        }

                        uiCamera.targetTexture = uiRenderTexture;
                        uiCamera.Render();
                        uiCamera.targetTexture = null;

                        if (mainCameraUiRootObject != null)
                            mainCameraUiRootObject.layer = LayerMask.NameToLayer("GUI");
                        if (secondCameraUiRootObject != null)
                            secondCameraUiRootObject.layer = LayerMask.NameToLayer("UI");
                    }

                    if (minimapUiCamera != null && Minimap.HasInstance && Minimap.Instance.IsFullscreen)
                    {
                        minimapUiCamera.targetTexture = uiRenderTexture;
                        minimapUiCamera.Render();
                        minimapUiCamera.targetTexture = null;
                    }

                    if (ammoCamera != null && AmmonomiconController.HasInstance && AmmonomiconController.Instance.IsOpen)
                    {
                        ammoCamera.targetTexture = uiRenderTexture;
                        ammoCamera.Render();
                        ammoCamera.targetTexture = null;
                    }
                }
                else if (weirdoUiCamera != null)
                {
                    Rect tempRect = weirdoUiCamera.rect;
                    weirdoUiCamera.rect = rectFullscreen;
                    weirdoUiCamera.targetTexture = uiRenderTexture;
                    weirdoUiCamera.Render();
                    weirdoUiCamera.targetTexture = null;
                    weirdoUiCamera.rect = tempRect;
                }

                DrawCursor(uiRenderTexture);
            }
        }

        private static IEnumerator CreateUiRoots()
        {
            if (mainCameraUiRootObject == null)
            {
                mainCameraUiRootObject = Instantiate(GameObject.Find("UI Root"));
                mainCameraUiRootObject.name = "Main Camera UI Root";
                DontDestroyOnLoad(mainCameraUiRootObject);
                mainCameraUiRoot = mainCameraUiRootObject.GetComponent<GameUIRoot>();
                mainCameraUiRootObject.GetComponentInChildren<Camera>().cullingMask = LayerMask.GetMask("GUI");
                Destroy(mainCameraUiRootObject.GetComponent<GameCursorController>());
                Destroy(mainCameraUiRootObject.GetComponent<InControlInputAdapter>());

                Transform mainChildCoopReloadBar = null;
                while (mainCameraCoopReloadBar == null)
                {
                    while (mainChildCoopReloadBar == null)
                    {
                        mainChildCoopReloadBar = mainCameraUiRootObject.transform.Find("ReloadSlider Coop");
                        if (mainChildCoopReloadBar != null)
                            break;
                        yield return null;
                    }

                    mainCameraCoopReloadBar = mainChildCoopReloadBar.GetComponent<GameUIReloadBarController>();
                    if (mainCameraCoopReloadBar != null)
                        break;
                    yield return null;
                }
                Transform mainChildReloadBar = mainCameraUiRootObject.transform.Find("ReloadSlider");
                mainCameraReloadBar = mainChildReloadBar.GetComponent<GameUIReloadBarController>();

                mainCameraUiRoot.p_playerReloadBar = mainCameraReloadBar;
                mainCameraUiRoot.p_secondaryPlayerReloadBar = mainCameraCoopReloadBar;
                mainCameraUiRoot.m_extantReloadBars = new List<GameUIReloadBarController> { mainCameraReloadBar, mainCameraCoopReloadBar };
                mainCameraUiRoot.motionGroups.Clear();
                _ = mainCameraUiRoot.Manager;

                Transform mainChildCamera = mainCameraUiRootObject.transform.Find("UI Camera");
                Transform mainChildStatus = mainCameraUiRootObject.transform.Find("Status Bar Panel");
                Transform mainChildCoopStatus = mainCameraUiRootObject.transform.Find("Status Bar Panel Coop");

                Transform mainChildReloadLabel = mainCameraUiRootObject.transform.Find("ReloadLabel");
                Transform mainChildCoopReloadLabel = Instantiate(mainChildReloadLabel, mainChildReloadLabel.transform.parent);

                mainCameraReloadLabel = mainChildReloadLabel.GetComponent<dfLabel>();
                mainCameraCoopReloadLabel = mainChildCoopReloadLabel.GetComponent<dfLabel>();

                foreach (Transform child in mainCameraUiRootObject.transform)
                {
                    if (child != mainChildReloadBar && child != mainChildCamera && child != mainChildStatus && child != mainChildCoopStatus && child != mainChildCoopReloadBar && child != mainChildReloadLabel && child != mainChildCoopReloadLabel)
                        Destroy(child.gameObject);
                }

                yield return null;
                mainCameraUiRoot.m_extantReloadBars = new List<GameUIReloadBarController> { mainCameraReloadBar, mainCameraCoopReloadBar };
                mainCameraUiRoot.m_extantReloadLabels = new List<dfLabel> { mainCameraReloadLabel, mainCameraCoopReloadLabel };
                mainCameraUiRoot.m_displayingReloadNeeded = new List<bool> { false, false };

                secondCameraUiRootObject = Instantiate(mainCameraUiRootObject);
                secondCameraUiRootObject.name = "Second Camera UI Root";
                DontDestroyOnLoad(secondCameraUiRootObject);
                secondCameraUiRoot = secondCameraUiRootObject.GetComponent<GameUIRoot>();
                secondCameraUiRootObject.GetComponentInChildren<Camera>().cullingMask = LayerMask.GetMask("GUI");
                Destroy(secondCameraUiRootObject.GetComponent<GameCursorController>());
                Destroy(secondCameraUiRootObject.GetComponent<InControlInputAdapter>());

                Transform secondChildCoopReloadBar = null;
                while (secondCameraCoopReloadBar == null)
                {
                    while (secondChildCoopReloadBar == null)
                    {
                        secondChildCoopReloadBar = secondCameraUiRootObject.transform.Find("ReloadSlider Coop");
                        if (secondChildCoopReloadBar != null)
                            break;
                        yield return null;
                    }

                    secondCameraCoopReloadBar = secondChildCoopReloadBar.GetComponent<GameUIReloadBarController>();
                    if (secondCameraCoopReloadBar != null)
                        break;
                    yield return null;
                }

                Transform secondChildReloadBar = secondCameraUiRootObject.transform.Find("ReloadSlider");
                secondCameraReloadBar = secondChildReloadBar.GetComponent<GameUIReloadBarController>();
                secondCameraUiRoot.p_playerReloadBar = secondCameraReloadBar;
                secondCameraUiRoot.p_secondaryPlayerReloadBar = secondCameraCoopReloadBar;
                secondCameraUiRoot.m_extantReloadBars = new List<GameUIReloadBarController> { secondCameraReloadBar, secondCameraCoopReloadBar };
                secondCameraUiRoot.motionGroups.Clear();
                _ = secondCameraUiRoot.Manager;
                yield return null;

                Transform secondChildReloadLabel = secondCameraUiRootObject.transform.Find("ReloadLabel");
                secondCameraReloadLabel = secondChildReloadLabel.GetComponent<dfLabel>();
                Transform secondChildCoopReloadLabel = secondCameraUiRootObject.transform.Find("ReloadLabel(Clone)");
                secondCameraCoopReloadLabel = secondChildCoopReloadLabel.GetComponent<dfLabel>();
                secondCameraUiRoot.m_extantReloadLabels = new List<dfLabel> { secondCameraReloadLabel, secondCameraCoopReloadLabel };
                secondCameraUiRoot.m_displayingReloadNeeded = new List<bool> { false, false };
            }
        }

        private IEnumerator ClearCaches()
        {
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;

            RenderTexture.active = uiRenderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;

            yield return null;

            if (originalCameraController != null)
                originalCameraController = null;
            if (cameraController != null)
                cameraController = null;
            if (originalCamera != null)
                originalCamera = null;
            if (camera != null)
            {
                Destroy(camera);
                camera = null;
            }
            if (uiCamera != null)
                uiCamera = null;
            if (weirdoUiCamera != null)
                weirdoUiCamera = null;
            if (minimapUiCamera != null)
                minimapUiCamera = null;
            if (minimapCamera != null)
                minimapCamera = null;
            if (ammoCamera != null)
                ammoCamera = null;
            if (originCameraPixelator != null)
                originCameraPixelator = null;
            if (cameraPixelator != null)
                cameraPixelator = null;
            if (coopReloadBarController != null)
                coopReloadBarController = null;
            if (reloadBarController != null)
                reloadBarController = null;
            if (originalCameraObject != null)
                originalCameraObject = null;
            if (cameraObject != null)
                cameraObject = null;
            if (uiRoot != null)
                uiRoot = null;
            if (uiManager != null)
                uiManager = null;
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
            if (uiRenderTexture != null)
            {
                uiRenderTexture.Release();
                uiRenderTexture = null;
            }
            if (rawImageObject != null)
            {
                Destroy(rawImageObject);
                rawImageObject = null;
            }
            if (uiRawImageObject != null)
            {
                Destroy(uiRawImageObject);
                uiRawImageObject = null;
            }
            if (mainCameraUiRootObject != null)
            {
                Destroy(mainCameraUiRootObject);
                mainCameraUiRootObject = null;
            }
            if (secondCameraUiRootObject != null)
            {
                Destroy(secondCameraUiRootObject);
                secondCameraUiRootObject = null;
            }

            Debug.Log("Second camera released.");
        }

        internal static IEnumerator OnUpdateResolution()
        {
            while (GameManager.Instance.IsLoadingLevel)
                yield return null;

            if (WindowManager.SecondWindow == IntPtr.Zero && !camera)
                yield break;

            if (uiRawImageObject != null)
            {
                uiRawImage.texture = null;
                uiRawImage.color = Color.black;
            }

            yield return null;
            yield return null;
            yield return null;

            if (GameManager.Options.CurrentPreferredFullscreenMode == GameOptions.PreferredFullscreenMode.WINDOWED)
            {
                IntPtr window = BraveOptionsMenuItem.ResolutionManagerWin.Window;
                int style = (int)BraveOptionsMenuItem.WindowsResolutionManager.GetWindowLongPtr(window, BraveOptionsMenuItem.WindowsResolutionManager.GWL_STYLE);
                BraveOptionsMenuItem.WindowsResolutionManager.SetWindowLongPtr(window, BraveOptionsMenuItem.WindowsResolutionManager.GWL_STYLE, style | BraveOptionsMenuItem.WindowsResolutionManager.WS_CAPTION | BraveOptionsMenuItem.WindowsResolutionManager.WS_THICKFRAME | BraveOptionsMenuItem.WindowsResolutionManager.WS_SYSMENU | BraveOptionsMenuItem.WindowsResolutionManager.WS_MINIMIZEBOX | BraveOptionsMenuItem.WindowsResolutionManager.WS_MAXIMIZEBOX);
                BraveOptionsMenuItem.WindowsResolutionManager.SetWindowPos(window, -2, 0, 0, 0, 0, BraveOptionsMenuItem.WindowsResolutionManager.SWP_FRAMECHANGED | BraveOptionsMenuItem.WindowsResolutionManager.SWP_NOMOVE | BraveOptionsMenuItem.WindowsResolutionManager.SWP_NOSIZE);
            }

            if (renderTexture != null)
            {
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
            }

            yield return null;

            if ((float)9 / 16 < ((float)WindowManager.SecondWindowHeight / (float)WindowManager.SecondWindowWidth))
            {
                secondWindowPixelWidth = WindowManager.SecondWindowWidth;
                secondWindowPixelHeight = (int)(WindowManager.SecondWindowWidth * (float)9 / 16);
            }
            else
            {
                secondWindowPixelWidth = (int)(WindowManager.SecondWindowHeight * (float)16 / 9);
                secondWindowPixelHeight = WindowManager.SecondWindowHeight;
            }

            if (rawImageObject != null)
            {
                RectTransform rectTransform = rawImageObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2((int)(secondWindowPixelWidth * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), (int)(secondWindowPixelHeight * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
            }

            if (uiRawImageObject != null)
            {
                uiRenderTexture.Release();
                uiRenderTexture = new RenderTexture((int)((float)WindowManager.referenceSecondWindowWidth / originalCamera.rect.width), (int)((float)WindowManager.referenceSecondWindowHeight / originalCamera.rect.height), 0, RenderTextureFormat.ARGB32);
                uiRenderTexture.enableRandomWrite = true;
                uiRenderTexture.Create();

                RectTransform uiRectTransform = uiRawImageObject.GetComponent<RectTransform>();
                uiRectTransform.sizeDelta = new Vector2((int)((float)secondWindowPixelWidth / originalCamera.rect.width * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), (int)((float)secondWindowPixelHeight / originalCamera.rect.height * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));

                uiRawImage.texture = null;
                uiRawImage.color = Color.black;
                yield return null;
                uiRawImage.color = Color.white;
                uiRawImage.texture = uiRenderTexture;

                maskRenderTexture.Release();
                maskRenderTexture = new RenderTexture((int)((float)WindowManager.referenceSecondWindowWidth / originalCamera.rect.width), (int)((float)WindowManager.referenceSecondWindowHeight / originalCamera.rect.height), 0, RenderTextureFormat.ARGB32);
                maskRenderTexture.enableRandomWrite = true;
                maskRenderTexture.Create();

                RectTransform maskRectTransform = maskRawImageObject.GetComponent<RectTransform>();
                maskRectTransform.sizeDelta = new Vector2((int)((float)secondWindowPixelWidth / originalCamera.rect.width * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), (int)((float)secondWindowPixelHeight / originalCamera.rect.height * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));

                maskRawImage.texture = null;
                maskRawImage.color = Color.black;
                yield return null;
                maskRawImage.color = Color.white;
                maskRawImage.texture = maskRenderTexture;

                float pixelWidth = Camera.main.rect.width;
                float pixelHeight = Camera.main.rect.height;
                maskMaterial.SetVector("_MaskRect", new Vector4((1 - pixelWidth) / 2, (1 - pixelHeight) / 2, pixelWidth, pixelHeight));
                Graphics.Blit(null, maskRenderTexture, maskMaterial);
            }

            if (originalCamera != null)
            {
                rectFullscreen = originalCamera.rect;
                rectSmall = new Rect(0.25f * rectFullscreen.width + 0.5f, 0.1625f * rectFullscreen.height + 0.5f, 0.25f * rectFullscreen.width, 0.25f * rectFullscreen.height);
            }

            ChangeMouseSensitivityMultipliers();
        }

        private static void DrawCursor(RenderTexture targetTexture)
        {
            if (!GameManager.HasInstance)
                return;
            if (gameCursorController == null)
                return;

            if (GameCursorController.showMouseCursor && (GameManager.HasInstance ? GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER : false))
            {
                if (RawInputHandler.ShowPublicCursor)
                {
                    Texture2D texture2D;
                    Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    float scale = 1f;
                    if (CoopKBnMPatches.customCursorIsOn)
                    {
                        if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                        {
                            texture2D = CoopKBnMPatches.playerTwoCursor;
                            color = CoopKBnMPatches.playerTwoCursorModulation;
                            scale = CoopKBnMPatches.playerTwoCursorScale;
                        }
                        else
                        {
                            texture2D = CoopKBnMPatches.playerOneCursor;
                            color = CoopKBnMPatches.playerOneCursorModulation;
                            scale = CoopKBnMPatches.playerOneCursorScale;
                        }
                        if (texture2D == null)
                        {
                            texture2D = gameCursorController.normalCursor;
                            int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                            if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                                texture2D = gameCursorController.cursors[currentCursorIndex];
                        }
                    }
                    else
                    {
                        texture2D = gameCursorController.normalCursor;
                        int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                        if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                            texture2D = gameCursorController.cursors[currentCursorIndex];

                        if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                            color = new Color(0.402f, 0.111f, 0.32f);
                    }

                    Vector2 mousePosition = RawInputHandler.firstMousePosition;
                    Vector2 vector = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                    Rect screenRect = new Rect((mousePosition.x + 0.5f - vector.x / 2f - Screen.width / 2) * WindowManager.referenceSecondWindowWidth / Screen.width, (mousePosition.y + 0.5f - vector.y / 2f - Screen.height / 2) * WindowManager.referenceSecondWindowHeight / Screen.height,
                        vector.x * WindowManager.referenceSecondWindowWidth / Screen.width, vector.y * WindowManager.referenceSecondWindowHeight / Screen.height);
                    screenRect = new Rect(screenRect.x, screenRect.y + screenRect.height, screenRect.width, -screenRect.height);
                    RenderTexture.active = targetTexture;
                    Graphics.DrawTexture(screenRect, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                    RenderTexture.active = null;
                }
                else
                {
                    if ((RawInputHandler.ShowPlayerOneMouseCursor && !CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera) || (RawInputHandler.ShowPlayerTwoMouseCursor && CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera))
                    {
                        Texture2D texture2D;
                        Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        float scale = 1f;
                        if (CoopKBnMPatches.customCursorIsOn)
                        {
                            if (CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera)
                            {
                                texture2D = CoopKBnMPatches.playerTwoCursor;
                                color = CoopKBnMPatches.playerTwoCursorModulation;
                                scale = CoopKBnMPatches.playerTwoCursorScale;
                            }
                            else
                            {
                                texture2D = CoopKBnMPatches.playerOneCursor;
                                color = CoopKBnMPatches.playerOneCursorModulation;
                                scale = CoopKBnMPatches.playerOneCursorScale;
                            }
                            if (texture2D == null)
                            {
                                texture2D = gameCursorController.normalCursor;
                                int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                                if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                                    texture2D = gameCursorController.cursors[currentCursorIndex];
                            }
                        }
                        else
                        {
                            texture2D = gameCursorController.normalCursor;
                            int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                            if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                                texture2D = gameCursorController.cursors[currentCursorIndex];

                            if (CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera)
                                color = new Color(0.402f, 0.111f, 0.32f);
                        }

                        Vector2 mousePosition;
                        if (!CoopKBnM.OptionsManager.restrictMouseInputPort)
                            mousePosition = RawInputHandler.firstMousePosition;
                        else
                        {
                            if (CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera)
                                mousePosition = CoopKBnM.OptionsManager.currentPlayerOneMousePort == 0 ? RawInputHandler.secondMousePosition : RawInputHandler.firstMousePosition;
                            else
                                mousePosition = CoopKBnM.OptionsManager.currentPlayerOneMousePort != 0 ? RawInputHandler.secondMousePosition : RawInputHandler.firstMousePosition;
                        }

                        Vector2 vector = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                        Rect screenRect = new Rect((mousePosition.x + 0.5f - vector.x / 2f - Screen.width / 2) * WindowManager.referenceSecondWindowWidth / Screen.width, (mousePosition.y + 0.5f - vector.y / 2f - Screen.height / 2) * WindowManager.referenceSecondWindowHeight / Screen.height,
                            vector.x * WindowManager.referenceSecondWindowWidth / Screen.width, vector.y * WindowManager.referenceSecondWindowHeight / Screen.height);
                        screenRect = new Rect(screenRect.x, screenRect.y + screenRect.height, screenRect.width, -screenRect.height);
                        RenderTexture.active = targetTexture;
                        Graphics.DrawTexture(screenRect, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                        RenderTexture.active = null;
                    }
                }
            }
            PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
            if (!CoopKBnM.OptionsManager.restrictMouseInputPort && !CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera && GameCursorController.showPlayerOneControllerCursor && !GameManager.Instance.IsPaused && !GameManager.IsBossIntro)
            {
                BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(0);
                if (primaryPlayer && instanceForPlayer.ActiveActions.Aim.Vector != Vector2.zero && (primaryPlayer.CurrentInputState == PlayerInputState.AllInput || primaryPlayer.IsInMinecart))
                {
                    Texture2D texture2D;
                    Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    float scale = 1f;
                    if (CoopKBnMPatches.customCursorIsOn)
                    {
                        texture2D = CoopKBnMPatches.playerOneCursor;
                        color = CoopKBnMPatches.playerOneCursorModulation;
                        scale = CoopKBnMPatches.playerOneCursorScale;
                        if (texture2D == null)
                        {
                            texture2D = gameCursorController.normalCursor;
                            int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                            if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                                texture2D = gameCursorController.cursors[currentCursorIndex];
                        }
                    }
                    else
                    {
                        texture2D = gameCursorController.normalCursor;
                        int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                        if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                            texture2D = gameCursorController.cursors[currentCursorIndex];
                    }

                    Vector2 pos = camera.WorldToViewportPoint(primaryPlayer.CenterPosition + instanceForPlayer.ActiveActions.Aim.Vector.normalized * 5f);
                    Vector2 vector2 = BraveCameraUtility.ConvertGameViewportToScreenViewport(pos);
                    Vector2 vector3 = new Vector2(vector2.x * (float)Screen.width, vector2.y * (float)Screen.height);
                    Vector2 vector4 = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                    Rect screenRect2 = new Rect((vector3.x + 0.5f - vector4.x / 2f - Screen.width / 2) * WindowManager.referenceSecondWindowWidth / Screen.width, (vector3.y + 0.5f - vector4.y / 2f - Screen.height / 2) * WindowManager.referenceSecondWindowHeight / Screen.height,
                        vector4.x * WindowManager.referenceSecondWindowWidth / Screen.width, vector4.y * WindowManager.referenceSecondWindowHeight / Screen.height);
                    screenRect2 = new Rect(screenRect2.x, screenRect2.y + screenRect2.height, screenRect2.width, -screenRect2.height);
                    RenderTexture.active = targetTexture;
                    Graphics.DrawTexture(screenRect2, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                    RenderTexture.active = null;
                }
            }
            PlayerController secondaryPlayer = GameManager.Instance.SecondaryPlayer;
            if (!CoopKBnM.OptionsManager.restrictMouseInputPort && CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera && GameCursorController.showPlayerTwoControllerCursor && !GameManager.Instance.IsPaused && !GameManager.IsBossIntro)
            {
                BraveInput instanceForPlayer2 = BraveInput.GetInstanceForPlayer(1);
                if (secondaryPlayer && instanceForPlayer2.ActiveActions.Aim.Vector != Vector2.zero && (secondaryPlayer.CurrentInputState == PlayerInputState.AllInput || secondaryPlayer.IsInMinecart))
                {
                    Texture2D texture2D;
                    Color color = new Color(0.402f, 0.111f, 0.32f);
                    float scale = 1f;
                    if (CoopKBnMPatches.customCursorIsOn)
                    {
                        texture2D = CoopKBnMPatches.playerTwoCursor;
                        color = CoopKBnMPatches.playerTwoCursorModulation;
                        scale = CoopKBnMPatches.playerTwoCursorScale;
                        if (texture2D == null)
                        {
                            texture2D = gameCursorController.normalCursor;
                            int currentCursorIndex = GameManager.Options.CurrentCursorIndex;
                            if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                                texture2D = gameCursorController.cursors[currentCursorIndex];
                        }
                    }
                    else
                    {
                        texture2D = gameCursorController.normalCursor;
                        int currentCursorIndex = GameManager.Options.CurrentCursorIndex;

                        if (currentCursorIndex >= 0 && currentCursorIndex < gameCursorController.cursors.Length)
                            texture2D = gameCursorController.cursors[currentCursorIndex];
                    }

                    Vector2 pos2 = camera.WorldToViewportPoint(secondaryPlayer.CenterPosition + instanceForPlayer2.ActiveActions.Aim.Vector.normalized * 5f);
                    Vector2 vector5 = BraveCameraUtility.ConvertGameViewportToScreenViewport(pos2);
                    Vector2 vector6 = new Vector2(vector5.x * (float)Screen.width, vector5.y * (float)Screen.height);
                    Vector2 vector7 = new Vector2((float)texture2D.width, (float)texture2D.height) * (float)((!(Pixelator.Instance != null)) ? 3 : ((int)Pixelator.Instance.ScaleTileScale)) * scale;
                    Rect screenRect3 = new Rect((vector6.x + 0.5f - vector7.x / 2f - Screen.width / 2) * WindowManager.referenceSecondWindowWidth / Screen.width, (vector6.y + 0.5f - vector7.y / 2f - Screen.height / 2) * WindowManager.referenceSecondWindowHeight / Screen.height,
                        vector7.x * WindowManager.referenceSecondWindowWidth / Screen.width, vector7.y * WindowManager.referenceSecondWindowHeight / Screen.height);
                    screenRect3 = new Rect(screenRect3.x, screenRect3.y + screenRect3.height, screenRect3.width, -screenRect3.height);
                    RenderTexture.active = targetTexture;
                    Graphics.DrawTexture(screenRect3, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, color);
                    RenderTexture.active = null;
                }
            }
        }

        private static void ChangeMouseSensitivityMultipliers()
        {
            if (Camera.main == null)
                return;

            if (CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera)
            {
                RawInputHandler.playerOneMouseSensitivityMultiplier = 1f;
                RawInputHandler.playerTwoMouseSensitivityMultiplier = (float)Camera.main.pixelWidth / secondWindowPixelWidth;
            }
            else
            {
                RawInputHandler.playerOneMouseSensitivityMultiplier = (float)Camera.main.pixelWidth / secondWindowPixelWidth;
                RawInputHandler.playerTwoMouseSensitivityMultiplier = 1f;
            }
        }

        public static Camera GetCameraForPlayer(PlayerController player, Camera orig = null)
        {
            if (!GameManager.HasInstance)
                return null;

            if (player == null)
                return null;

            if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                return orig == null ? Camera.main : orig;

            if (camera == null || originalCamera == null)
                return orig == null ? Camera.main : orig;

            if (cameraController.m_player == player)
                return camera;
            else
                return originalCamera;
        }

        public static CameraController GetCameraControllerForPlayer(PlayerController player, CameraController orig = null)
        {
            if (!GameManager.HasInstance)
                return null;

            if (player == null)
                return null;

            if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                return orig == null ? GameManager.Instance.MainCameraController : orig;

            if (cameraController == null || originalCameraController == null)
                return orig == null ? GameManager.Instance.MainCameraController : orig;

            if (cameraController.m_player == player)
                return cameraController;
            else
                return originalCameraController;
        }

        public static CameraController GetCameraControllerForPlayer(int playerIDX, CameraController orig = null)
        {
            if (!GameManager.HasInstance)
                return null;

            if (playerIDX != 0 && playerIDX != 1)
                return null;

            if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                return orig == null ? GameManager.Instance.MainCameraController : orig;

            if (cameraController == null || originalCameraController == null)
                return orig == null ? GameManager.Instance.MainCameraController : orig;

            if (cameraController.m_player.PlayerIDX == playerIDX)
                return cameraController;
            else
                return originalCameraController;
        }

        public static Pixelator GetPixelatorForPlayer(PlayerController player, Pixelator orig = null)
        {
            if (!GameManager.HasInstance)
                return null;

            if (player == null)
                return null;

            if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                return orig == null ? Pixelator.Instance : orig;

            if (cameraController == null || originalCameraController == null)
                return orig == null ? Pixelator.Instance : orig;

            if (cameraController.m_player == player)
                return cameraPixelator;
            else
                return originCameraPixelator;
        }

        public static PlayerController GetPlayerForCameraController(CameraController controller)
        {
            if (!GameManager.HasInstance)
                return null;

            if (controller == null)
                return null;

            if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                return GameManager.Instance.PrimaryPlayer;

            if (cameraController == null || originalCameraController == null)
                return GameManager.Instance.PrimaryPlayer;

            if (OptionsManager.playerOneCamera == 0)
                return controller == originalCameraController ? GameManager.Instance.PrimaryPlayer : GameManager.Instance.SecondaryPlayer;
            else if (OptionsManager.playerOneCamera == 1)
                return controller != originalCameraController ? GameManager.Instance.PrimaryPlayer : GameManager.Instance.SecondaryPlayer;
            else
            {
                if (CoopKBnM.OptionsManager.restrictMouseInputPort || !BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && !BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false))
                    return controller == originalCameraController ? GameManager.Instance.PrimaryPlayer : GameManager.Instance.SecondaryPlayer;
                else
                {
                    if (controller == originalCameraController)
                        return BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) ? GameManager.Instance.PrimaryPlayer : GameManager.Instance.SecondaryPlayer;
                    else
                        return BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false) ? GameManager.Instance.PrimaryPlayer : GameManager.Instance.SecondaryPlayer;
                }
            }
        }

        internal static IEnumerator UpdatePlayerAndCameraBindings()
        {
            yield return null;
            yield return null;
            yield return null;
            CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera = IsPrimaryPlayerOnMainCamera();
            if (originalCameraController != null)
            {
                originalCameraController.m_player = GetPlayerForCameraController(originalCameraController);
                if (originalCameraController.m_player != null)
                    Debug.Log($"Coop View: Update player of main camera to {originalCameraController.m_player.PlayerIDX}");
                else
                    Debug.Log($"Coop View: Update player of main camera to null");
            }
            if (cameraController != null)
            {
                cameraController.m_player = GetPlayerForCameraController(cameraController);
                if (cameraController.m_player != null)
                    Debug.Log($"Coop View: Update player of second camera to {cameraController.m_player.PlayerIDX}");
                else
                    Debug.Log($"Coop View: Update player of second camera to null");
            }
            ChangeMouseSensitivityMultipliers();
        }

        public static bool IsPrimaryPlayerOnMainCamera()
        {
            if (originalCameraController == null)
                return true;
            return originalCameraController.m_player == GameManager.Instance.PrimaryPlayer;
        }

        public static GameObject GetAnotherThreatArrowForCameraController(CameraController controller)
        {
            return controller.m_player.IsPrimaryPlayer ? secondaryThreatArrow : primaryThreatArrow;
        }
    }
}
