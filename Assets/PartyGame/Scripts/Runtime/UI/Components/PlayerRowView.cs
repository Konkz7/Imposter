using System;
using PartyGame.Core.Session;
using PartyGame.UI.Design;
using PartyGame.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Components
{
    /// <summary>
    /// One editable player in the setup list: seat number, name field and a remove button.
    /// </summary>
    public class PlayerRowView : MonoBehaviour
    {
        private TMP_InputField _field;
        private TextMeshProUGUI _seatLabel;
        private UiButton _remove;
        private int _playerId;

        public int PlayerId => _playerId;

        public static PlayerRowView Create(Transform parent, PlayerState player, Action<int, string> onRenamed,
            Action<int> onRemoved, bool canRemove)
        {
            var card = UIFactory.CreateCard("Player-" + player.Id, parent, Theme.Surface, Theme.RadiusMedium);
            var row = card.gameObject.AddComponent<PlayerRowView>();
            row.Construct(card, player, onRenamed, onRemoved, canRemove);
            return row;
        }

        private void Construct(RectTransform card, PlayerState player, Action<int, string> onRenamed,
            Action<int> onRemoved, bool canRemove)
        {
            _playerId = player.Id;

            var row = UIFactory.HorizontalGroup(card, "Row", Theme.SpaceS,
                new RectOffset((int)Theme.SpaceS, (int)Theme.SpaceS, (int)Theme.SpaceXs, (int)Theme.SpaceXs),
                TextAnchor.MiddleLeft);
            UIFactory.Stretch((RectTransform)row.transform);

            var badge = UIFactory.CreateRect("Seat", row.transform);
            UIFactory.SetSize(badge.gameObject, 84f, 84f, 84f, 84f);
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = UIGraphics.Circle(72);
            badgeImage.color = Theme.WithAlpha(Theme.Accent(player.SeatIndex), 0.28f);
            badgeImage.raycastTarget = false;

            _seatLabel = UIFactory.CreateFittedText(badge, (player.SeatIndex + 1).ToString(), Theme.FontBody,
                Theme.FontCaption, Theme.Accent(player.SeatIndex), TextAlignmentOptions.Center, FontStyles.Bold, "SeatNumber");
            UIFactory.Stretch(_seatLabel.rectTransform, 6f);

            _field = UIFactory.CreateInputField(row.transform, "Player name", 14);
            _field.text = player.DisplayName;
            UIFactory.SetSize(_field.gameObject, 104f, 104f, flexibleWidth: 1f);
            _field.onEndEdit.AddListener(value => onRenamed?.Invoke(_playerId, value));

            _remove = UiButton.Create(row.transform, "x", ButtonStyle.Ghost, () => onRemoved?.Invoke(_playerId),
                84f, null, "Remove");
            UIFactory.SetSize(_remove.gameObject, 84f, 84f, 84f, 84f);
            _remove.Interactable = canRemove;

            UIFactory.SetSize(gameObject, 132f, 132f);
        }

        public void Refresh(PlayerState player, bool canRemove)
        {
            if (player == null) return;
            if (_seatLabel != null) _seatLabel.text = (player.SeatIndex + 1).ToString();
            if (_field != null && _field.text != player.DisplayName) _field.SetTextWithoutNotify(player.DisplayName);
            if (_remove != null) _remove.Interactable = canRemove;
        }

        public void FocusField()
        {
            if (_field != null) _field.Select();
        }
    }
}
