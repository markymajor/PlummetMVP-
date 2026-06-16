using System.IO;
using Plummet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PlummetEditor
{
    public static class PlummetSceneRepair
    {
        private const string GamePath = "Assets/Plummet/Sprites/Game/";
        private const string UiPath = "Assets/Plummet/Sprites/UI/";

        [MenuItem("Plummet/Repair Open Scene")]
        public static void RepairOpenScene()
        {
            AssetDatabase.Refresh();
            ConfigureSprites();

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            ObstacleSpawner spawner = Object.FindFirstObjectByType<ObstacleSpawner>();
            ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
            ConfigurePlayer(player);
            ConfigureWallDetails();
            ConfigureObstaclePool(pool);
            DisableStraightWallColliders();
            PathManager pathManager = EnsurePathManager();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
            }

            UIManager uiManager = canvas.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = canvas.gameObject.AddComponent<UIManager>();
            }

            GameObject startPanel = CreatePanel(canvas.transform, "Start Panel");
            AddImage(startPanel.transform, "Menu Background", LoadGameSprite("main-menu-background_2014-12-19_adjusted.png"), Stretch(), false);
            AddImage(startPanel.transform, "Sky Cloud 1", LoadGameSprite("Cloud1.png"), Anchor(0.18f, 0.88f, 420f, 86f));
            AddImage(startPanel.transform, "Sky Cloud 2", LoadGameSprite("Cloud2.png"), Anchor(0.76f, 0.84f, 330f, 74f));
            AddImage(startPanel.transform, "Sky Cloud 3", LoadGameSprite("Cloud3.png"), Anchor(0.45f, 0.75f, 320f, 44f));
            AddImage(startPanel.transform, "Start Great Wall", LoadGameSprite("start-great-wall.png"), Anchor(0.51f, 0.43f, 1120f, 300f));
            AddImage(startPanel.transform, "Start Red Building", LoadGameSprite("start-red-building.png"), Anchor(0.1f, 0.34f, 320f, 375f));
            AddImage(startPanel.transform, "Start Skyscraper", LoadGameSprite("start-skyscraper.png"), Anchor(0.94f, 0.48f, 170f, 720f));
            AddImage(startPanel.transform, "Start Shaft Lower Details", LoadGameSprite("start-shaft-lower-details.png"), Anchor(0.5f, 0.145f, 1080f, 430f), false);
            AddImage(startPanel.transform, "Title", LoadGameSprite("Title.png"), Anchor(0.5f, 0.79f, 850f, 215f));
            AddText(startPanel.transform, "Tap Text", "TAP TO DROP", Anchor(0.5f, 0.665f, 500f, 72f), 42, TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.14f, 0.75f));
            AddImage(startPanel.transform, "Standing Mark", LoadGameSprite("mark.png"), Anchor(0.5f, 0.405f, 130f, 300f));
            AddText(startPanel.transform, "Start Text", "Start", Anchor(0.8f, 0.17f, 220f, 78f), 48, TextAnchor.MiddleCenter);
            AddText(startPanel.transform, "Best Value", $"Best {PlayerPrefs.GetInt("PlummetHighScore", 0):N0}", Anchor(0.5f, 0.09f, 500f, 60f), 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.82f));
            Button playButton = AddTextButton(startPanel.transform, "Play Button", string.Empty, Stretch());

            GameObject instructionDistancePanel = CreatePanel(canvas.transform, "Instruction Distance Panel");
            AddImage(instructionDistancePanel.transform, "Instruction Distance Art", LoadUiSprite("instruction-distance.png"), Stretch(), false);
            Button distanceNextButton = AddTextButton(instructionDistancePanel.transform, "Distance Next Button", string.Empty, Stretch());
            Button distanceBackButton = AddTextButton(instructionDistancePanel.transform, "Distance Back Button", string.Empty, Anchor(0.12f, 0.965f, 240f, 105f));

            GameObject instructionSpeedPanel = CreatePanel(canvas.transform, "Instruction Speed Panel");
            AddImage(instructionSpeedPanel.transform, "Instruction Speed Art", LoadUiSprite("instruction-speed.png"), Stretch(), false);
            Button speedNextButton = AddTextButton(instructionSpeedPanel.transform, "Speed Next Button", string.Empty, Stretch());
            Button speedBackButton = AddTextButton(instructionSpeedPanel.transform, "Speed Back Button", string.Empty, Anchor(0.12f, 0.965f, 240f, 105f));

            GameObject hudPanel = CreatePanel(canvas.transform, "HUD Panel");
            Text scoreText = AddText(hudPanel.transform, "Score Text", "0", Anchor(0.5f, 0.935f, 560f, 105f), 68, TextAnchor.MiddleCenter, new Color(1f, 0.47f, 0.18f));
            ApplyScoreStyle(scoreText);
            Text highScoreText = AddText(hudPanel.transform, "High Score Text", string.Empty, Anchor(0.5f, 0.89f, 540f, 60f), 30, TextAnchor.MiddleCenter);

            GameObject gameOverPanel = CreatePanel(canvas.transform, "Game Over Panel");
            AddDimmer(gameOverPanel.transform);
            AddImage(gameOverPanel.transform, "Game Over Image", LoadUiSprite("gameover.png"), Anchor(0.5f, 0.78f, 690f, 250f));
            AddImage(gameOverPanel.transform, "Death Mark", LoadUiSprite("death.png"), Anchor(0.5f, 0.57f, 560f, 315f));
            Text finalScoreText = AddText(gameOverPanel.transform, "Final Score Text", "Score 0\nBest 0", Anchor(0.5f, 0.4f, 600f, 140f), 44, TextAnchor.MiddleCenter);
            Button resetButton = AddImageButton(gameOverPanel.transform, "Reset Button", LoadUiSprite("button-reset.png"), Anchor(0.35f, 0.25f, 170f, 170f));
            Button homeButton = AddImageButton(gameOverPanel.transform, "Home Button", LoadUiSprite("button-home.png"), Anchor(0.5f, 0.25f, 170f, 170f));
            Button shareButton = AddImageButton(gameOverPanel.transform, "Share Button", LoadUiSprite("button-share.png"), Anchor(0.65f, 0.25f, 170f, 170f));

            Set(uiManager, "startPanel", startPanel);
            Set(uiManager, "instructionDistancePanel", instructionDistancePanel);
            Set(uiManager, "instructionSpeedPanel", instructionSpeedPanel);
            Set(uiManager, "hudPanel", hudPanel);
            Set(uiManager, "gameOverPanel", gameOverPanel);
            Set(uiManager, "scoreText", scoreText);
            Set(uiManager, "highScoreText", highScoreText);
            Set(uiManager, "finalScoreText", finalScoreText);
            Set(uiManager, "playButton", playButton);
            Set(uiManager, "distanceNextButton", distanceNextButton);
            Set(uiManager, "speedNextButton", speedNextButton);
            Set(uiManager, "distanceBackButton", distanceBackButton);
            Set(uiManager, "speedBackButton", speedBackButton);
            Set(uiManager, "resetButton", resetButton);
            Set(uiManager, "homeButton", homeButton);
            Set(uiManager, "shareButton", shareButton);

            instructionDistancePanel.SetActive(false);
            instructionSpeedPanel.SetActive(false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            ScoreManager scoreManager = Object.FindFirstObjectByType<ScoreManager>();
            if (scoreManager == null)
            {
                scoreManager = new GameObject("Score Manager").AddComponent<ScoreManager>();
            }

            Set(scoreManager, "uiManager", uiManager);

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                gameManager = new GameObject("Game Manager").AddComponent<GameManager>();
            }

            Set(gameManager, "player", player);
            Set(gameManager, "scoreManager", scoreManager);
            Set(gameManager, "uiManager", uiManager);
            Set(gameManager, "obstacleSpawner", spawner);
            Set(gameManager, "pathManager", pathManager);

            if (spawner != null && pool != null)
            {
                Set(spawner, "obstaclePool", pool);
                Set(spawner, "pathManager", pathManager);
                SetBool(spawner, "spawnOnWalls", true);
                SetFloat(spawner, "wallX", 2.55f);
                SetFloat(spawner, "wallInset", 0.32f);
                SetFloat(spawner, "spawnY", -6.35f);
                SetFloat(spawner, "initialInterval", 1.1f);
                SetFloat(spawner, "minimumInterval", 0.55f);
            }

            EnsureEventSystem();

            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(scoreManager);
            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(pathManager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Plummet scene repaired. Press Play to test the start screen.");
        }

        private static void ConfigureSprites()
        {
            if (!Directory.Exists("Assets/Plummet/Sprites"))
            {
                return;
            }

            string[] imagePaths = Directory.GetFiles("Assets/Plummet/Sprites", "*.png", SearchOption.AllDirectories);
            foreach (string path in imagePaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePlayer(PlayerController player)
        {
            if (player == null)
            {
                return;
            }

            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
            Sprite[] fallingFrames = LoadFallingFrames();
            Sprite fallingSprite = fallingFrames.Length > 0 ? fallingFrames[0] : LoadGameSprite("mark-falling-custom.png");
            if (fallingSprite == null)
            {
                fallingSprite = LoadGameSprite("120.png") != null ? LoadGameSprite("120.png") : LoadGameSprite("mark-falling.png");
            }
            if (renderer != null && fallingSprite != null)
            {
                renderer.sprite = fallingSprite;
                renderer.sortingOrder = 10;
            }

            player.transform.localScale = Vector3.one * 1.15f;
            player.transform.rotation = Quaternion.identity;

            CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
            if (capsule != null)
            {
                capsule.isTrigger = true;
                capsule.direction = CapsuleDirection2D.Horizontal;
                capsule.size = new Vector2(1.45f, 0.75f);
            }

            SetFloat(player, "horizontalLimit", 3.25f);
            SetFloat(player, "visualLeanDegrees", 6f);
            SetFloat(player, "fallingFrameRate", 10f);
            SetSpriteArray(player, "fallingFrames", fallingFrames.Length > 0 ? fallingFrames : new[] { fallingSprite });
        }

        private static Sprite[] LoadFallingFrames()
        {
            Sprite[] candidates =
            {
                LoadGameSprite("mark-falling-flail-01.png"),
                LoadGameSprite("mark-falling-flail-02.png"),
                LoadGameSprite("mark-falling-flail-03.png"),
                LoadGameSprite("mark-falling-flail-04.png")
            };

            int validCount = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null)
                {
                    validCount++;
                }
            }

            Sprite[] frames = new Sprite[validCount];
            int frameIndex = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null)
                {
                    frames[frameIndex] = candidates[i];
                    frameIndex++;
                }
            }

            return frames;
        }

        private static PathManager EnsurePathManager()
        {
            PathManager pathManager = Object.FindFirstObjectByType<PathManager>();
            if (pathManager == null)
            {
                pathManager = new GameObject("Path Manager").AddComponent<PathManager>();
            }

            Set(pathManager, "wallSprite", LoadGameSprite("Bricks-background.png"));
            SetInt(pathManager, "segmentCount", 20);
            SetFloat(pathManager, "segmentHeight", 1.05f);
            SetFloat(pathManager, "startWidth", 4.05f);
            SetFloat(pathManager, "minimumWidth", 2.45f);
            SetFloat(pathManager, "maximumWidth", 4.75f);
            SetFloat(pathManager, "maximumCenterX", 1.05f);
            SetFloat(pathManager, "centerStep", 0.45f);
            SetFloat(pathManager, "widthStep", 0.32f);
            SetFloat(pathManager, "wallThickness", 0.82f);
            SetColor(pathManager, "wallColor", new Color(0.015f, 0.19f, 0.23f, 1f));
            pathManager.ResetPath();
            return pathManager;
        }

        private static void ConfigureObstaclePool(ObjectPool pool)
        {
            if (pool == null)
            {
                return;
            }

            string[] prefabNames =
            {
                "Pipe",
                "Sewer",
                "Window",
                "Obsticle1",
                "Brick-Color",
                "Brick-white",
                "Hatch"
            };

            GameObject[] prefabs = new GameObject[prefabNames.Length];
            for (int i = 0; i < prefabNames.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Plummet/Prefabs/{prefabNames[i]}.prefab");
            }

            SetArray(pool, "prefabs", prefabs);

            for (int i = pool.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(pool.transform.GetChild(i).gameObject);
            }
        }

        private static void DisableStraightWallColliders()
        {
            BoxCollider2D[] colliders = Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            foreach (BoxCollider2D collider in colliders)
            {
                if (collider.name == "Left Wall" || collider.name == "Right Wall")
                {
                    collider.enabled = false;
                }
            }
        }

        private static void ConfigureWallDetails()
        {
            Scroller[] scrollers = Object.FindObjectsByType<Scroller>(FindObjectsSortMode.None);
            foreach (Scroller scroller in scrollers)
            {
                if (scroller.name.Contains("Brick Detail"))
                {
                    SetBool(scroller, "loop", true);
                    SetFloat(scroller, "loopHeight", 7f);
                    SetFloat(scroller, "speedMultiplier", 1f);
                    TintRenderer(scroller.GetComponent<SpriteRenderer>());
                }
            }

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform item in transforms)
            {
                if (!item.name.Contains("Brick Detail"))
                {
                    continue;
                }

                Scroller scroller = item.GetComponent<Scroller>();
                if (scroller == null)
                {
                    scroller = item.gameObject.AddComponent<Scroller>();
                }

                SetBool(scroller, "loop", true);
                SetFloat(scroller, "loopHeight", 7f);
                SetFloat(scroller, "speedMultiplier", 1f);
                TintRenderer(item.GetComponent<SpriteRenderer>());
            }
        }

        private static void TintRenderer(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.color = new Color(0.02f, 0.23f, 0.27f, 0.92f);
            EditorUtility.SetDirty(renderer);
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            ApplyRect(panel.GetComponent<RectTransform>(), Stretch());
            return panel;
        }

        private static void AddDimmer(Transform parent)
        {
            GameObject dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dimmer.transform.SetParent(parent, false);
            ApplyRect(dimmer.GetComponent<RectTransform>(), Stretch());
            dimmer.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        }

        private static Image AddImage(Transform parent, string name, Sprite sprite, RectSpec rect, bool preserveAspect = true)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            ApplyRect(item.GetComponent<RectTransform>(), rect);

            Image image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null && preserveAspect;
            image.color = sprite == null ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            return image;
        }

        private static Button AddImageButton(Transform parent, string name, Sprite sprite, RectSpec rect)
        {
            Image image = AddImage(parent, name, sprite, rect);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button AddTextButton(Transform parent, string name, string label, RectSpec rect)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            ApplyRect(buttonObject.GetComponent<RectTransform>(), rect);

            Image image = buttonObject.GetComponent<Image>();
            image.color = string.IsNullOrEmpty(label) ? new Color(1f, 1f, 1f, 0.01f) : new Color(0.02f, 0.42f, 0.5f, 0.92f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = AddText(buttonObject.transform, "Label", label, Stretch(), 48, TextAnchor.MiddleCenter);
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            return button;
        }

        private static Text AddText(Transform parent, string name, string value, RectSpec rect, int size, TextAnchor alignment)
        {
            return AddText(parent, name, value, rect, size, alignment, Color.white);
        }

        private static Text AddText(Transform parent, string name, string value, RectSpec rect, int size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            ApplyRect(textObject.GetComponent<RectTransform>(), rect);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void ApplyScoreStyle(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.68f, 0.95f);
            outline.effectDistance = new Vector2(3.5f, -3.5f);

            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.64f, 0.29f, 0.1f, 0.75f);
            shadow.effectDistance = new Vector2(5f, -5f);
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static Sprite LoadGameSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(GamePath + fileName);
        }

        private static Sprite LoadUiSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + fileName);
        }

        private static RectSpec Stretch()
        {
            return new RectSpec(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        private static RectSpec Anchor(float x, float y, float width, float height)
        {
            return new RectSpec(new Vector2(x, y), new Vector2(x, y), new Vector2(width, height), Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        private static void ApplyRect(RectTransform rectTransform, RectSpec spec)
        {
            rectTransform.anchorMin = spec.AnchorMin;
            rectTransform.anchorMax = spec.AnchorMax;
            rectTransform.sizeDelta = spec.SizeDelta;
            rectTransform.anchoredPosition = spec.AnchoredPosition;
            rectTransform.pivot = spec.Pivot;

            if (spec.AnchorMin != spec.AnchorMax)
            {
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }

        private static void Set(Object target, string fieldName, Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning($"Could not wire {fieldName} on {target.name}.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetFloat(Object target, string fieldName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void SetBool(Object target, string fieldName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void SetInt(Object target, string fieldName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void SetColor(Object target, string fieldName, Color value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.colorValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void SetArray(Object target, string fieldName, GameObject[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetSpriteArray(Object target, string fieldName, Sprite[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.DestroyImmediate(oldModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private readonly struct RectSpec
        {
            public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot)
            {
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                SizeDelta = sizeDelta;
                AnchoredPosition = anchoredPosition;
                Pivot = pivot;
            }

            public Vector2 AnchorMin { get; }
            public Vector2 AnchorMax { get; }
            public Vector2 SizeDelta { get; }
            public Vector2 AnchoredPosition { get; }
            public Vector2 Pivot { get; }
        }
    }
}
