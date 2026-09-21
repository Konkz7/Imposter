using UnityEngine;

namespace PartyGame.UI.Framework
{
    /// <summary>
    /// Lifts its target clear of the on-screen keyboard so the field being typed into is never
    /// hidden behind it. Does nothing on platforms without a software keyboard.
    /// </summary>
    public class KeyboardAvoider : MonoBehaviour
    {
        private RectTransform _target;
        private Vector2 _restingPosition;
        private float _currentOffset;

        public static KeyboardAvoider Attach(RectTransform target)
        {
            if (target == null) return null;
            var avoider = target.gameObject.AddComponent<KeyboardAvoider>();
            avoider._target = target;
            avoider._restingPosition = target.anchoredPosition;
            return avoider;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            var desired = KeyboardOffset();
            if (Mathf.Approximately(desired, _currentOffset)) return;

            _currentOffset = Mathf.MoveTowards(_currentOffset, desired, Time.unscaledDeltaTime * 4000f);
            _target.anchoredPosition = _restingPosition + new Vector2(0f, _currentOffset);
        }

        /// <summary>Keyboard height converted from device pixels into canvas units.</summary>
        private float KeyboardOffset()
        {
            if (!TouchScreenKeyboard.visible) return 0f;

            var area = TouchScreenKeyboard.area;
            if (area.height <= 0f || Screen.height <= 0) return 0f;

            var canvas = _target.GetComponentInParent<Canvas>();
            var scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

            // Half the keyboard is enough to clear a centred field without pushing the
            // action button off the top of the screen.
            return area.height / scale * 0.5f;
        }

        private void OnDisable()
        {
            if (_target != null) _target.anchoredPosition = _restingPosition;
            _currentOffset = 0f;
        }
    }
}
