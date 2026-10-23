using System;
using System.Collections.Generic;

namespace PartyGame.Core.Modes
{
    /// <summary>
    /// What the UI hands back once a step is finished. Plain data, so a future network layer
    /// can deliver the same payload from a remote device.
    /// </summary>
    public class StepResult
    {
        public string StepId { get; set; } = string.Empty;
        public int? ActorPlayerId { get; set; }

        /// <summary>Text typed by the player, for <see cref="TextInputStep"/>.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Selected option id, for <see cref="ChoiceStep"/>.</summary>
        public string OptionId { get; set; } = string.Empty;

        /// <summary>What was typed into each box, by field id, for <see cref="FormStep"/>.</summary>
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>True when a timed step ran out rather than being skipped.</summary>
        public bool TimedOut { get; set; }

        /// <summary>A form value, trimmed, or empty when the box was left blank.</summary>
        public string ValueOf(string fieldId)
        {
            return fieldId != null && Values.TryGetValue(fieldId, out var value) ? (value ?? string.Empty).Trim() : string.Empty;
        }

        public static StepResult Acknowledged(GameStep step)
        {
            return new StepResult
            {
                StepId = step != null ? step.Id : string.Empty,
                ActorPlayerId = step?.ActorPlayerId
            };
        }

        public static StepResult ForText(GameStep step, string text)
        {
            var result = Acknowledged(step);
            result.Text = text ?? string.Empty;
            return result;
        }

        public static StepResult ForChoice(GameStep step, string optionId)
        {
            var result = Acknowledged(step);
            result.OptionId = optionId ?? string.Empty;
            return result;
        }

        public static StepResult ForForm(GameStep step, IEnumerable<KeyValuePair<string, string>> values)
        {
            var result = Acknowledged(step);
            if (values != null)
                foreach (var pair in values)
                    if (!string.IsNullOrEmpty(pair.Key)) result.Values[pair.Key] = pair.Value ?? string.Empty;
            return result;
        }
    }
}
