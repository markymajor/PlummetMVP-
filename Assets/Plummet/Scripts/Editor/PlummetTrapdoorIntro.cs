using Plummet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PlummetEditor
{
    /// <summary>
    /// Adds the grey trapdoor under the standing character on the Start Panel and
    /// wires up an <see cref="IntroTransition"/> so the home screen drops the
    /// player into the run. Re-run safely; it rebuilds the trapdoor each time.
    /// </summary>
    public static class PlummetTrapdoorIntro
    {
        private const float HalfWidth = 180f;
        private const float DoorHeight = 70f;

        [MenuItem("Plummet/Legacy/Add Trapdoor Intro To Scene", priority = 130)]
        public static void AddTrapdoorIntro()
        {
            UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("Trapdoor intro: no UIManager found. Open the Plummet scene first.");
                return;
            }

            Transform startPanel = FindDeep(uiManager.transform.root, "Start Panel");
            if (startPanel == null)
            {
                Debug.LogError("Trapdoor intro: could not find a 'Start Panel' object.");
                return;
            }

            Transform standingMark = FindDeep(startPanel, "Standing Mark");
            if (standingMark == null)
            {
                Debug.LogError("Trapdoor intro: could not find 'Standing Mark' under the Start Panel.");
                return;
            }

            Transform existing = FindDeep(startPanel, "Trapdoor");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            RectTransform markRect = standingMark as RectTransform;
            CreateTrapdoor(startPanel, markRect, out RectTransform leftDoor, out RectTransform rightDoor);

            IntroTransition intro = uiManager.GetComponent<IntroTransition>();
            if (intro == null)
            {
                intro = uiManager.gameObject.AddComponent<IntroTransition>();
            }

            SerializedObject introSo = new SerializedObject(intro);
            introSo.FindProperty("leftDoor").objectReferenceValue = leftDoor;
            introSo.FindProperty("rightDoor").objectReferenceValue = rightDoor;
            introSo.FindProperty("fallingActor").objectReferenceValue = markRect;
            introSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject uiSo = new SerializedObject(uiManager);
            uiSo.FindProperty("introTransition").objectReferenceValue = intro;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(uiManager);
            EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Trapdoor intro added and wired. Press Play, then tap to drop through the doors.");
        }

        private static void CreateTrapdoor(Transform parent, RectTransform standingMark, out RectTransform leftDoor, out RectTransform rightDoor)
        {
            GameObject container = new GameObject("Trapdoor", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.345f);
            rect.anchorMax = new Vector2(0.5f, 0.345f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(HalfWidth * 2f, DoorHeight);
            rect.anchoredPosition = Vector2.zero;

            Color doorColor = new Color(0.42f, 0.43f, 0.46f, 1f);
            leftDoor = CreateDoorHalf(rect, "Left Door", new Vector2(0f, 0.5f), doorColor);
            rightDoor = CreateDoorHalf(rect, "Right Door", new Vector2(1f, 0.5f), doorColor);

            // Render the doors just above the character so he reads as falling through them.
            if (standingMark != null)
            {
                container.transform.SetSiblingIndex(standingMark.GetSiblingIndex() + 1);
            }
        }

        private static RectTransform CreateDoorHalf(Transform parent, string name, Vector2 edge, Color color)
        {
            GameObject door = new GameObject(name, typeof(RectTransform), typeof(Image));
            door.transform.SetParent(parent, false);

            RectTransform rect = door.GetComponent<RectTransform>();
            rect.anchorMin = edge;
            rect.anchorMax = edge;
            rect.pivot = edge;
            rect.sizeDelta = new Vector2(HalfWidth, DoorHeight);
            rect.anchoredPosition = Vector2.zero;

            Image image = door.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Transform FindDeep(Transform root, string name)
        {
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
