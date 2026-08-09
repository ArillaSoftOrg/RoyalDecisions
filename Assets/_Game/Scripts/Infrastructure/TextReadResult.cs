namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// The raw text of a save file, or why it could not be produced.
    /// </summary>
    /// <remarks>
    /// Keeps "could the bytes be read" separate from "do the bytes mean anything", so the file layer
    /// never needs to know what a run is.
    /// </remarks>
    public readonly struct TextReadResult
    {
        private TextReadResult(LoadStatus status, string text, string message)
        {
            Status = status;
            Text = text ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static TextReadResult Success(string text)
        {
            return new TextReadResult(LoadStatus.Success, text, string.Empty);
        }

        public static TextReadResult Failure(LoadStatus status, string message)
        {
            return new TextReadResult(status, string.Empty, message);
        }

        public LoadStatus Status { get; }

        public string Text { get; }

        public string Message { get; }

        public bool HasText => Status == LoadStatus.Success;
    }
}
