namespace PartyGame.Core.Util
{
    /// <summary>
    /// Tiny result type used everywhere a configuration can be rejected.
    /// Keeps validation out of UI callbacks and makes the rules unit testable.
    /// </summary>
    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }

        private ValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message ?? string.Empty;
        }

        public static ValidationResult Ok() => new ValidationResult(true, string.Empty);
        public static ValidationResult Fail(string message) => new ValidationResult(false, message);

        public override string ToString() => IsValid ? "OK" : "Invalid: " + Message;
    }
}
