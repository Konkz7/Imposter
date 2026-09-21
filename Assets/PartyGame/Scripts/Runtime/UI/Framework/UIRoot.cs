using PartyGame.UI.Design;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Builds the canvas, the safe area and the layer structure the whole app lives in.
    /// One canvas for every screen means transitions never flash the scene behind them,
    /// which matters when the previous screen was showing somebody's secret role.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        /// <summary>Portrait phone reference. Matched on both axes so tall and short phones both work.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        public Canvas Canvas { get; private set; }
        public RectTransform SafeArea { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }

        public static UIRoot Create(Transform parent)
        {
            var go = new GameObject("UI Root", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var root = go.AddComponent<UIRoot>();
            root.Build();
            return root;
        }

        private void Build()
        {
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.pixelPerfect = false;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Background sits outside the safe area so the colour bleeds under the notch.
            var background = UIFactory.CreateRect("Background", transform);
            var image = background.gameObject.AddComponent<Image>();
            image.sprite = UIGraphics.VerticalGradient(Theme.BackgroundTop, Theme.Background);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            SafeArea = UIFactory.CreateRect("Safe Area", transform);
            SafeArea.gameObject.AddComponent<SafeAreaFitter>();

            ScreenLayer = UIFactory.CreateRect("Screens", SafeArea);
            OverlayLayer = UIFactory.CreateRect("Overlay", SafeArea);

            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("Event System", typeof(EventSystem));
            DontDestroyOnLoad(go);
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
