using UnityEngine;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Keeps its target clear of the on-screen keyboard by raising the target's bottom edge to
    /// the top of the keyboard, so the layout inside re-flows into the space that is left. Only
    /// the covered part is given up and the top edge never moves, so the target cannot slide
    /// over whatever sits above it. Does nothing on platforms without a software keyboard.
    /// </summary>
    public class KeyboardAvoider : MonoBehaviour
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        private RectTransform _target;
        private float _restingBottom;
        private float _currentInset;

        public static KeyboardAvoider Attach(RectTransform target)
        {
            if (target == null) return null;
            var avoider = target.gameObject.AddComponent<KeyboardAvoider>();
            avoider._target = target;
            avoider._restingBottom = target.offsetMin.y;
            return avoider;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Sub-unit differences are float noise from the world-space maths; chasing them
            // would rebuild the layout every frame.
            var desired = CoveredHeight();
            if (Mathf.Abs(desired - _currentInset) < 0.5f) return;

            _currentInset = Mathf.MoveTowards(_currentInset, desired, Time.unscaledDeltaTime * 4000f);
            _target.offsetMin = new Vector2(_target.offsetMin.x, _restingBottom + _currentInset);
        }

        /// <summary>
        /// How far the keyboard reaches up into the target at rest, in the parent's units. Zero
        /// when the target already ends above the keyboard.
        /// </summary>
        private float CoveredHeight()
        {
            if (!TouchScreenKeyboard.visible) return 0f;

            // A docked keyboard rises from the bottom edge of the screen, so its height in
            // screen pixels is also where its top edge sits.
            var keyboardTop = TouchScreenKeyboard.area.height;
            if (keyboardTop <= 0f) return 0f;

            // The root canvas is screen-space overlay, so world units are screen pixels.
            var scale = _target.parent != null ? _target.parent.lossyScale.y : 1f;
            if (scale <= 0f) return 0f;

            _target.GetWorldCorners(Corners);
            var restingBottom = Corners[0].y - _currentInset * scale;
            return Mathf.Max(0f, (keyboardTop - restingBottom) / scale);
        }

        private void OnDisable()
        {
            if (_target != null) _target.offsetMin = new Vector2(_target.offsetMin.x, _restingBottom);
            _currentInset = 0f;
        }
    }
}
