using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugDefense
{
    /// <summary>
    /// Builds the whole game from code, so pressing Play in ANY scene (including the default
    /// SampleScene) starts Bug Defense. No editor setup required.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // AfterSceneLoad only fires once per Play session, so also hook scene reloads (used by Restart).
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildIfNeeded();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) BuildIfNeeded();
        }

        static void BuildIfNeeded()
        {
            if (GameManager.Instance != null) return; // already built
            var root = new GameObject("BugDefense");
            root.AddComponent<GameManager>(); // GameManager.Awake builds everything else in order
        }
    }
}
