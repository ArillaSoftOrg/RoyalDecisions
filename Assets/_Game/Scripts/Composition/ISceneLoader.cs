namespace RoyalDecisions.Composition
{
    /// <summary>
    /// Loads a scene by name.
    /// </summary>
    /// <remarks>
    /// Abstracted so a test can assert which scene a controller asked for without actually loading
    /// one. Names rather than build indices: an index silently means something different the moment
    /// the Build Profile list is reordered.
    /// </remarks>
    public interface ISceneLoader
    {
        void LoadScene(string sceneName);
    }
}
