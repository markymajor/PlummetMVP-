using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PlummetEditor
{
    /// <summary>
    /// Polishes the home-screen shaft: adds a dark fill behind the shaft so no
    /// sky teal shows beside the bricks at the bottom of the screen, and a tiled
    /// horizontal-brick course for texture. Re-run safely; it rebuilds both.
    /// </summary>
    public static class PlummetStartShaftPolish
    {
        private const string GamePath = "Assets/Plummet/Sprites/Game/";
        private const string BackdropName = "Shaft Side Backdrop";
        private const string BrickCourseName = "Shaft Brick Course";

        [MenuItem("Plummet/Legacy/Polish Start Shaft", priority = 140)]
        public static void PolishStartShaft()
        {
            Transform startPanel = FindDeep(null, "Start Panel");
            if (startPanel == null)
            {
                Debug.LogError("Polish Start Shaft: could not find a 'Start Panel'. Open the Plummet scene first.");
                return;
            }

            Transform shaftDetails = FindDeep(startPanel, "Start Shaft Lower Details");
            if (shaftDetails == null)
            {
                Debug.LogError("Polish Start Shaft: could not find 'Start Shaft Lower Details' under the Start Panel.");
                return;
            }

            RemoveExisting(startPanel, BackdropName);
            RemoveExisting(startPanel, BrickCourseName);

            // Dark fill behind the shaft (renders below the shaft bricks).
            RectTransform backdrop = CreateSolidBand(startPanel, BackdropName, new Color(0.02f, 0.13f, 0.17f, 1f), 520f);
            backdrop.SetSiblingIndex(shaftDetails.GetSiblingIndex());

            // Tiled horizontal-brick texture (renders between the fill and the shaft art).
            RectTransform bricks = CreateBrickCourse(startPanel, BrickCourseName, LoadSprite("Brick-white.png"), new Color(0.09f, 0.21f, 0.26f, 1f));
            bricks.SetSiblingIndex(shaftDetails.GetSiblingIndex());

            EditorSceneManager.MarkSceneDirty(startPanel.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Start shaft polished: dark side fill + horizontal brick course added.");
        }

        private static RectTransform CreateSolidBand(Transform parent, string name, Color color, float height)
        {
            GameObject band = new GameObject(name, typeof(RectTransform), typeof(Image));
            band.transform.SetParent(parent, false);

            RectTransform rect = band.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;

            Image image = band.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateBrickCourse(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject bricks = new GameObject(name, typeof(RectTransform), typeof(Image));
            bricks.transform.SetParent(parent, false);

            RectTransform rect = bricks.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.145f);
            rect.anchorMax = new Vector2(0.5f, 0.145f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1080f, 430f);
            rect.anchoredPosition = Vector2.zero;

            Image image = bricks.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Tiled;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void RemoveExisting(Transform root, string name)
        {
            Transform existing = FindDeep(root, name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(GamePath + fileName);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (go.name == name)
                    {
                        return go.transform;
                    }
                }

                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
