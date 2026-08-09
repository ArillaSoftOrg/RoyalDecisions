using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RoyalDecisions.Editor
{
    public sealed class ReleaseBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == UnityEditor.BuildTarget.Android)
            {
                ReleaseValidationAutomation.ValidateBatch();
            }
        }
    }
}
