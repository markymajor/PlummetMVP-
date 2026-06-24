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
        private const float HalfWidth = 200f;
        private const float DoorHeight = 82f;

        // A solid ledge reads as ground: a lit top surface over a darker front face,
        // finished with a crisp bright lip along the very top edge (modern flat look).
        private static readonly Color GroundFace = new Color(0.30f, 0.33f, 0.36f, 1f);
        private static readonly Color GroundTop = new Color(0.55f, 0.57f, 0.59f, 1f);
        private static readonly Color GroundEdge = new Color(0.73f, 0.75f, 0.77f, 1f);

        [MenuItem("Plummet/Add Trapdoor Intro To Scene")]
        public static void AddTrapdoorIntro()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Plummet: run scene tools in Edit mode, not Play mode.");
                return;
            }

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
            // Faithful drop: Mark falls straight down (no spin) from the ledge through
            // the trapdoor and into the shaft, handing off to the pinned gameplay player
            // only once he reaches the run's position - so steering takes over after the
            // fall, not during it. fallDistance carries his centre from the standing
            // ledge position down to the gameplay player's pinned screen position.
            introSo.FindProperty("fallDistance").floatValue = 100f;
            introSo.FindProperty("fallDuration").floatValue = 0.5f;
            introSo.FindProperty("handoffFraction").floatValue = 1f;
            introSo.FindProperty("fallSpin").floatValue = 0f;
            // Falling-pose sprite = the world player's first falling frame, so the actor
            // matches the gameplay player (same pose and size) at hand-off.
            Sprite fallingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Plummet/Sprites/Game/mark-falling-flail-01.png");
            if (fallingSprite != null)
            {
                introSo.FindProperty("fallingSprite").objectReferenceValue = fallingSprite;
            }
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
            // Centre trapdoor sits at the lowered ground line (~0.34); its top aligns with
            // the side ground ledges so the surface reads as one continuous ledge.
            rect.anchorMin = new Vector2(0.5f, 0.32f);
            rect.anchorMax = new Vector2(0.5f, 0.32f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(HalfWidth * 2f, DoorHeight);
            rect.anchoredPosition = Vector2.zero;

            leftDoor = CreateDoorHalf(rect, "Left Door", new Vector2(0f, 0.5f));
            rightDoor = CreateDoorHalf(rect, "Right Door", new Vector2(1f, 0.5f));

            // Render the doors just above the character so he reads as falling through them.
            if (standingMark != null)
            {
                container.transform.SetSiblingIndex(standingMark.GetSiblingIndex() + 1);
            }
        }

        private static RectTransform CreateDoorHalf(Transform parent, string name, Vector2 edge)
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
            image.color = GroundFace;
            image.raycastTarget = false;

            // Layer a lit top surface and a bright top lip so the slab reads as a
            // solid ledge the character stands on rather than a flat grey bar.
            AddTopStrip(rect, "Top Surface", GroundTop, 0.36f);
            AddTopStrip(rect, "Top Edge", GroundEdge, 0.09f);

            return rect;
        }

        private static void AddTopStrip(RectTransform parent, string name, Color color, float heightFraction)
        {
            GameObject strip = new GameObject(name, typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);

            RectTransform rect = strip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, DoorHeight * heightFraction);
            rect.anchoredPosition = Vector2.zero;

            Image image = strip.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
