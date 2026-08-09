using System;

namespace RoyalDecisions.Editor
{
    [Serializable]
    public sealed class ReleaseValidationIssue
    {
        public ReleaseValidationIssueSeverity severity;
        public string code = string.Empty;
        public string message = string.Empty;
    }
}
