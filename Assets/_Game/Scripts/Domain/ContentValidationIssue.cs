namespace RoyalDecisions.Domain
{
    /// <summary>
    /// One problem found in a content set.
    /// </summary>
    public readonly struct ContentValidationIssue
    {
        private ContentValidationIssue(
            ContentIssueSeverity severity,
            ContentIssueCode code,
            string subjectId,
            string message)
        {
            Severity = severity;
            Code = code;
            SubjectId = subjectId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static ContentValidationIssue Error(
            ContentIssueCode code,
            string subjectId,
            string message)
        {
            return new ContentValidationIssue(ContentIssueSeverity.Error, code, subjectId, message);
        }

        public static ContentValidationIssue Warning(
            ContentIssueCode code,
            string subjectId,
            string message)
        {
            return new ContentValidationIssue(ContentIssueSeverity.Warning, code, subjectId, message);
        }

        public static ContentValidationIssue Information(
            ContentIssueCode code,
            string subjectId,
            string message)
        {
            return new ContentValidationIssue(
                ContentIssueSeverity.Information, code, subjectId, message);
        }

        public ContentIssueSeverity Severity { get; }

        public ContentIssueCode Code { get; }

        /// <summary>The card or ending ID the issue concerns, or an index when the ID is unusable.</summary>
        public string SubjectId { get; }

        public string Message { get; }

        public bool IsError => Severity == ContentIssueSeverity.Error;

        public bool IsWarning => Severity == ContentIssueSeverity.Warning;

        public bool IsInformation => Severity == ContentIssueSeverity.Information;

        public override string ToString()
        {
            return string.Format(
                "[{0}] {1} ({2}): {3}",
                Severity,
                Code,
                string.IsNullOrEmpty(SubjectId) ? "-" : SubjectId,
                Message);
        }
    }
}
