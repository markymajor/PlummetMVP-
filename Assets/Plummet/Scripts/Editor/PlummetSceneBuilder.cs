using System.IO;
using Plummet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PlummetEditor
{
    public static class PlummetSceneBuilder
    {
        private const string GamePath = "Assets/Plummet/Sprites/Game/";
        private const string UiPath = "Assets/Plummet/Sprites/UI/";
        private const string PrefabPath = "Assets/Plummet/Prefabs/";
        private const string ScenePath = "Assets/Plummet/Scenes/PlummetMVP.unity";

        [MenuItem("Plummet/Build MVP Scene")]
        public static void BuildScene()
        {
            EnsureFolders();
            EnsureTags();
            ConfigureSprites();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateScrollingBackground();
            PlayerController player = CreatePlayer();
            CreateWalls();

            GameObject[] obstaclePrefabs = CreateObstaclePrefabs();
            ObjectPool pool = CreatePool(obstaclePrefabs);
            ObstacleSpawner spawner = CreateSpawner(pool);
            UIManager uiManager = CreateUi(camera);
            EnsureManagers(player, uiManager, spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"Plummet MVP scene built at {ScenePath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Plummet/Sprites/Game");
            Directory.CreateDirectory("Assets/Plummet/Sprites/UI");
            Directory.CreateDirectory("Assets/Plummet/Sprites/Icons");
            Directory.CreateDirectory("Assets/Plummet/Scripts");
            Directory.CreateDirectory("Assets/Plummet/Prefabs");
            Directory.CreateDirectory("Assets/Plummet/Scenes");
        }

        private static void ConfigureSprites()
        {
            string[] imagePaths = Directory.GetFiles("Assets/Plummet/Sprites", "*.png", SearchOption.AllDirectories);
            foreach (string path in imagePaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                if (Path.GetFileName(path) == "mark-falling-flail-sheet.png")
                {
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritesheet = BuildFallingSheetMeta();
                }
                else
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                }
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static SpriteMetaData[] BuildFallingSheetMeta()
        {
            SpriteMetaData[] sprites = new SpriteMetaData[8];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = new SpriteMetaData
                {
                    name = $"mark-falling-flail-sheet_{i + 1:00}",
                    rect = new Rect(i * 256f, 0f, 256f, 256f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

            return sprites;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void CreateScrollingBackground()
        {
            Sprite backgroundSprite = LoadGameSprite("Background.png");
            Sprite brickSprite = LoadGameSprite("Bricks-background.png");

            for (int i = 0; i < 2; i++)
            {
                GameObject background = CreateSpriteObject($"Shaft Background {i + 1}", backgroundSprite, new Vector3(0f, -30f * i, 2f), 0);
                Scroller scroller = background.AddComponent<Scroller>();
                Set(scroller, "loop", true);
                Set(scroller, "loopHeight", 30f);
                Set(scroller, "speedMultiplier", 1f);
            }

            for (int i = 0; i < 10; i++)
            {
                float y = -5f + i * 1.2f;
                GameObject leftBrick = CreateSpriteObject($"Left Brick Detail {i + 1}", brickSprite, new Vector3(-3.1f, y, 1f), 1);
                leftBrick.transform.localScale = Vector3.one * 0.75f;
                Scroller leftScroller = leftBrick.AddComponent<Scroller>();
                Set(leftScroller, "loop", true);
                Set(leftScroller, "loopHeight", 7f);

                GameObject rightBrick = CreateSpriteObject($"Right Brick Detail {i + 1}", brickSprite, new Vector3(3.1f, y + 0.5f, 1f), 1);
                rightBrick.transform.localScale = Vector3.one * 0.75f;
                Scroller rightScroller = rightBrick.AddComponent<Scroller>();
                Set(rightScroller, "loop", true);
                Set(rightScroller, "loopHeight", 7f);
            }
        }

        private static PlayerController CreatePlayer()
        {
            Sprite[] fallingFrames = LoadFallingFrames();
            Sprite playerSprite = fallingFrames.Length > 0 ? fallingFrames[0] : LoadGameSprite("mark-falling-custom.png");
            if (playerSprite == null)
            {
                playerSprite = LoadGameSprite("mark-falling.png") != null ? LoadGameSprite("mark-falling.png") : LoadGameSprite("mark.png");
            }
            GameObject player = CreateSpriteObject("Player - mark", playerSprite, new Vector3(0f, -0.3f, 0f), 10);
            player.tag = "Player";
            player.transform.localScale = Vector3.one * 1.15f;

            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(1.45f, 0.75f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;

            PlayerController controller = player.AddComponent<PlayerController>();
            Set(controller, "visualLeanDegrees", 6f);
            Set(controller, "fallingFrameRate", 12f);
            SetSpriteArray(controller, "fallingFrames", fallingFrames.Length > 0 ? fallingFrames : new[] { playerSprite });
            return controller;
        }

        private static void CreateWalls()
        {
            CreateWall("Left Wall", -2.65f);
            CreateWall("Right Wall", 2.65f);
        }

        private static void CreateWall(string name, float x)
        {
            GameObject wall = new GameObject(name);
            wall.tag = "Wall";
            wall.transform.position = new Vector3(x, 0f, 0f);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.35f, 12f);
        }

        private static GameObject[] CreateObstaclePrefabs()
        {
            string[] spriteNames =
            {
                "Pipe.png",
                "Sewer.png",
                "Window.png",
                "Obsticle1.png",
                "Brick-Color.png",
                "Brick-white.png",
                "Hatch.png"
            };

            GameObject[] prefabs = new GameObject[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++)
            {
                string spriteName = spriteNames[i];
                Sprite sprite = LoadGameSprite(spriteName);
                GameObject item = CreateSpriteObject(Path.GetFileNameWithoutExtension(spriteName), sprite, Vector3.zero, 5);
                item.tag = "Obstacle";

                BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;

                Scroller scroller = item.AddComponent<Scroller>();
                Set(scroller, "recycleY", 7.4f);
                Set(scroller, "speedMultiplier", Random.Range(0.9f, 1.1f));

                string prefabFile = PrefabPath + Path.GetFileNameWithoutExtension(spriteName) + ".prefab";
                prefabs[i] = PrefabUtility.SaveAsPrefabAsset(item, prefabFile);
                Object.DestroyImmediate(item);
            }

            return prefabs;
        }

        private static ObjectPool CreatePool(GameObject[] obstaclePrefabs)
        {
            GameObject poolObject = new GameObject("Obstacle Pool");
            ObjectPool pool = poolObject.AddComponent<ObjectPool>();
            SetArray(pool, "prefabs", obstaclePrefabs);
            Set(pool, "warmCountPerPrefab", 4);
            return pool;
        }

        private static ObstacleSpawner CreateSpawner(ObjectPool pool)
        {
            GameObject spawnerObject = new GameObject("Obstacle Spawner");
            ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
            Set(spawner, "obstaclePool", pool);
            return spawner;
        }

        private static void EnsureManagers(PlayerController player, UIManager uiManager, ObstacleSpawner spawner)
        {
            ScoreManager scoreManager = Object.FindFirstObjectByType<ScoreManager>();
            if (scoreManager == null)
            {
                scoreManager = CreateScoreManager(uiManager);
            }
            else
            {
                Set(scoreManager, "uiManager", uiManager);
            }

            GameManager manager = Object.FindFirstObjectByType<GameManager>();
            if (manager == null)
            {
                CreateGameManager(player, scoreManager, uiManager, spawner);
            }
            else
            {
                Set(manager, "player", player);
                Set(manager, "scoreManager", scoreManager);
                Set(manager, "uiManager", uiManager);
                Set(manager, "obstacleSpawner", spawner);
            }
        }

        private static ScoreManager CreateScoreManager(UIManager uiManager)
        {
            GameObject scoreObject = new GameObject("Score Manager");
            ScoreManager scoreManager = scoreObject.AddComponent<ScoreManager>();
            Set(scoreManager, "uiManager", uiManager);
            return scoreManager;
        }

        private static void CreateGameManager(PlayerController player, ScoreManager scoreManager, UIManager uiManager, ObstacleSpawner spawner)
        {
            GameObject managerObject = new GameObject("Game Manager");
            GameManager manager = managerObject.AddComponent<GameManager>();
            Set(manager, "player", player);
            Set(manager, "scoreManager", scoreManager);
            Set(manager, "uiManager", uiManager);
            Set(manager, "obstacleSpawner", spawner);
        }

        private static UIManager CreateUi(Camera camera)
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject startPanel = CreatePanel("Start Panel", canvasObject.transform);
            AddImage(startPanel.transform, "Menu Background", LoadGameSprite("main-menu-background_2014-12-19_adjusted.png"), Stretch(), false);
            AddImage(startPanel.transform, "Sky Cloud 1", LoadGameSprite("Cloud1.png"), Anchor(new Vector2(0.18f, 0.88f), new Vector2(420f, 86f)));
            AddImage(startPanel.transform, "Sky Cloud 2", LoadGameSprite("Cloud2.png"), Anchor(new Vector2(0.76f, 0.84f), new Vector2(330f, 74f)));
            AddImage(startPanel.transform, "Sky Cloud 3", LoadGameSprite("Cloud3.png"), Anchor(new Vector2(0.45f, 0.75f), new Vector2(320f, 44f)));
            AddImage(startPanel.transform, "Start Great Wall", LoadGameSprite("start-great-wall.png"), Anchor(new Vector2(0.51f, 0.43f), new Vector2(1120f, 300f)));
            AddImage(startPanel.transform, "Start Red Building", LoadGameSprite("start-red-building.png"), Anchor(new Vector2(0.1f, 0.34f), new Vector2(320f, 375f)));
            AddImage(startPanel.transform, "Start Skyscraper", LoadGameSprite("start-skyscraper.png"), Anchor(new Vector2(0.94f, 0.48f), new Vector2(170f, 720f)));
            AddImage(startPanel.transform, "Start Shaft Lower Details", LoadGameSprite("start-shaft-lower-details.png"), Anchor(new Vector2(0.5f, 0.145f), new Vector2(1080f, 430f)), false);
            AddImage(startPanel.transform, "Title", LoadGameSprite("Title.png"), Anchor(new Vector2(0.5f, 0.79f), new Vector2(850f, 215f)));
            AddText(startPanel.transform, "Tap Text", "TAP TO DROP", font, Anchor(new Vector2(0.5f, 0.665f), new Vector2(500f, 72f)), 42, TextAnchor.MiddleCenter).color = new Color(0.12f, 0.12f, 0.14f, 0.75f);
            AddImage(startPanel.transform, "Standing Mark", LoadGameSprite("mark.png"), Anchor(new Vector2(0.5f, 0.405f), new Vector2(130f, 300f)));
            AddText(startPanel.transform, "Start Text", "Start", font, Anchor(new Vector2(0.8f, 0.17f), new Vector2(220f, 78f)), 48, TextAnchor.MiddleCenter);
            AddText(startPanel.transform, "Best Value", $"Best {PlayerPrefs.GetInt("PlummetHighScore", 0):N0}", font, Anchor(new Vector2(0.5f, 0.09f), new Vector2(500f, 60f)), 30, TextAnchor.MiddleCenter).color = new Color(1f, 1f, 1f, 0.82f);
            Button playButton = AddTextButton(startPanel.transform, "Play Button", string.Empty, font, Stretch());

            GameObject instructionDistancePanel = CreatePanel("Instruction Distance Panel", canvasObject.transform);
            AddImage(instructionDistancePanel.transform, "Instruction Distance Art", LoadUiSprite("instruction-distance.png"), Stretch(), false);
            Button distanceNextButton = AddTextButton(instructionDistancePanel.transform, "Distance Next Button", string.Empty, font, Stretch());
            Button distanceBackButton = AddTextButton(instructionDistancePanel.transform, "Distance Back Button", string.Empty, font, Anchor(new Vector2(0.12f, 0.965f), new Vector2(240f, 105f)));

            GameObject instructionSpeedPanel = CreatePanel("Instruction Speed Panel", canvasObject.transform);
            AddImage(instructionSpeedPanel.transform, "Instruction Speed Art", LoadUiSprite("instruction-speed.png"), Stretch(), false);
            Button speedNextButton = AddTextButton(instructionSpeedPanel.transform, "Speed Next Button", string.Empty, font, Stretch());
            Button speedBackButton = AddTextButton(instructionSpeedPanel.transform, "Speed Back Button", string.Empty, font, Anchor(new Vector2(0.12f, 0.965f), new Vector2(240f, 105f)));

            GameObject hudPanel = CreatePanel("HUD Panel", canvasObject.transform);
            Text scoreText = AddText(hudPanel.transform, "Score Text", "0", font, Anchor(new Vector2(0.5f, 0.935f), new Vector2(560f, 105f)), 68, TextAnchor.MiddleCenter);
            scoreText.color = new Color(1f, 0.47f, 0.18f);
            ApplyScoreStyle(scoreText);
            Text highScoreText = AddText(hudPanel.transform, "Best Text", string.Empty, font, Anchor(new Vector2(0.5f, 0.91f), new Vector2(420f, 60f)), 28, TextAnchor.MiddleCenter);

            GameObject gameOverPanel = CreatePanel("Game Over Panel", canvasObject.transform);
            AddDimmer(gameOverPanel.transform);
            AddImage(gameOverPanel.transform, "Game Over", LoadUiSprite("gameover.png"), Anchor(new Vector2(0.5f, 0.67f), new Vector2(760f, 190f)));
            Text finalScoreText = AddText(gameOverPanel.transform, "Final Score Text", "Score 0\nBest 0", font, Anchor(new Vector2(0.5f, 0.52f), new Vector2(560f, 150f)), 40, TextAnchor.MiddleCenter);
            Button resetButton = AddImageButton(gameOverPanel.transform, "Reset Button", LoadUiSprite("button-reset.png"), Anchor(new Vector2(0.5f, 0.36f), new Vector2(150f, 155f)));
            Button homeButton = AddImageButton(gameOverPanel.transform, "Home Button", LoadUiSprite("button-home.png"), Anchor(new Vector2(0.34f, 0.22f), new Vector2(130f, 135f)));
            Button shareButton = AddImageButton(gameOverPanel.transform, "Share Button", LoadUiSprite("button-share.png"), Anchor(new Vector2(0.66f, 0.22f), new Vector2(130f, 135f)));

            UIManager uiManager = canvasObject.AddComponent<UIManager>();
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

            return uiManager;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private static void AddDimmer(Transform parent)
        {
            GameObject dimmer = new GameObject("Dimmer");
            dimmer.transform.SetParent(parent, false);
            Image image = dimmer.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            ApplyRect(dimmer.GetComponent<RectTransform>(), Stretch());
        }

        private static Image AddImage(Transform parent, string name, Sprite sprite, RectSpec rectSpec, bool preserveAspect = true)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null && preserveAspect;
            ApplyRect(imageObject.GetComponent<RectTransform>(), rectSpec);
            return image;
        }

        private static Button AddImageButton(Transform parent, string name, Sprite sprite, RectSpec rectSpec)
        {
            Image image = AddImage(parent, name, sprite, rectSpec);
            return image.gameObject.AddComponent<Button>();
        }

        private static Button AddTextButton(Transform parent, string name, string label, Font font, RectSpec rectSpec)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = string.IsNullOrEmpty(label) ? new Color(1f, 1f, 1f, 0.01f) : new Color(0f, 0f, 0f, 0.3f);
            Button button = buttonObject.AddComponent<Button>();
            ApplyRect(buttonObject.GetComponent<RectTransform>(), rectSpec);
            Text labelText = AddText(buttonObject.transform, "Label", label, font, Stretch(), 46, TextAnchor.MiddleCenter);
            labelText.raycastTarget = false;
            return button;
        }

        private static Text AddText(Transform parent, string name, string text, Font font, RectSpec rectSpec, int size, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = font;
            textComponent.fontSize = size;
            textComponent.alignment = anchor;
            textComponent.color = Color.white;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            ApplyRect(textObject.GetComponent<RectTransform>(), rectSpec);
            return textComponent;
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

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, int sortingOrder)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        private static Sprite LoadGameSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(GamePath + fileName);
        }

        private static Sprite LoadUiSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + fileName);
        }

        private static Sprite[] LoadFallingFrames()
        {
            Sprite[] candidates =
            {
                LoadGameSprite("mark-falling-flail-01.png"),
                LoadGameSprite("mark-falling-flail-02.png"),
                LoadGameSprite("mark-falling-flail-03.png"),
                LoadGameSprite("mark-falling-flail-04.png"),
                LoadGameSprite("mark-falling-flail-05.png"),
                LoadGameSprite("mark-falling-flail-06.png"),
                LoadGameSprite("mark-falling-flail-07.png"),
                LoadGameSprite("mark-falling-flail-08.png")
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

        private static RectSpec Stretch()
        {
            return new RectSpec(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        private static RectSpec Anchor(Vector2 anchor, Vector2 size)
        {
            return new RectSpec(anchor, anchor, size, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        private static void ApplyRect(RectTransform rect, RectSpec spec)
        {
            rect.anchorMin = spec.AnchorMin;
            rect.anchorMax = spec.AnchorMax;
            rect.sizeDelta = spec.SizeDelta;
            rect.anchoredPosition = spec.AnchoredPosition;
            rect.pivot = spec.Pivot;
        }

        private static void Set(Object target, string fieldName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string fieldName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string fieldName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string fieldName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string fieldName, GameObject[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSpriteArray(Object target, string fieldName, Sprite[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTags()
        {
            AddTag("Player");
            AddTag("Obstacle");
            AddTag("Wall");
        }

        private static void AddTag(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
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
