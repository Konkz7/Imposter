using System.Collections.Generic;

namespace PartyGame.Core.Modes
{
    /// <summary>
    /// Every screen a mode can ask the UI to present. Adding a kind means adding one view,
    /// not a new screen per game.
    /// </summary>
    public enum StepKind
    {
        Message = 0,
        PrivateInfo = 1,
        TextInput = 2,
        Choice = 3,
        Discussion = 4,
        Reveal = 5,
        Scoreboard = 6
    }

    /// <summary>Colour intent, resolved to real colours by the theme. Modes never mention hex codes.</summary>
    public enum StepAccent
    {
        Neutral = 0,
        Primary = 1,
        Danger = 2,
        Success = 3,
        Warning = 4
    }

    /// <summary>
    /// A pure data description of one screen in a round.
    /// Modes emit these, the UI renders them: no mode ever touches a GameObject, which is
    /// what keeps the rules unit testable and portable to an online layer later.
    /// </summary>
    public abstract class GameStep
    {
        public abstract StepKind Kind { get; }

        /// <summary>Stable id inside the round, used by the flow controller and by tests.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Small label above the title, e.g. "Secret role" or "Voting".</summary>
        public string PhaseLabel { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        /// <summary>Player this step belongs to, or null when it addresses the whole table.</summary>
        public int? ActorPlayerId { get; set; }

        /// <summary>
        /// When true the flow controller inserts a hand-the-phone gate before the step and
        /// blanks the screen afterwards. This is what makes pass-and-play safe.
        /// </summary>
        public bool IsPrivate { get; set; }

        public string ContinueLabel { get; set; } = "Continue";

        public StepAccent Accent { get; set; } = StepAccent.Neutral;

        /// <summary>Progress hint for the UI, e.g. player 3 of 7. Zero means unknown.</summary>
        public int SequenceIndex { get; set; }
        public int SequenceCount { get; set; }
    }

    /// <summary>Plain information or a spoken-turn prompt. Advances on a single tap.</summary>
    public sealed class MessageStep : GameStep
    {
        public override StepKind Kind => StepKind.Message;

        /// <summary>Optional large focal string, e.g. the debate statement or trivia question.</summary>
        public string Feature { get; set; } = string.Empty;

        public List<string> Bullets { get; } = new List<string>();
    }

    public readonly struct InfoLine
    {
        public string Label { get; }
        public string Value { get; }
        public bool Emphasise { get; }

        public InfoLine(string label, string value, bool emphasise = false)
        {
            Label = label;
            Value = value;
            Emphasise = emphasise;
        }
    }

    /// <summary>
    /// Secret information for exactly one player. Always rendered behind the reveal gate.
    /// </summary>
    public sealed class PrivateInfoStep : GameStep
    {
        public PrivateInfoStep()
        {
            IsPrivate = true;
        }

        public override StepKind Kind => StepKind.PrivateInfo;

        /// <summary>Headline shown once revealed, e.g. "YOU ARE THE IMPOSTER".</summary>
        public string Headline { get; set; } = string.Empty;

        public List<InfoLine> Lines { get; } = new List<InfoLine>();

        /// <summary>Extra warning line shown under the card, e.g. "Do not let anyone else see this".</summary>
        public string Footnote { get; set; } = "Keep this to yourself.";
    }

    /// <summary>Free text entry from one player, e.g. a bluff answer.</summary>
    public sealed class TextInputStep : GameStep
    {
        public TextInputStep()
        {
            IsPrivate = true;
            ContinueLabel = "Submit";
        }

        public override StepKind Kind => StepKind.TextInput;

        public string Prompt { get; set; } = string.Empty;
        public string Placeholder { get; set; } = "Type here";
        public int MaxLength { get; set; } = 40;
        public bool AllowEmpty { get; set; }

        /// <summary>Answers that would be confusing if duplicated, checked case insensitively.</summary>
        public List<string> RejectedValues { get; } = new List<string>();
        public string RejectionMessage { get; set; } = "Someone already wrote that. Try another.";
    }

    public sealed class ChoiceOption
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string SubLabel { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public StepAccent Accent { get; set; } = StepAccent.Neutral;

        public ChoiceOption() { }

        public ChoiceOption(string id, string label, string subLabel = "")
        {
            Id = id;
            Label = label;
            SubLabel = subLabel;
        }
    }

    /// <summary>One player picks one option. Used for votes, guesses and stance selection.</summary>
    public sealed class ChoiceStep : GameStep
    {
        public ChoiceStep()
        {
            IsPrivate = true;
            ContinueLabel = "Confirm";
        }

        public override StepKind Kind => StepKind.Choice;

        public string Prompt { get; set; } = string.Empty;
        public List<ChoiceOption> Options { get; } = new List<ChoiceOption>();

        /// <summary>Shuffle the options before display so position never leaks information.</summary>
        public bool ShuffleOptions { get; set; }
    }

    /// <summary>Group discussion with a shared countdown. Never private.</summary>
    public sealed class DiscussionStep : GameStep
    {
        public DiscussionStep()
        {
            ContinueLabel = "Done talking";
        }

        public override StepKind Kind => StepKind.Discussion;

        public float Seconds { get; set; } = 90f;
        public bool CanSkip { get; set; } = true;
        public List<string> Bullets { get; } = new List<string>();
    }

    public sealed class RevealEntry
    {
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public StepAccent Accent { get; set; } = StepAccent.Neutral;
        public bool Highlight { get; set; }
    }

    /// <summary>End of round reveal: who was who, what the answers were, what happened.</summary>
    public sealed class RevealStep : GameStep
    {
        public RevealStep()
        {
            ContinueLabel = "Scores";
        }

        public override StepKind Kind => StepKind.Reveal;

        public string Headline { get; set; } = string.Empty;
        public List<RevealEntry> Entries { get; } = new List<RevealEntry>();
    }

    public sealed class ScoreRow
    {
        public int PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Delta { get; set; }
        public int Rank { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>Shared leaderboard, used both between rounds and at game over.</summary>
    public sealed class ScoreboardStep : GameStep
    {
        public ScoreboardStep()
        {
            ContinueLabel = "Next round";
        }

        public override StepKind Kind => StepKind.Scoreboard;

        public List<ScoreRow> Rows { get; } = new List<ScoreRow>();
        public bool IsFinal { get; set; }
    }
}
