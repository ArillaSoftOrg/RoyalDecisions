using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Loads scenes through Unity's scene manager.
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneLoader] No scene name was supplied; nothing was loaded.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
