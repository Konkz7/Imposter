using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core.Content;
using PartyGame.Core.Services;
using PartyGame.Core.Session;
using PartyGame.Core.Util;

namespace PartyGame.Games.SocialDeduction
{
    /// <summary>
    /// Builds the factual half of a clue. Flavour text is authored in a <see cref="CluePackData"/>,
    /// the fact itself is generated here from live round state and is always true - a clue can
    /// never contradict what actually happened.
    /// </summary>
    public class SocialClueFactory
    {
        private readonly IRandomProvider _random;

        public SocialClueFactory(IRandomProvider random)
        {
            _random = random;
        }

        /// <summary>Exactly one of the two named players is a suspect. Always true by construction.</summary>
        public string BuildInvestigatorPair(ClueTemplate template, IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> innocents)
        {
            if (suspects.Count == 0 || innocents.Count == 0) return null;

            var suspect = Shuffler.Pick(suspects, _random);
            var innocent = Shuffler.Pick(innocents, _random);

            var names = new List<string> { suspect.DisplayName, innocent.DisplayName };
            Shuffler.ShuffleInPlace(names, _random);

            var text = template != null && template.IsValid
                ? template.Text
                : "Exactly one of {a} and {b} is involved.";
            return text.Replace("{a}", names[0]).Replace("{b}", names[1]);
        }

        /// <summary>A true, non-identifying detail about one suspect.</summary>
        public string BuildWitnessDetail(ClueTemplate template, IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> everyone)
        {
            if (suspects.Count == 0) return null;
            var suspect = Shuffler.Pick(suspects, _random);
            var fact = BuildFact(suspect, everyone);
            if (string.IsNullOrEmpty(fact)) return null;

            var text = template != null && template.IsValid
                ? template.Text
                : "You caught a glimpse of them. {fact}";
            return text.Replace("{fact}", fact);
        }

        /// <summary>A true but weak statement the whole table hears.</summary>
        public string BuildAnonymousHint(ClueTemplate template, IReadOnlyList<PlayerState> suspects,
            IReadOnlyList<PlayerState> everyone)
        {
            if (suspects.Count == 0 || everyone.Count == 0) return null;

            var suspect = Shuffler.Pick(suspects, _random);
            var others = everyone.Where(p => p.Id != suspect.Id).ToList();
            var groupSize = Math.Min(3, everyone.Count);
            var group = new List<PlayerState> { suspect };
            group.AddRange(Shuffler.PickDistinct(others, groupSize - 1, _random));
            Shuffler.ShuffleInPlace(group, _random);

            var fact = "At least one suspect is among " + JoinNames(group.Select(p => p.DisplayName).ToList()) + ".";

            var text = template != null && template.IsValid ? template.Text : "{fact}";
            return text.Replace("{fact}", fact);
        }

        /// <summary>Picks one of several true observations about a single suspect.</summary>
        private string BuildFact(PlayerState suspect, IReadOnlyList<PlayerState> everyone)
        {
            var ordered = everyone.OrderBy(p => p.SeatIndex).ToList();
            var facts = new List<string>();

            // Seat neighbour: true because it is read straight off the seating order.
            if (ordered.Count >= 3)
            {
                var index = ordered.FindIndex(p => p.Id == suspect.Id);
                if (index >= 0)
                {
                    var left = ordered[(index - 1 + ordered.Count) % ordered.Count];
                    var right = ordered[(index + 1) % ordered.Count];
                    if (left.Id != suspect.Id && right.Id != suspect.Id)
                        facts.Add("They were sitting next to " +
                                  (_random.Range(0, 2) == 0 ? left.DisplayName : right.DisplayName) + ".");
                }
            }

            // Initial range: only used when more than one player falls inside it, so it narrows
            // the field without naming anybody.
            var initial = TextUtility.Initial(suspect.DisplayName)[0];
            var lower = (char)Math.Max('A', initial - 4);
            var upper = (char)Math.Min('Z', initial + 4);
            var inRange = everyone.Count(p =>
            {
                var c = TextUtility.Initial(p.DisplayName)[0];
                return c >= lower && c <= upper;
            });
            if (inRange >= 2 && inRange < everyone.Count)
                facts.Add("Their name starts with a letter between " + lower + " and " + upper + ".");

            // Name length: true, and vague enough to stay fun.
            var nameLength = (suspect.DisplayName ?? string.Empty).Trim().Length;
            var sameLength = everyone.Count(p => (p.DisplayName ?? string.Empty).Trim().Length == nameLength);
            if (sameLength < everyone.Count)
                facts.Add("Their name is " + nameLength + " characters long.");

            // Seat half: true by seat index.
            var half = ordered.Count / 2;
            var seatIndex = ordered.FindIndex(p => p.Id == suspect.Id);
            if (ordered.Count >= 4 && seatIndex >= 0)
                facts.Add(seatIndex < half
                    ? "They received the phone in the first half of the circle."
                    : "They received the phone in the second half of the circle.");

            if (facts.Count == 0) return "Somebody at this table is definitely involved.";
            return facts[_random.Range(0, facts.Count)];
        }

        private static string JoinNames(IReadOnlyList<string> names)
        {
            if (names.Count == 0) return "nobody";
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }
    }
}
