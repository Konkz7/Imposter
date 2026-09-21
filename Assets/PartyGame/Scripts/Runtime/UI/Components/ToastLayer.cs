using System.Collections;
using PartyGame.Core.Audio;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Components
{
    /// <summary>
    /// Short, non blocking messages. Used for validation feedback so an invalid action never
    /// needs a modal dialog or, worse, silently does nothing.
    /// </summary>
    public class ToastLayer : MonoBehaviour
    {
        private RectTransform _card;
        private CanvasGroup _group;
        private TextMeshProUGUI _label;
        private Image _accentBar;
        private Coroutine _routine;

        public static ToastLayer Create(RectTransform parent)
        {
            var holder = UIFactory.CreateRect("Toasts", parent);
            holder.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var layer = holder.gameObject.AddComponent<ToastLayer>();
            layer.Build(holder);
            return layer;
        }

        private void Build(RectTransform holder)
        {
            var card = UIFactory.CreatePanel("Toast", holder, Theme.SurfaceRaised, Theme.RadiusMedium);
            _card = card.rectTransform;
            _card.anchorMin = new Vector2(0.5f, 0f);
            _card.anchorMax = new Vector2(0.5f, 0f);
            _card.pivot = new Vector2(0.5f, 0f);
            _card.sizeDelta = new Vector2(900f, 130f);
            _card.anchoredPosition = new Vector2(0f, 200f);

            _group = card.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _accentBar = UIFactory.CreatePanel("Accent", _card, Theme.Warning, Theme.RadiusSmall);
            _accentBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            _accentBar.rectTransform.anchorMax = new Vector2(0f, 1f);
            _accentBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            _accentBar.rectTransform.sizeDelta = new Vector2(10f, -24f);
            _accentBar.rectTransform.anchoredPosition = new Vector2(12f, 0f);

            _label = UIFactory.CreateText(_card, string.Empty, Theme.FontLabel, Theme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Normal, "Message");
            UIFactory.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(44f, 12f);
            _label.rectTransform.offsetMax = new Vector2(-24f, -12f);
        }

        public void Show(string message, bool isError = true, float seconds = 2.6f)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (_label != null) _label.text = message;
            if (_accentBar != null) _accentBar.color = isError ? Theme.Danger : Theme.Success;

            UiFeedback.Sound(isError ? SoundId.Error : SoundId.Confirm);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(seconds));
        }

        private IEnumerator ShowRoutine(float seconds)
        {
            _card.SetAsLastSibling();
            yield return UiTween.FadeCanvas(_group, 0f, 1f, Theme.FastTransition);
            yield return new WaitForSecondsRealtime(seconds);
            yield return UiTween.FadeCanvas(_group, 1f, 0f, Theme.FastTransition);
            _routine = null;
        }
    }
}
