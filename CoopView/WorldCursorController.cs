using CoopKBnM;
using UnityEngine;
using UnityEngine.UI;

namespace CoopView
{
    public sealed class WorldCursorController : MonoBehaviour
    {
        private RawImage cursorImage;
        private RectTransform cursorRect;

        public void Start()
        {
            if (cursorImage != null) return;

            cursorImage = ViewController.worldCursorObject.AddComponent<RawImage>();
            cursorImage.raycastTarget = false;

            cursorImage.color = new Color(1f, 1f, 1f, 1f);
            cursorImage.material = null;

            cursorRect = ViewController.worldCursorObject.GetComponent<RectTransform>();
            cursorRect.pivot = new Vector2(0.5f, 0.5f);
            cursorRect.sizeDelta = new Vector2(0f, 0f);
        }

        public void UpdateCursorPosition(Vector2 screenPos)
        {
            if (cursorRect == null) return;
            cursorRect.position = new Vector3(Mathf.RoundToInt(screenPos.x), Mathf.RoundToInt(screenPos.y), 0);
        }

        public void SetCursor(Texture2D tex, Color color, float scale, float widthScale = 1f, float heightScale = 1f)
        {
            if (tex == null || cursorImage == null) return;

            SetCursorVisible(true);

            Color opaqueColor = new Color(color.r, color.g, color.b, color.a) * 2f;
            cursorImage.color = opaqueColor;
            if (cursorImage.texture != tex)
                cursorImage.texture = tex;
            cursorImage.material = null;

            if (cursorRect != null)
            {
                float width = tex.width * scale * widthScale;
                float height = tex.height * scale * heightScale;
                cursorRect.sizeDelta = new Vector2(width, height);
            }
        }

        public void SetCursorVisible(bool visible)
        {
            if (cursorImage != null)
                cursorImage.enabled = visible;
        }

        internal void DrawCursor()
        {
            if (!GameManager.HasInstance)
                return;
            if (ViewController.gameCursorController == null)
                return;
            if (!enabled)
                return;

            bool hasCursorToDraw = false;

            if (GameCursorController.showMouseCursor &&
                GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER &&
                RawInputHandler.ShowPublicCursor)
            {
                hasCursorToDraw = ProcessMouseCursor(
                    isPrimary: !(!BraveInput.GetInstanceForPlayer(0).IsKeyboardAndMouse(false) && BraveInput.GetInstanceForPlayer(1).IsKeyboardAndMouse(false)),
                    mousePosition: RawInputHandler.FirstMousePosition
                );
            }
            else if ((RawInputHandler.ShowPlayerOneMouseCursor && !CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera) ||
                (RawInputHandler.ShowPlayerTwoMouseCursor && CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera))
            {
                Vector2 mousePos = ResolveMousePosition();
                hasCursorToDraw = ProcessMouseCursor(
                    isPrimary: !CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera,
                    mousePosition: mousePos
                );
            }

            if (!CoopKBnM.OptionsManager.restrictMouseInputPort && !CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera
                && GameCursorController.showPlayerOneControllerCursor && !GameManager.Instance.IsPaused && !GameManager.IsBossIntro)
            {
                hasCursorToDraw = ProcessControllerCursor(
                    GameManager.Instance.PrimaryPlayer,
                    BraveInput.GetInstanceForPlayer(0),
                    isPrimary: true
                );
            }

            if (!CoopKBnM.OptionsManager.restrictMouseInputPort && CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera
                && GameCursorController.showPlayerTwoControllerCursor && !GameManager.Instance.IsPaused && !GameManager.IsBossIntro)
            {
                hasCursorToDraw = ProcessControllerCursor(
                    GameManager.Instance.SecondaryPlayer,
                    BraveInput.GetInstanceForPlayer(1),
                    isPrimary: false
                );
            }

            if (!hasCursorToDraw)
                SetCursorVisible(false);
        }

        internal bool ProcessMouseCursor(bool isPrimary, Vector2 mousePosition)
        {
            Texture2D tex;
            Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            float scale = 1f;

            if (CoopKBnMPatches.customCursorIsOn)
            {
                tex = isPrimary
                    ? CoopKBnMPatches.playerOneCursor
                    : CoopKBnMPatches.playerTwoCursor;

                color = isPrimary
                    ? CoopKBnMPatches.playerOneCursorModulation
                    : CoopKBnMPatches.playerTwoCursorModulation;

                scale = isPrimary
                    ? CoopKBnMPatches.playerOneCursorScale
                    : CoopKBnMPatches.playerTwoCursorScale;

                if (tex == null)
                    tex = ResolveDefaultCursor();
            }
            else
            {
                tex = ResolveDefaultCursor();
                if (!isPrimary)
                    color = new Color(0.402f, 0.111f, 0.32f);
            }

            float pixelScale = 3f * scale;
            int secondWindowWitdh = WindowManager.SecondWindowWidth;
            int secondWindowHeight = WindowManager.SecondWindowHeight;
            int mainPixelWidth = Camera.main.pixelWidth;
            int mainPixelHeight = Camera.main.pixelHeight;

            float scaleX = (float)ViewController.secondWindowPixelWidth / secondWindowWitdh;
            float scaleY = (float)ViewController.secondWindowPixelHeight / secondWindowHeight;

            float screenX = ((mousePosition.x + 0.5f - (Screen.width - mainPixelWidth) * 0.5f) / mainPixelWidth * scaleX
                - (float)ViewController.secondWindowPixelWidth / secondWindowWitdh * 0.5f) * WindowManager.referenceSecondWindowWidth;
            float screenY = ((mousePosition.y + 0.5f - (Screen.height - mainPixelHeight) * 0.5f) / mainPixelHeight * scaleY
                - (float)ViewController.secondWindowPixelHeight / secondWindowHeight * 0.5f) * WindowManager.referenceSecondWindowHeight;

            SetCursor(tex, color, pixelScale, scaleX / 1920 * WindowManager.referenceSecondWindowWidth, scaleY / 1080 * WindowManager.referenceSecondWindowHeight);
            UpdateCursorPosition(new Vector2(screenX, screenY));

            return true;
        }

