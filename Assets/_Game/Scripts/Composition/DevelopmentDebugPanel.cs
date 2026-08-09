#if UNITY_EDITOR || DEVELOPMENT_BUILD
using RoyalDecisions.Application;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>Explicitly runtime-created development panel; never serialized into a scene.</summary>
    public sealed class DevelopmentDebugPanel : MonoBehaviour
    {
        private GameSession session;
        private bool visible;
        private string flag = string.Empty;

        public void Configure(GameSession gameSession) => session = gameSession;

        private void OnGUI()
        {
            if (GUI.Button(new Rect(8f, 8f, 120f, 36f), visible ? "Close Debug" : "Open Debug"))
            {
                visible = !visible;
            }
            if (!visible || session == null)
            {
                return;
            }
            GUILayout.BeginArea(new Rect(8f, 52f, 420f, 620f), GUI.skin.box);
            GUILayout.Label("State: " + session.State);
            RunState run = session.CurrentRun;
            if (run != null)
            {
                GUILayout.Label("Turn: " + run.Turn + " Seed: " + run.Seed);
                GUILayout.Label("Stats: " + run.Stats);
                GUILayout.Label("Current: " + (session.CurrentCard != null
                    ? session.CurrentCard.Id : "-"));
                GUILayout.Label("Forced: " + run.ForcedNextCardId);
                GUILayout.Label("Flags: " + string.Join(", ", run.Flags));
                GUILayout.Label("History: " + string.Join(", ", run.ShownCardIds));
            }
            if (GUILayout.Button("New Game"))
                session.ExecuteDevelopmentCommand(DevelopmentSessionCommand.NewGame);
            if (GUILayout.Button("Choose Left"))
                session.ExecuteDevelopmentCommand(DevelopmentSessionCommand.ChooseLeft);
            if (GUILayout.Button("Choose Right"))
                session.ExecuteDevelopmentCommand(DevelopmentSessionCommand.ChooseRight);
            if (GUILayout.Button("Delete Save"))
                session.ExecuteDevelopmentCommand(DevelopmentSessionCommand.DeleteSave);
            flag = GUILayout.TextField(flag);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Flag")) session.DevelopmentSetFlag(flag, true);
            if (GUILayout.Button("Remove Flag")) session.DevelopmentSetFlag(flag, false);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
#endif
