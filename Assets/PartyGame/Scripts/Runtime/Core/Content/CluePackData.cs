using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core.Content
{
    /// <summary>
    /// How a clue is produced. The flavour text is authored, the factual part is generated
    /// from live round state so a clue can never contradict the truth.
    /// </summary>
    public enum ClueKind
    {
        /// <summary>Scene setting shown to everyone. Carries no information.</summary>
        Scene = 0,
        /// <summary>Private: exactly one of two named players is an imposter.</summary>
        InvestigatorPair = 1,
        /// <summary>Private: a true detail about an imposter (seat neighbour, initial, name length).</summary>
        WitnessDetail = 2,
        /// <summary>Public: a true but weak statement everyone hears.</summary>
        AnonymousHint = 3
    }

    [Serializable]
    public class ClueTemplate
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private ClueKind kind = ClueKind.Scene;
        [SerializeField, TextArea(1, 3)] private string text = string.Empty;

        public string Id => string.IsNullOrEmpty(id) ? text : id;
        public ClueKind Kind => kind;

        /// <summary>Supports {a}, {b} and {fact} tokens depending on the kind.</summary>
        public string Text => text;

        public bool IsValid => !string.IsNullOrWhiteSpace(text);

        public ClueTemplate() { }

        public ClueTemplate(string id, ClueKind kind, string text)
        {
            this.id = id;
            this.kind = kind;
            this.text = text;
        }
    }

    [CreateAssetMenu(menuName = "Party Game/Content/Clue Pack", fileName = "CluePack")]
    public class CluePackData : ContentPack
    {
        [SerializeField] private List<ClueTemplate> clues = new List<ClueTemplate>();

        public IReadOnlyList<ClueTemplate> Clues => clues;
        public override int EntryCount => clues.Count;

        public void SetClues(IEnumerable<ClueTemplate> newClues)
        {
            clues = new List<ClueTemplate>(newClues);
        }
    }
}