        internal bool ProcessControllerCursor(
            PlayerController player,
            BraveInput input,
            bool isPrimary)
        {
            if (!player || input == null)
                return false;

            if (GameManager.Instance.IsPaused || GameManager.IsBossIntro)
                return false;

            bool showControllerCursor = isPrimary
                ? GameCursorController.showPlayerOneControllerCursor
                : GameCursorController.showPlayerTwoControllerCursor;

            if (!showControllerCursor)
                return false;

            if (input.ActiveActions.Aim.Vector == Vector2.zero)
                return false;

            if (!(player.CurrentInputState == PlayerInputState.AllInput || player.IsInMinecart))
                return false;

            Texture2D tex;
            Color color = isPrimary
                ? new Color(0.5f, 0.5f, 0.5f, 0.5f)
                : new Color(0.402f, 0.111f, 0.32f);

            float scale = 1f;

            if (CoopKBnMPatches.customCursorIsOn)
            {
                tex = isPrimary
                    ? CoopKBnMPatches.playerOneCursor
                    : CoopKBnMPatches.playerTwoCursor;

                color = isPrimary
                    ? CoopKBnMPatches.playerOneCursorModulation
                    : CoopKBnMPatches.playerTwoCursorModulation;

                scale = isPrimary
                    ? CoopKBnMPatches.playerOneCursorScale
                    : CoopKBnMPatches.playerTwoCursorScale;

                if (tex == null)
                    tex = ResolveDefaultCursor();
            }
            else
            {
                tex = ResolveDefaultCursor();
            }

            Vector2 pos = ViewController.camera.WorldToViewportPoint(player.CenterPosition + input.ActiveActions.Aim.Vector.normalized * 5f);
            Vector2 vector2 = BraveCameraUtility.ConvertGameViewportToScreenViewport(pos);
            Vector2 vector3 = new Vector2(vector2.x * Camera.main.pixelWidth, vector2.y * Camera.main.pixelHeight);

            float pixelScale = 3f * scale;
            int secondWindowWitdh = WindowManager.SecondWindowWidth;
            int secondWindowHeight = WindowManager.SecondWindowHeight;
            int mainPixelWidth = Camera.main.pixelWidth;
            int mainPixelHeight = Camera.main.pixelHeight;

            float scaleX = (float)ViewController.secondWindowPixelWidth / secondWindowWitdh;
            float scaleY = (float)ViewController.secondWindowPixelHeight / secondWindowHeight;

            float screenX = (((vector3.x + 0.5f) / Camera.main.rect.width - (Screen.width - mainPixelWidth) * 0.5f) / mainPixelWidth * scaleX
                - (float)ViewController.secondWindowPixelWidth / secondWindowWitdh * 0.5f) * WindowManager.referenceSecondWindowWidth;
            float screenY = (((vector3.y + 0.5f) / Camera.main.rect.height - (Screen.height - mainPixelHeight) * 0.5f) / mainPixelHeight * scaleY
                - (float)ViewController.secondWindowPixelHeight / secondWindowHeight * 0.5f) * WindowManager.referenceSecondWindowHeight;

            SetCursor(tex, color, pixelScale, scaleX / 1920 * WindowManager.referenceSecondWindowWidth, scaleY / 1080 * WindowManager.referenceSecondWindowHeight);
            UpdateCursorPosition(new Vector2(screenX, screenY));

            return true;
        }

        internal Texture2D ResolveDefaultCursor()
        {
            Texture2D tex = ViewController.gameCursorController.normalCursor;
            int idx = GameManager.Options.CurrentCursorIndex;

            if (idx >= 0 && idx < ViewController.gameCursorController.cursors.Length)
                tex = ViewController.gameCursorController.cursors[idx];

            return tex;
        }

        internal Vector2 ResolveMousePosition()
        {
            if (!CoopKBnM.OptionsManager.restrictMouseInputPort)
                return RawInputHandler.FirstMousePosition;

            if (CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera)
                return CoopKBnM.OptionsManager.currentPlayerOneMousePort == 0
                    ? RawInputHandler.SecondMousePosition
                    : RawInputHandler.FirstMousePosition;

            return CoopKBnM.OptionsManager.currentPlayerOneMousePort != 0
                ? RawInputHandler.SecondMousePosition
                : RawInputHandler.FirstMousePosition;
        }
    }
}