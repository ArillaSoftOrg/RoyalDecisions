using RoyalDecisions.Data;
using RoyalDecisions.Domain;

namespace RoyalDecisions.Editor
{
    /// <summary>Single editor-facing entry point for the core content validator.</summary>
    public static class ProjectContentAudit
    {
        public static ContentValidationReport Validate(ContentCatalogue catalogue)
        {
            if (catalogue == null)
            {
                return new ContentValidationReport(new[]
                {
                    ContentValidationIssue.Error(
                        ContentIssueCode.NullCardEntry,
                        "catalogue",
                        "A ContentCatalogue must be selected.")
                });
            }
            return new ContentValidator().Validate(
                catalogue.Cards, catalogue.Endings, catalogue.OpeningCardId);
        }
    }
}
