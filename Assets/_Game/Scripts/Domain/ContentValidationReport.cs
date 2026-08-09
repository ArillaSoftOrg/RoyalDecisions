using System.Collections.Generic;

namespace RoyalDecisions.Domain
{
    /// <summary>
    /// Everything <see cref="ContentValidator"/> found in one content set.
    /// </summary>
    public sealed class ContentValidationReport
    {
        private readonly List<ContentValidationIssue> issues;
        private readonly List<ContentValidationIssue> errors;
        private readonly List<ContentValidationIssue> warnings;
        private readonly List<ContentValidationIssue> information;

        public ContentValidationReport(IReadOnlyList<ContentValidationIssue> foundIssues)
        {
            issues = new List<ContentValidationIssue>();
            errors = new List<ContentValidationIssue>();
            warnings = new List<ContentValidationIssue>();
            information = new List<ContentValidationIssue>();

            if (foundIssues == null)
            {
                return;
            }

            for (int i = 0; i < foundIssues.Count; i++)
            {
                ContentValidationIssue issue = foundIssues[i];
                issues.Add(issue);

                if (issue.IsError)
                {
                    errors.Add(issue);
                }
                else if (issue.IsWarning)
                {
                    warnings.Add(issue);
                }
                else
                {
                    information.Add(issue);
                }
            }
        }

        public IReadOnlyList<ContentValidationIssue> Issues => issues;

        public IReadOnlyList<ContentValidationIssue> Errors => errors;

        public IReadOnlyList<ContentValidationIssue> Warnings => warnings;

        public IReadOnlyList<ContentValidationIssue> Information => information;

        public int ErrorCount => errors.Count;

        public int WarningCount => warnings.Count;

        public int InformationCount => information.Count;

        public bool HasErrors => errors.Count > 0;

        public bool HasWarnings => warnings.Count > 0;

        public bool IsValid => errors.Count == 0;

        /// <summary>True when any issue carries the given code, whatever its severity.</summary>
        public bool Contains(ContentIssueCode code)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        public int CountOf(ContentIssueCode code)
        {
            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Code == code)
                {
                    count++;
                }
            }

            return count;
        }

        public override string ToString()
        {
            return string.Format(
                "Content validation: {0} error(s), {1} warning(s), {2} information item(s)",
                ErrorCount,
                WarningCount,
                InformationCount);
        }
    }
}
