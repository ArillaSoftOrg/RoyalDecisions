using System;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    [Serializable]
    public sealed class SceneSetupIssue
    {
        [SerializeField] private SceneSetupIssueSeverity severity;
        [SerializeField] private string code;
        [SerializeField] private string category;
        [SerializeField] private string assetPath;
        [SerializeField] private string hierarchyPath;
        [SerializeField] private string message;

        public SceneSetupIssue(
            SceneSetupIssueSeverity issueSeverity,
            string issueCode,
            string issueCategory,
            string path,
            string objectPath,
            string issueMessage)
        {
            severity = issueSeverity;
            code = issueCode ?? string.Empty;
            category = issueCategory ?? string.Empty;
            assetPath = path ?? string.Empty;
            hierarchyPath = objectPath ?? string.Empty;
            message = issueMessage ?? string.Empty;
        }

        public SceneSetupIssueSeverity Severity => severity;
        public string Code => code;
        public string Category => category;
        public string AssetPath => assetPath;
        public string HierarchyPath => hierarchyPath;
        public string Message => message;
    }
}
