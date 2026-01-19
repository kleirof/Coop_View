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

        private static GameObject overlayRawImageObject;
        private static RawImage overlayRawImage;
        internal static RenderTexture overlayRenderTexture;

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

        internal static GameCursorController gameCursorController;

        internal static BraveOptionsMenuItem resolutionOptionsMenuItem;
        internal static BraveOptionsMenuItem screenModeOptionsMenuItem;

        private static Rect rectFullscreen;
        private static Rect rectSmall;

        private bool clearable = false;

        internal static int secondWindowPixelWidth;
        internal static int secondWindowPixelHeight;

        internal static bool secondWindowActive = false;

        internal static AssetBundle coopViewAssets;
        private static Shader ignoreAlphaShader;
        private static Material ignoreAlphaMaterial;
        private static Shader overlayOnBackgroundShader;
        private static Material overlayOnBackgroundMaterial;
        private static Material overlayOnBackgroundMaterialNotBrighter;

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

        private static Camera secondWindowClearCamera;

        internal static Canvas simpleStatsCanvas;
        internal static Camera uiMainDisplayCamera;
        internal static Camera uiSecondDisplayCamera;
        private static WorldCursorController worldCursorController;
        internal static GameObject worldCursorObject;

        internal static bool simplestatsLoaded;

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

                canvasObject = new GameObject("Coop View Canvas");
                DontDestroyOnLoad(canvasObject);

                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                int wsLayer = 31;
                canvas.gameObject.layer = wsLayer;

                RectTransform rt = canvas.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(WindowManager.referenceSecondWindowWidth, WindowManager.referenceSecondWindowHeight);

                canvas.transform.position = new Vector3(0, 0, -100);

                GameObject canvasCameraObject = new GameObject("Coop View Canvas Camera");
                DontDestroyOnLoad(canvasCameraObject);

                Camera canvasCamera = canvasCameraObject.AddComponent<Camera>();
                canvasCamera.targetDisplay = 1;
                canvasCamera.orthographic = true;
                canvasCamera.orthographicSize = WindowManager.referenceSecondWindowHeight * 0.5f;
                canvasCamera.clearFlags = CameraClearFlags.Depth;
                canvasCamera.backgroundColor = Color.clear;
                canvasCamera.cullingMask = 1 << wsLayer;
                canvasCamera.depth = 1000;
                canvasCamera.nearClipPlane = 0.01f;
                canvasCamera.farClipPlane = 1000f;

                canvasCamera.transform.position = new Vector3(0, 0, -200);
                canvasCamera.transform.rotation = Quaternion.identity;

                Camera mainCam = Camera.main;
                ExcludeGameObjectLayerFromCamera(mainCam, canvasObject);

                GameObject clearCameraObject = new GameObject("Coop View Clear Camera");
                DontDestroyOnLoad(clearCameraObject);

                secondWindowClearCamera = clearCameraObject.AddComponent<Camera>();

                secondWindowClearCamera.targetDisplay = 1;
                secondWindowClearCamera.rect = new Rect(0, 0, 1, 1);
                secondWindowClearCamera.clearFlags = CameraClearFlags.SolidColor;
                secondWindowClearCamera.backgroundColor = Color.black;
                secondWindowClearCamera.cullingMask = 0;
                secondWindowClearCamera.depth = -10000f;
                secondWindowClearCamera.useOcclusionCulling = false;
                secondWindowClearCamera.allowHDR = false;
                secondWindowClearCamera.allowMSAA = false;
                secondWindowClearCamera.orthographic = true;
                secondWindowClearCamera.orthographicSize = 1;
                secondWindowClearCamera.nearClipPlane = 0.01f;
                secondWindowClearCamera.farClipPlane = 100f;

                WindowManager.InitWindowHook();
                ShortcutKeyHandler.InitMessageHandler();

                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("CoopView.coopview_assets"))
                {
                    coopViewAssets = AssetBundle.LoadFromStream(s);
                }

                ignoreAlphaShader = coopViewAssets.LoadAsset<Shader>("IgnoreAlpha");
                ignoreAlphaMaterial = new Material(ignoreAlphaShader);

                overlayOnBackgroundShader = coopViewAssets.LoadAsset<Shader>("OverlayOnBackground");
                overlayOnBackgroundMaterial = new Material(overlayOnBackgroundShader);

                overlayOnBackgroundMaterialNotBrighter = new Material(overlayOnBackgroundShader);
                overlayOnBackgroundMaterialNotBrighter.SetFloat("_Brightness", 1f);
                overlayOnBackgroundMaterialNotBrighter.SetFloat("_AlphaMultiplier", 3.6f);

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
                        ExcludeGameObjectLayerFromCamera(weirdoUiCamera, canvasObject);
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
                            UpdateSecondWindowPixelSize();
                        }

                        ExcludeGameObjectLayerFromCamera(originalCamera, canvasObject);
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
                            ExcludeGameObjectLayerFromCamera(camera, canvasObject);
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
                        ExcludeGameObjectLayerFromCamera(uiCamera, canvasObject);
                        clearable = true;
                    }
                }

                if (minimapUiCamera == null)
                {
                    GameObject minimapRootObject = GameObject.Find("Minimap UI Root");
                    if (minimapRootObject != null)
                    {
                        minimapUiCamera = minimapRootObject.GetComponentInChildren<Camera>();
                        ExcludeGameObjectLayerFromCamera(minimapUiCamera, canvasObject);
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
                        ExcludeGameObjectLayerFromCamera(minimapCamera, canvasObject);
                        clearable = true;
                    }
                }

                if (ammoCamera == null)
                {
                    GameObject ammoRootObject = AmmonomiconController.ForceInstance?.gameObject?.transform?.parent?.gameObject;
                    if (ammoRootObject != null)
                    {
                        ammoCamera = ammoRootObject.GetComponentInChildren<Camera>();
                        ExcludeGameObjectLayerFromCamera(ammoCamera, canvasObject);
                        clearable = true;
                    }
                }

                if (secondWindowActive && (rawImageObject == null || uiRawImageObject == null))
                {
                    UpdateSecondWindowPixelSize();

                    renderTexture = new RenderTexture(WindowManager.startupWidth, WindowManager.startupHeight, 0, RenderTextureFormat.ARGB32);
                    renderTexture.enableRandomWrite = true;
                    renderTexture.filterMode = FilterMode.Point;
                    renderTexture.useMipMap = false;
                    renderTexture.Create();

                    rawImageObject = new GameObject("rawImage");
                    DontDestroyOnLoad(rawImageObject);
                    rawImageObject.transform.SetParent(canvasObject.transform);
                    rawImageObject.gameObject.layer = canvasObject.gameObject.layer;
                    rawImageObject.transform.position = canvasObject.transform.position.WithZ(-100f);
                    rawImage = rawImageObject.AddComponent<RawImage>();
                    RectTransform rectTransform = rawImageObject.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(Mathf.RoundToInt((float)secondWindowPixelWidth * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), Mathf.RoundToInt((float)secondWindowPixelHeight * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
                    rectTransform.anchoredPosition = Vector2.zero;
                    rawImage.raycastTarget = false;
                    rawImage.texture = renderTexture;
                    rawImage.material = ignoreAlphaMaterial;

                    uiRenderTexture = new RenderTexture(Mathf.RoundToInt((float)secondWindowPixelWidth / originalCamera.rect.width), Mathf.RoundToInt((float)secondWindowPixelHeight / originalCamera.rect.height), 0, RenderTextureFormat.ARGB32);
                    uiRenderTexture.enableRandomWrite = true;
                    uiRenderTexture.filterMode = FilterMode.Point;
                    uiRenderTexture.useMipMap = false;
                    uiRenderTexture.Create();

                    uiRawImageObject = new GameObject("uiRawImage");
                    DontDestroyOnLoad(uiRawImageObject);
                    uiRawImageObject.transform.SetParent(canvasObject.transform);
                    uiRawImageObject.gameObject.layer = canvasObject.gameObject.layer;
                    uiRawImageObject.transform.position = canvasObject.transform.position.WithZ(-100f);
                    uiRawImage = uiRawImageObject.AddComponent<RawImage>();
                    RectTransform uiRectTransform = uiRawImageObject.GetComponent<RectTransform>();
                    uiRectTransform.sizeDelta = new Vector2(Mathf.RoundToInt((float)secondWindowPixelWidth / originalCamera.rect.width * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), Mathf.RoundToInt((float)secondWindowPixelHeight / originalCamera.rect.height * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
                    uiRectTransform.anchoredPosition = Vector2.zero;
                    uiRawImage.raycastTarget = false;
                    uiRawImage.texture = uiRenderTexture;
                    uiRawImage.material = overlayOnBackgroundMaterialNotBrighter;


                    if (simplestatsLoaded)
                    {
                        overlayRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                        overlayRenderTexture.enableRandomWrite = true;
                        overlayRenderTexture.filterMode = FilterMode.Point;
                        overlayRenderTexture.useMipMap = false;
                        overlayRenderTexture.antiAliasing = 1;
                        overlayRenderTexture.anisoLevel = 0;
                        overlayRenderTexture.Create();

                        overlayRawImageObject = new GameObject("overlayRawImage");
                        DontDestroyOnLoad(overlayRawImageObject);
                        overlayRawImageObject.transform.SetParent(canvasObject.transform);
                        overlayRawImageObject.gameObject.layer = canvasObject.gameObject.layer;
                        overlayRawImageObject.transform.position = canvasObject.transform.position.WithZ(-100f);
                        overlayRawImage = overlayRawImageObject.AddComponent<RawImage>();
                        RectTransform overlayRectTransform = overlayRawImageObject.GetComponent<RectTransform>();
                        overlayRectTransform.sizeDelta = new Vector2(Mathf.RoundToInt((float)secondWindowPixelWidth * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), Mathf.RoundToInt((float)secondWindowPixelHeight * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
                        overlayRectTransform.anchoredPosition = Vector2.zero;
                        overlayRawImage.raycastTarget = false;
                        overlayRawImage.texture = overlayRenderTexture;
                        overlayRawImage.material = overlayOnBackgroundMaterial;


                        if (uiMainDisplayCamera != null)
                            uiMainDisplayCamera.GetComponent<MultiDisplayCanvasFitter>()?.ForceRefresh();

                        if (uiSecondDisplayCamera != null)
                        {
                            uiSecondDisplayCamera.targetTexture = overlayRenderTexture;

                            uiSecondDisplayCamera.GetComponent<MultiDisplayCanvasFitter>()?.ForceRefresh();
                            uiSecondDisplayCamera.enabled = true;
                        }
                    }

                    clearable = true;
                }

                if (worldCursorController == null)
                {
                    worldCursorObject = new GameObject("WorldCursor");
                    worldCursorObject.transform.SetParent(canvasObject.transform);
                    worldCursorObject.layer = canvasObject.gameObject.layer;

                    worldCursorController = worldCursorObject.AddComponent<WorldCursorController>();

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
                        camera.aspect = 16f / 9;
                    }

                    if (simpleStatsCanvas != null)
                    {
                        simpleStatsCanvas.renderMode = RenderMode.WorldSpace;
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

                if (worldCursorController != null)
                    worldCursorController.DrawCursor();
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

        private static void ClearRenderTexture(RenderTexture rt)
        {
            if (rt == null)
                return;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            GL.Flush();
            RenderTexture.active = null;
        }

        private IEnumerator ClearCaches()
        {
            ClearRenderTexture(renderTexture);
            ClearRenderTexture(uiRenderTexture);

            yield return null;

            if (renderTexture != null)
                renderTexture.DiscardContents();
            if (uiRenderTexture != null)
                uiRenderTexture.DiscardContents();

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
            if (overlayRenderTexture != null)
            {
                overlayRenderTexture.Release();
                overlayRenderTexture = null;
            }
            if (rawImageObject != null)
            {
                Destroy(rawImageObject);
                rawImage = null;
                rawImageObject = null;
            }
            if (uiRawImageObject != null)
            {
                Destroy(uiRawImageObject);
                uiRawImage = null;
                uiRawImageObject = null;
            }
            if (overlayRawImageObject != null)
            {
                Destroy(overlayRawImageObject);
                overlayRawImage = null;
                overlayRawImageObject = null;
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
            if (worldCursorObject != null)
            {
                Destroy(worldCursorObject);
                worldCursorObject = null;
                worldCursorController = null;
            }
            if (simplestatsLoaded && uiSecondDisplayCamera != null)
            {
                uiSecondDisplayCamera.targetTexture = null;
                uiSecondDisplayCamera.enabled = false;
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

            yield return null;

            UpdateSecondWindowPixelSize();

            if (rawImageObject != null)
            {
                RectTransform rectTransform = rawImageObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(Mathf.RoundToInt((float)secondWindowPixelWidth * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), Mathf.RoundToInt((float)secondWindowPixelHeight * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
            }

            if (uiRawImageObject != null)
            {
                uiRenderTexture.Release();
                uiRenderTexture = new RenderTexture(Mathf.RoundToInt((float)WindowManager.referenceSecondWindowWidth / originalCamera.rect.width), Mathf.RoundToInt((float)WindowManager.referenceSecondWindowHeight / originalCamera.rect.height), 0, RenderTextureFormat.ARGB32);
                uiRenderTexture.enableRandomWrite = true;
                uiRenderTexture.filterMode = FilterMode.Point;
                uiRenderTexture.useMipMap = false;
                uiRenderTexture.Create();

                RectTransform uiRectTransform = uiRawImageObject.GetComponent<RectTransform>();
                uiRectTransform.sizeDelta = new Vector2(Mathf.RoundToInt((float)secondWindowPixelWidth / originalCamera.rect.width * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), Mathf.RoundToInt((float)secondWindowPixelHeight / originalCamera.rect.height * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));

                uiRawImage.texture = null;
                uiRawImage.color = Color.black;
                yield return null;
                uiRawImage.color = Color.white;
                uiRawImage.texture = uiRenderTexture;
            }

            if (overlayRawImageObject != null)
            {
                RectTransform rectTransform = overlayRawImageObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(Mathf.RoundToInt((float)secondWindowPixelWidth * WindowManager.referenceSecondWindowWidth / WindowManager.SecondWindowWidth), Mathf.RoundToInt((float)secondWindowPixelHeight * WindowManager.referenceSecondWindowHeight / WindowManager.SecondWindowHeight));
            }

            if (originalCamera != null)
            {
                rectFullscreen = originalCamera.rect;
                rectSmall = new Rect(0.25f * rectFullscreen.width + 0.5f, 0.1625f * rectFullscreen.height + 0.5f, 0.25f * rectFullscreen.width, 0.25f * rectFullscreen.height);
            }

            if (simplestatsLoaded)
            {
                uiMainDisplayCamera?.GetComponent<MultiDisplayCanvasFitter>()?.ForceRefresh();
                uiSecondDisplayCamera?.GetComponent<MultiDisplayCanvasFitter>()?.ForceRefresh();
            }

            ChangeMouseSensitivityMultipliers();
        }

        private static void ChangeMouseSensitivityMultipliers()
        {
            if (Camera.main == null)
                return;

            if (CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera)
            {
                RawInputHandler.playerOneMouseSensitivityMultiplier = 1f;
                RawInputHandler.playerTwoMouseSensitivityMultiplier = (float)Camera.main.pixelWidth / ViewController.secondWindowPixelWidth;
            }
            else
            {
                RawInputHandler.playerOneMouseSensitivityMultiplier = (float)Camera.main.pixelWidth / ViewController.secondWindowPixelWidth;
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

        public static void UpdateSecondWindowPixelSize()
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

        private void ExcludeGameObjectLayerFromCamera(Camera camera, GameObject gameObject)
        {
            if (camera != null && gameObject != null)
                camera.cullingMask &= ~(1 << gameObject.layer);
        }
    }
}
