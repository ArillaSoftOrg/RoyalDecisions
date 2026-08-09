using System;
using System.Collections.Generic;

namespace RoyalDecisions.Editor
{
    [Serializable]
    public sealed class ReleaseValidationReport
    {
        public string unityVersion = string.Empty;
        public List<ReleaseValidationIssue> issues = new List<ReleaseValidationIssue>();

        public int ErrorCount => Count(ReleaseValidationIssueSeverity.Error);
        public int WarningCount => Count(ReleaseValidationIssueSeverity.Warning);
        public bool Succeeded => ErrorCount == 0 && WarningCount == 0;

        public void Add(
            ReleaseValidationIssueSeverity severity,
            string code,
            string message)
        {
            issues.Add(new ReleaseValidationIssue
            {
                severity = severity,
                code = code ?? string.Empty,
                message = message ?? string.Empty
            });
        }

        private int Count(ReleaseValidationIssueSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].severity == severity)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
