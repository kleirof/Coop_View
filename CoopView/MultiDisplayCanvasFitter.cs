using UnityEngine;

namespace CoopView
{
    public class MultiDisplayCanvasFitter : MonoBehaviour
    {
        private Camera camera;
        private RectTransform canvasRT;

        private float baseWidth;
        private float baseHeight;

        private int displayIndex;

        internal static void SetupCanvas(Canvas canvas)
        {
            if (canvas == null || canvas.gameObject == null)
                return;

            canvas.gameObject.layer = 30;
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920, 1080);

            rt.localScale = Vector3.one;
            rt.position = Vector3.zero;
        }

        internal static Camera CreateUICamera(string name, int displayIndex, Canvas canvas)
        {
            if (canvas == null || canvas.gameObject == null)
                return null;

            GameObject camObj = new GameObject(name);
            DontDestroyOnLoad(camObj);

            Camera cam = camObj.AddComponent<Camera>();

            cam.orthographic = true;
            cam.clearFlags = displayIndex == 0 ? CameraClearFlags.Depth : CameraClearFlags.Color;
            cam.backgroundColor = Color.clear;

            cam.cullingMask = 1 << canvas.gameObject.layer;

            cam.depth = 1000f;
            cam.transform.position = new Vector3(0, 0, -1000f);
            cam.transform.rotation = Quaternion.identity;

            camObj.AddComponent<MultiDisplayCanvasFitter>().Init(cam, displayIndex, canvas);

            return cam;
        }

        public void Init(Camera camera, int displayIndex, Canvas canvas)
        {
            this.camera = camera;
            canvasRT = canvas.GetComponent<RectTransform>();

            baseWidth = canvasRT.sizeDelta.x;
            baseHeight = canvasRT.sizeDelta.y;

            this.displayIndex = displayIndex;

            if (displayIndex != 0)
                this.camera.enabled = false;

            ForceRefresh();
        }

        private void UpdateDisplay0()
        {
            float aspect = (float)Screen.width / Screen.height;

            float halfH = baseHeight * 0.5f;
            float halfW = baseWidth * 0.5f;

            camera.orthographicSize = Mathf.Max(halfH, halfW / aspect);
        }

        private void UpdateDisplay1()
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float baseSize = Mathf.Max(baseHeight * 0.5f, baseWidth * 0.5f / screenAspect);
            float referenceAspect = 16f / 9;
            bool isWideScreen = screenAspect >= referenceAspect;
            Rect cameraRect = Camera.main.rect;
            float cameraRectDimension = isWideScreen ? cameraRect.width : cameraRect.height;
            float aspectRatioCorrection = (float)Camera.main.pixelWidth / Camera.main.pixelHeight / referenceAspect;

            camera.orthographicSize = baseSize * cameraRectDimension * aspectRatioCorrection;
        }

        public void ForceRefresh()
        {
            if (camera == null)
                return;

            if (displayIndex == 0)
                UpdateDisplay0();
            else
                UpdateDisplay1();
        }
    }
}