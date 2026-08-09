using UnityEngine;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Carries the player's menu choice across a scene load.
    /// </summary>
    /// <remarks>
    /// A ScriptableObject asset rather than a static field or a singleton: it survives the scene
    /// change, is visible in the Inspector, and can be handed to a test directly.
    ///
    /// Not persisted to disk — the value only matters between pressing a menu button and the game
    /// scene reading it.
    /// </remarks>
    [CreateAssetMenu(menuName = "Royal Decisions/Session Intent", fileName = "SessionIntent")]
    public class SessionIntent : ScriptableObject
    {
        [SerializeField] private SessionStartMode mode = SessionStartMode.NewGame;

        public SessionStartMode Mode => mode;

        public void RequestNewGame()
        {
            mode = SessionStartMode.NewGame;
        }

        public void RequestContinue()
        {
            mode = SessionStartMode.Continue;
        }
    }
}
