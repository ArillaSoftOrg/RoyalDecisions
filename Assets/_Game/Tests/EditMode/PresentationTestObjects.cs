using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalDecisions.Tests.EditMode
{
    /// <summary>
    /// Builds throwaway UI objects for presentation tests and releases them again.
    /// </summary>
    /// <remarks>
    /// Views are tested as real components rather than through an abstraction, so every fixture
    /// needs genuine GameObjects, Images and TMP texts. Everything created here is tracked so a
    /// failing assertion still cleans up.
    /// </remarks>
    public static class PresentationTestObjects
    {
        private static readonly List<Object> Created = new List<Object>();

        public static GameObject CreateObject(string name = "TestObject")
        {
            GameObject instance = new GameObject(name);
            instance.AddComponent<RectTransform>();
            Created.Add(instance);
            return instance;
        }

        public static T CreateComponent<T>(string name = null) where T : Component
        {
            GameObject host = CreateObject(name ?? typeof(T).Name);
            return host.AddComponent<T>();
        }

        public static Image CreateImage(string name = "Image")
        {
            return CreateComponent<Image>(name);
        }

        public static TextMeshProUGUI CreateText(string name = "Text")
        {
            return CreateComponent<TextMeshProUGUI>(name);
        }

        public static CanvasGroup CreateCanvasGroup(string name = "Group")
        {
            return CreateComponent<CanvasGroup>(name);
        }

        /// <summary>A 2x2 sprite. Content, never appearance — nothing asserts what it looks like.</summary>
        public static Sprite CreateSprite(string name = "Sprite")
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.name = name + "_Texture";
            Created.Add(texture);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            sprite.name = name;
            Created.Add(sprite);

            return sprite;
        }

        public static void DestroyAll()
        {
            for (int i = 0; i < Created.Count; i++)
            {
                if (Created[i] != null)
                {
                    Object.DestroyImmediate(Created[i]);
                }
            }

            Created.Clear();
        }
    }
}
