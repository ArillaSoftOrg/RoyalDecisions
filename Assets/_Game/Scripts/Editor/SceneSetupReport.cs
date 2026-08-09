using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    [Serializable]
    public sealed class SceneSetupReport
    {
        [SerializeField] private string operation;
        [SerializeField] private List<SceneSetupIssue> issues = new List<SceneSetupIssue>();

        public SceneSetupReport(string operationName)
        {
            operation = operationName ?? string.Empty;
        }

        public string Operation => operation;
        public IReadOnlyList<SceneSetupIssue> Issues => issues;
        public int ErrorCount => Count(SceneSetupIssueSeverity.Error);
        public int WarningCount => Count(SceneSetupIssueSeverity.Warning);
        public int InfoCount => Count(SceneSetupIssueSeverity.Info);
        public bool Succeeded => ErrorCount == 0;

        public void Add(
            SceneSetupIssueSeverity severity,
            string code,
            string category,
            string assetPath,
            string hierarchyPath,
            string message)
        {
            issues.Add(new SceneSetupIssue(
                severity, code, category, assetPath, hierarchyPath, message));
        }

        public void Merge(SceneSetupReport other)
        {
            if (other == null)
            {
                return;
            }

            for (int i = 0; i < other.issues.Count; i++)
            {
                issues.Add(other.issues[i]);
            }
        }

        private int Count(SceneSetupIssueSeverity severity)
        {
            int count = 0;

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
