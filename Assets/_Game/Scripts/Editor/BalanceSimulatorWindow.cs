using System.IO;
using RoyalDecisions.Data;
using UnityEditor;
using UnityEngine;

namespace RoyalDecisions.Editor
{
    public sealed class BalanceSimulatorWindow : EditorWindow
    {
        private ContentCatalogue catalogue;
        private int runs = 10000;
        private int baseSeed = 1000;
        private int maximumTurns = 500;
        private BalanceSimulationReport lastReport;

        [MenuItem("Tools/Royal Decisions/Balance Simulator")]
        public static void Open() => GetWindow<BalanceSimulatorWindow>("RD Balance");

        private void OnGUI()
        {
            catalogue = (ContentCatalogue)EditorGUILayout.ObjectField(
                "Catalogue", catalogue, typeof(ContentCatalogue), false);
            runs = EditorGUILayout.IntField("Runs per strategy", runs);
            baseSeed = EditorGUILayout.IntField("Base seed", baseSeed);
            maximumTurns = EditorGUILayout.IntField("Maximum turns", maximumTurns);
            using (new EditorGUI.DisabledScope(catalogue == null || runs < 1 || maximumTurns < 1))
            {
                if (GUILayout.Button("Run deterministic simulation"))
                {
                    RunSimulation();
                }
            }
            if (lastReport != null)
            {
                EditorGUILayout.LabelField("Hash", lastReport.reproducibilityHash);
                EditorGUILayout.LabelField("Strategies", lastReport.strategies.Count.ToString());
                if (GUILayout.Button("Export JSON to Logs/Balance"))
                {
                    Directory.CreateDirectory("Logs/Balance");
                    File.WriteAllText(
                        "Logs/Balance/RoyalDecisionsBalance.json",
                        JsonUtility.ToJson(lastReport, true));
                }
            }
        }

        private void RunSimulation()
        {
            try
            {
                lastReport = new BalanceSimulationRunner().Run(
                    catalogue,
                    new BalanceSimulationOptions
                    {
                        RunCount = runs,
                        BaseSeed = baseSeed,
                        MaximumTurns = maximumTurns
                    },
                    () => EditorUtility.DisplayCancelableProgressBar(
                        "Royal Decisions Balance", "Simulating", 0.5f));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
