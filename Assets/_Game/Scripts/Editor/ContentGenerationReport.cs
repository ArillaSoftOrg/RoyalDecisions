using System.Collections.Generic;

namespace RoyalDecisions.Editor
{
    /// <summary>
    /// What one generator run did, in numbers a test can assert and a human can read.
    /// </summary>
    public sealed class ContentGenerationReport
    {
        private readonly List<string> messages = new List<string>();

        public int Created { get; private set; }

        public int Updated { get; private set; }

        public int Unchanged { get; private set; }

        public int Skipped { get; private set; }

        public int Warnings { get; private set; }

        public int Errors { get; private set; }

        /// <summary>True when the run stopped before writing anything.</summary>
        public bool Aborted { get; private set; }

        public IReadOnlyList<string> Messages => messages;

        public int TotalWritten => Created + Updated;

        public bool Succeeded => !Aborted && Errors == 0;

        public void RecordCreated(string assetPath)
        {
            Created++;
            messages.Add("Created: " + assetPath);
        }

        public void RecordUpdated(string assetPath)
        {
            Updated++;
            messages.Add("Updated: " + assetPath);
        }

        public void RecordUnchanged(string assetPath)
        {
            Unchanged++;
        }

        public void RecordSkipped(string assetPath, string reason)
        {
            Skipped++;
            messages.Add(string.Format("Skipped: {0} ({1})", assetPath, reason));
        }

        public void RecordWarning(string message)
        {
            Warnings++;
            messages.Add("Warning: " + message);
        }

        public void RecordError(string message)
        {
            Errors++;
            messages.Add("Error: " + message);
        }

        public void MarkAborted(string reason)
        {
            Aborted = true;
            messages.Add("Aborted: " + reason);
        }

        public override string ToString()
        {
            return string.Format(
                "Created {0}, Updated {1}, Unchanged {2}, Skipped {3}, Warnings {4}, Errors {5}{6}",
                Created,
                Updated,
                Unchanged,
                Skipped,
                Warnings,
                Errors,
                Aborted ? " (ABORTED)" : string.Empty);
        }
    }
}
