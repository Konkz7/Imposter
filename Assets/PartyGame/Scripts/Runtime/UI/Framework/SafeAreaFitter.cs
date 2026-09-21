using UnityEngine;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Keeps content clear of notches, cutouts and home indicators. Re-applies on rotation
    /// and resolution changes so nothing important ever sits under system UI.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;

        /// <summary>Extra inset in reference pixels, applied on top of the device safe area.</summary>
        public float ExtraTop = 0f;
        public float ExtraBottom = 0f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _lastSafeArea = Screen.safeArea;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);

            var area = Screen.safeArea;
            var min = area.position;
            var max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = new Vector2(0f, ExtraBottom);
            _rect.offsetMax = new Vector2(0f, -ExtraTop);
        }
    }
}
