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

        /// <summary>True when a timed step ran out rather than being skipped.</summary>
        public bool TimedOut { get; set; }

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
    }
}
