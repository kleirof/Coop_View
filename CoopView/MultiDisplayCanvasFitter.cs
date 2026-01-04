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
            rt.sizeDelta = new Vector2(1920f, 1080f);
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
            cam.targetDisplay = displayIndex;

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.Depth;
            cam.backgroundColor = Color.clear;

            cam.cullingMask = 1 << canvas.gameObject.layer;

            cam.depth = 1000f;
            cam.transform.position = new Vector3(0, 0, -1000f);
            cam.transform.rotation = Quaternion.identity;

            camObj.AddComponent<MultiDisplayCanvasFitter>().Init(cam, canvas);

            return cam;
        }

        public void Init(Camera camera, Canvas canvas)
        {
            this.camera = camera;
            canvasRT = canvas.GetComponent<RectTransform>();

            baseWidth = canvasRT.sizeDelta.x;
            baseHeight = canvasRT.sizeDelta.y;

            displayIndex = camera.targetDisplay;

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
            int secondWindowHeight = WindowManager.SecondWindowHeight;
            camera.orthographicSize = baseHeight / ViewController.secondWindowPixelHeight * secondWindowHeight * 0.5f;
            camera.aspect = (float)WindowManager.SecondWindowWidth / secondWindowHeight;
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