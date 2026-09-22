using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.Tests.PlayMode
{
    /// <summary>
    /// Renders the live UI at a real phone resolution and writes it to a PNG.
    ///
    /// The canvas is switched to world space and framed by an orthographic camera for the whole
    /// session, so every screen is built against an exact 1080x1920 rect rather than whatever
    /// size the editor game view happens to be. That makes the captures a faithful preview.
    /// </summary>
    public static class ScreenshotCapture
    {
        /// <summary>Google Play's phone screenshot size, and the app's design resolution.</summary>
        public const int DefaultWidth = 1080;
        public const int DefaultHeight = 1920;

        public static int Width { get; private set; } = DefaultWidth;
        public static int Height { get; private set; } = DefaultHeight;

        public static string OutputFolder =
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");

        private static Canvas _canvas;
        private static CanvasScaler _scaler;
        private static Camera _camera;
        private static RenderTexture _renderTexture;

        private static RenderMode _previousMode;
        private static Camera _previousCamera;
        private static bool _previousScalerEnabled;
        private static Vector2 _previousSize;
        private static Vector3 _previousPosition;
        private static Vector3 _previousScale;

        public static void BeginSession(Canvas canvas, int width = DefaultWidth, int height = DefaultHeight)
        {
            if (canvas == null || _canvas != null) return;

            Width = Mathf.Max(64, width);
            Height = Mathf.Max(64, height);
            _canvas = canvas;
            var rect = (RectTransform)canvas.transform;
            _scaler = canvas.GetComponent<CanvasScaler>();

            _previousMode = canvas.renderMode;
            _previousCamera = canvas.worldCamera;
            _previousScalerEnabled = _scaler != null && _scaler.enabled;
            _previousSize = rect.sizeDelta;
            _previousPosition = rect.position;
            _previousScale = rect.localScale;

            if (_scaler != null) _scaler.enabled = false;

            canvas.renderMode = RenderMode.WorldSpace;
            rect.sizeDelta = new Vector2(Width, Height);
            rect.localScale = Vector3.one;
            rect.position = Vector3.zero;
            rect.rotation = Quaternion.identity;

            _renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };

            var cameraObject = new GameObject("Screenshot Camera");
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 5000f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.07f, 0.06f, 0.11f, 1f);
            _camera.targetTexture = _renderTexture;
            _camera.enabled = false;

            canvas.worldCamera = _camera;
        }

        public static void Shot(string fileName)
        {
            if (_canvas == null || _camera == null) return;

            var rect = (RectTransform)_canvas.transform;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            // Frame the canvas from its real world corners so the capture matches the rect
            // exactly, whatever the canvas pivot happens to be.
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var centre = (corners[0] + corners[2]) * 0.5f;
            var worldHeight = Vector3.Distance(corners[0], corners[1]);

            _camera.orthographicSize = Mathf.Max(1f, worldHeight * 0.5f);
            _camera.transform.position = new Vector3(centre.x, centre.y, centre.z - 1000f);
            _camera.transform.rotation = Quaternion.identity;
            _camera.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;

            Directory.CreateDirectory(OutputFolder);
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName + ".png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        public static void EndSession()
        {
            if (_canvas == null) return;

            var rect = (RectTransform)_canvas.transform;
            _canvas.worldCamera = _previousCamera;
            _canvas.renderMode = _previousMode;
            rect.sizeDelta = _previousSize;
            rect.position = _previousPosition;
            rect.localScale = _previousScale;
            if (_scaler != null) _scaler.enabled = _previousScalerEnabled;

            if (_camera != null)
            {
                _camera.targetTexture = null;
                Object.DestroyImmediate(_camera.gameObject);
            }
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.DestroyImmediate(_renderTexture);
            }

            _canvas = null;
            _scaler = null;
            _camera = null;
            _renderTexture = null;
            Width = DefaultWidth;
            Height = DefaultHeight;
        }
    }
}
