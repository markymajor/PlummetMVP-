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

        // Per the reference: dark NAVY blue procedural walls, light blueish-grey shaft.
        private static readonly Color DarkWallColor = new Color(0.06f, 0.12f, 0.28f, 1f);
        private static readonly Color ShaftColor = new Color(0.62f, 0.69f, 0.74f, 1f);

        [MenuItem("Plummet/Repair Open Scene")]
        public static void RepairOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Plummet: run scene tools in Edit mode, not Play mode.");
                return;
            }

            AssetDatabase.Refresh();
            ConfigureSprites();

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            ObstacleSpawner spawner = Object.FindFirstObjectByType<ObstacleSpawner>();
            ObjectPool pool = Object.FindFirstObjectByType<ObjectPool>();
            ConfigurePlayer(player);
            ConfigureWallDetails();
            ConfigureObstaclePool(pool);
            DisableStraightWallColliders();
            EnsureGameplayCamera();
            PathManager pathManager = EnsurePathManager();
            ConfigureShaftBackground();
            CreateShaftWindows();

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

            Transform uiRoot = CreatePortraitUiRoot(canvas.transform).transform;

            UIManager uiManager = canvas.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = canvas.gameObject.AddComponent<UIManager>();
            }

            // Original 2014 composition, modernized: the surface (sky, skyline, ledge)
            // fills the top of the screen as the land Mark stands on, and the real
            // scrolling shaft (rendered by the camera) shows through the transparent
            // lower half, descending below the ground. groundLine is the surface height
            // where Mark stands and the trapdoor opens.
            const float groundLine = 0.60f;
            GameObject startPanel = CreatePanel(uiRoot, "Start Panel");

            // Opaque surface backdrop + skyline above the ground (hides the shaft here).
            AddImage(startPanel.transform, "Surface Sky", LoadGameSprite("main-menu-background_2014-12-19_adjusted.png"), Region(0f, groundLine - 0.03f, 1f, 1f), false);
            AddImage(startPanel.transform, "Sky Cloud 1", LoadGameSprite("Cloud1.png"), Anchor(0.2f, 0.9f, 360f, 74f));
            AddImage(startPanel.transform, "Sky Cloud 2", LoadGameSprite("Cloud2.png"), Anchor(0.79f, 0.86f, 300f, 66f));
            AddImage(startPanel.transform, "Start Skyscraper", LoadGameSprite("start-skyscraper.png"), Anchor(0.9f, groundLine + 0.17f, 170f, 720f));
            AddImage(startPanel.transform, "Start Red Building", LoadGameSprite("start-red-building.png"), Anchor(0.12f, groundLine + 0.085f, 320f, 375f));
            AddImage(startPanel.transform, "Start Great Wall", LoadGameSprite("start-great-wall.png"), Anchor(0.5f, groundLine + 0.06f, 1120f, 300f));

            // Ground ledge either side of the centre trapdoor (the trapdoor itself is
            // built by Plummet/Add Trapdoor Intro). Below the ledge the shaft descends.
            AddGroundLedge(startPanel.transform, "Ground Ledge Left", 0f, 0.32f, groundLine);
            AddGroundLedge(startPanel.transform, "Ground Ledge Right", 0.68f, 1f, groundLine);

            // Darken the shaft mouth just under the ground so it reads as descending
            // into depth (a faked gradient: stacked dark strips fading downward).
            AddShaftMouthShadow(startPanel.transform, groundLine - 0.06f);

            AddImage(startPanel.transform, "Title", LoadGameSprite("Title.png"), Anchor(0.5f, 0.9f, 720f, 182f));
            AddText(startPanel.transform, "Tap Text", "TAP TO DROP", Anchor(0.5f, 0.8f, 500f, 70f), 40, TextAnchor.MiddleCenter, new Color(0.12f, 0.13f, 0.16f, 0.85f));
            // Standing Mark is sized to the world player's on-screen footprint (~514px
            // square) so there is no size pop when the drop hands off to the run. The
            // 514-tall box places mark.png's feet on the ground line (centre +0.134).
            Image standingMark = AddImage(startPanel.transform, "Standing Mark", LoadGameSprite("mark.png"), Anchor(0.5f, groundLine + 0.0755f, 290f, 290f));
            Button playButton = AddTextButton(startPanel.transform, "Play Button", string.Empty, Stretch());

            GameObject instructionDistancePanel = CreatePanel(uiRoot, "Instruction Distance Panel");
            AddImage(instructionDistancePanel.transform, "Instruction Distance Art", LoadUiSprite("instruction-distance.png"), Stretch(), false);
            Button distanceNextButton = AddTextButton(instructionDistancePanel.transform, "Distance Next Button", string.Empty, Stretch());
            Button distanceBackButton = AddTextButton(instructionDistancePanel.transform, "Distance Back Button", string.Empty, Anchor(0.12f, 0.965f, 240f, 105f));

            GameObject instructionSpeedPanel = CreatePanel(uiRoot, "Instruction Speed Panel");
            AddImage(instructionSpeedPanel.transform, "Instruction Speed Art", LoadUiSprite("instruction-speed.png"), Stretch(), false);
            Button speedNextButton = AddTextButton(instructionSpeedPanel.transform, "Speed Next Button", string.Empty, Stretch());
            Button speedBackButton = AddTextButton(instructionSpeedPanel.transform, "Speed Back Button", string.Empty, Anchor(0.12f, 0.965f, 240f, 105f));

            GameObject hudPanel = CreatePanel(uiRoot, "HUD Panel");
            Text scoreText = AddText(hudPanel.transform, "Score Text", "0", Anchor(0.5f, 0.935f, 560f, 105f), 68, TextAnchor.MiddleCenter, new Color(1f, 0.47f, 0.18f));
            ApplyScoreStyle(scoreText);
            Text highScoreText = AddText(hudPanel.transform, "High Score Text", string.Empty, Anchor(0.5f, 0.89f, 540f, 60f), 30, TextAnchor.MiddleCenter);

            GameObject gameOverPanel = CreatePanel(uiRoot, "Game Over Panel");
            AddDimmer(gameOverPanel.transform);
            AddImage(gameOverPanel.transform, "Game Over Image", LoadUiSprite("gameover.png"), Anchor(0.5f, 0.78f, 690f, 250f));
            AddImage(gameOverPanel.transform, "Death Mark", LoadUiSprite("death.png"), Anchor(0.5f, 0.57f, 560f, 315f));
            Text finalScoreText = AddText(gameOverPanel.transform, "Final Score Text", "Score 0\nBest 0", Anchor(0.5f, 0.4f, 600f, 140f), 44, TextAnchor.MiddleCenter);
            Button resetButton = AddImageButton(gameOverPanel.transform, "Reset Button", LoadUiSprite("button-reset.png"), Anchor(0.35f, 0.25f, 170f, 170f));
            Button homeButton = AddImageButton(gameOverPanel.transform, "Home Button", LoadUiSprite("button-home.png"), Anchor(0.5f, 0.25f, 170f, 170f));
            Button shareButton = AddImageButton(gameOverPanel.transform, "Share Button", LoadUiSprite("button-share.png"), Anchor(0.65f, 0.25f, 170f, 170f));

            Set(uiManager, "startPanel", startPanel);
            Set(uiManager, "startCharacterImage", standingMark);
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

            // --- Choose Player (skins) ---
            Button chooseSkinButton = AddSpriteButton(startPanel.transform, "Choose Player Button", LoadUiSprite("button-players.png"), LoadUiSprite("button-players-pressed.png"), Anchor(0.5f, 0.14f, 525f, 145f));

            GameObject chooseSkinPanel = CreatePanel(uiRoot, "Choose Skin Panel");
            AddImage(chooseSkinPanel.transform, "Choose Background", LoadGameSprite("main-menu-background_2014-12-19_adjusted.png"), Stretch(), false);
            AddText(chooseSkinPanel.transform, "Choose Title", "CHOOSE YOUR PLAYER", Anchor(0.5f, 0.85f, 960f, 110f), 54, TextAnchor.MiddleCenter, Color.white);
            RectTransform cardsRect = CreateChildRect(chooseSkinPanel.transform, "Cards", Anchor(0.5f, 0.52f, 1000f, 420f));
            Button chooseSkinBackButton = AddSpriteButton(chooseSkinPanel.transform, "Choose Back Button", LoadUiSprite("button-back.png"), LoadUiSprite("button-back-pressed.png"), Anchor(0.3f, 0.13f, 365f, 125f));
            Button chooseSkinSelectButton = AddSpriteButton(chooseSkinPanel.transform, "Choose Select Button", LoadUiSprite("button-select.png"), LoadUiSprite("button-select-pressed.png"), Anchor(0.7f, 0.13f, 475f, 135f));
            SkinPickerUI picker = chooseSkinPanel.AddComponent<SkinPickerUI>();
            Set(picker, "content", cardsRect);
            Set(picker, "player", player);

            SkinLibrary skinLibrary = EnsureSkinLibrary();

            Set(uiManager, "chooseSkinPanel", chooseSkinPanel);
            Set(uiManager, "chooseSkinButton", chooseSkinButton);
            Set(uiManager, "chooseSkinBackButton", chooseSkinBackButton);
            Set(uiManager, "chooseSkinSelectButton", chooseSkinSelectButton);
            Set(uiManager, "skinPicker", picker);
            EditorUtility.SetDirty(skinLibrary);

            chooseSkinPanel.SetActive(false);
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
            SetFloat(gameManager, "baseScrollSpeed", 5.5f);
            SetFloat(gameManager, "maxScrollSpeed", 12f);
            SetFloat(gameManager, "speedIncreasePerSecond", 0.06f);
            SetFloat(gameManager, "attractScrollSpeed", 2.5f);

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

                // The shaft walls render as Tiled SpriteRenderers, which require a
                // Full Rect mesh; otherwise Unity logs "Sprite Tiling ... not
                // generated with Full Rect" once per tile.
                if (Path.GetFileName(path) == "Bricks-background.png")
                {
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                }

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
            SetFloat(player, "fallingFrameRate", 12f);
            SetSpriteArray(player, "fallingFrames", fallingFrames.Length > 0 ? fallingFrames : new[] { fallingSprite });
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
            SetFloat(pathManager, "minStep", 0.5f);
            SetFloat(pathManager, "maxStep", 1.0f);
            SetFloat(pathManager, "playHalfWidth", 2.65f);
            SetFloat(pathManager, "widthStep", 0.32f);
            SetFloat(pathManager, "wallThickness", 4.5f);
            SetColor(pathManager, "wallColor", DarkWallColor);
            pathManager.ResetPath();
            return pathManager;
        }

        private static void ConfigureObstaclePool(ObjectPool pool)
        {
            if (pool == null)
            {
                return;
            }

            // Windows and bricks are decoration, not hazards - only real obstacles here.
            string[] prefabNames =
            {
                "Pipe",
                "Sewer",
                "Obsticle1",
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

        private static void EnsureGameplayCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                return;
            }

            // SolidColor clear so backgroundColor shows as the light shaft. Under URP this
            // only takes effect when the camera has a UniversalAdditionalCameraData set to
            // a Base render type; without it URP leaves the centre black even with
            // clearFlags = SolidColor.
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ShaftColor;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            EnsureUrpBaseCamera(camera);

            PortraitViewportFitter fitter = camera.GetComponent<PortraitViewportFitter>();
            if (fitter != null)
            {
                Object.DestroyImmediate(fitter);
            }

            EditorUtility.SetDirty(camera);
        }

        // Ensure the camera carries URP camera data as a Base camera so the SolidColor clear
        // is honoured. Done via reflection so this editor script needs no hard URP reference.
        private static void EnsureUrpBaseCamera(Camera camera)
        {
            System.Type dataType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (dataType == null)
            {
                return;
            }

            Component data = camera.GetComponent(dataType);
            if (data == null)
            {
                data = camera.gameObject.AddComponent(dataType);
            }

            System.Reflection.PropertyInfo renderType = dataType.GetProperty("renderType");
            if (renderType != null && renderType.CanWrite)
            {
                renderType.SetValue(data, System.Enum.ToObject(renderType.PropertyType, 0)); // CameraRenderType.Base
            }

            EditorUtility.SetDirty(data);
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

        // The shaft reads as a flat light blueish-grey from the camera clear colour; the
        // old teal shaft-texture sprites are disabled (a multiply tint can't brighten that
        // dark texture to a light shaft). Faint background-window decals are the only
        // shaft decoration.
        private static void ConfigureShaftBackground()
        {
            foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.name.StartsWith("Shaft Background"))
                {
                    renderer.color = ShaftColor;
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        // Decorative window decals that scroll + loop with the shaft like the brick
        // details. Pure decoration: no colliders, untagged, so they never kill the player.
        private static void CreateShaftWindows()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && (t.name.StartsWith("Wall Window") || t.name.StartsWith("Shaft Bg Window")))
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }

            Sprite litWindow = LoadGameSprite("Window.png");
            Sprite bgWindow = LoadGameSprite("Window-Background.png");

            // Lit windows: warm orange glow on the dark walls near the screen edges.
            // x = +/-2.72 is always inside the wall (the corridor gap never reaches
            // beyond +/-playHalfWidth = 2.65), so they sit in the dark band.
            Color litTint = new Color(1f, 0.82f, 0.5f, 1f);
            float[] leftY = { -5.0f, -0.3f, 4.4f };
            float[] rightY = { -3.2f, 1.6f, 6.2f };
            for (int i = 0; i < leftY.Length; i++)
            {
                CreateWindowDecal("Wall Window L" + i, litWindow, new Vector3(-2.78f, leftY[i], 0f), 0.7f, litTint, 3);
            }

            for (int i = 0; i < rightY.Length; i++)
            {
                CreateWindowDecal("Wall Window R" + i, litWindow, new Vector3(2.78f, rightY[i], 0f), 0.7f, litTint, 3);
            }

            // Faint background windows: low-contrast grey/teal in the lighter shaft
            // centre, behind the player (sorting order 1, below the player's 10).
            Color bgTint = new Color(0.72f, 0.80f, 0.84f, 0.2f);
            float[] bgX = { -0.5f, 0.5f, -0.1f };
            float[] bgY = { -4.2f, 0.8f, 5.2f };
            for (int i = 0; i < bgY.Length; i++)
            {
                CreateWindowDecal("Shaft Bg Window " + i, bgWindow, new Vector3(bgX[i], bgY[i], 0f), 0.85f, bgTint, 1);
            }
        }

        private static void CreateWindowDecal(string name, Sprite sprite, Vector3 position, float scale, Color tint, int sortingOrder)
        {
            if (sprite == null)
            {
                return;
            }

            GameObject go = new GameObject(name, typeof(SpriteRenderer), typeof(Scroller));
            go.tag = "Untagged";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;

            Scroller scroller = go.GetComponent<Scroller>();
            SetBool(scroller, "loop", true);
            SetFloat(scroller, "loopHeight", 7f);
            SetFloat(scroller, "speedMultiplier", 1f);
        }

        private static GameObject CreatePortraitUiRoot(Transform parent)
        {
            RectTransform leftBar = CreateLetterboxBar(parent, "Left Letterbox Bar");
            RectTransform rightBar = CreateLetterboxBar(parent, "Right Letterbox Bar");
            RectTransform topBar = CreateLetterboxBar(parent, "Top Letterbox Bar");
            RectTransform bottomBar = CreateLetterboxBar(parent, "Bottom Letterbox Bar");

            GameObject root = new GameObject("Portrait Phone Frame", typeof(RectTransform), typeof(PortraitScreenFrame));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1080f, 1920f);

            PortraitScreenFrame screenFrame = root.GetComponent<PortraitScreenFrame>();
            screenFrame.Configure(rect, leftBar, rightBar, topBar, bottomBar);
            EditorUtility.SetDirty(screenFrame);

            return root;
        }

        private static RectTransform CreateLetterboxBar(Transform parent, string name)
        {
            GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;

            Image image = bar.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
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

        // Image button with no text label (the art carries the words) that depresses on
        // tap via a SpriteSwap to the matching pressed sprite.
        private static Button AddSpriteButton(Transform parent, string name, Sprite normal, Sprite pressed, RectSpec rect)
        {
            Image image = AddImage(parent, name, normal, rect, true);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.pressedSprite = pressed;
            state.selectedSprite = normal;
            button.spriteState = state;
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

        // A rect that fills a normalized region of the parent (anchorMin..anchorMax).
        private static RectSpec Region(float minX, float minY, float maxX, float maxY)
        {
            return new RectSpec(new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        // Builds a solid ground ledge band whose top sits at groundLine, spanning the
        // normalized x range. Same lit-top-over-dark-face look as the centre trapdoor so
        // the surface reads as one continuous ledge with the trapdoor in the middle.
        private static void AddGroundLedge(Transform parent, string name, float xMin, float xMax, float groundLine)
        {
            const float bandHeight = 0.06f;
            GameObject ledge = new GameObject(name, typeof(RectTransform), typeof(Image));
            ledge.transform.SetParent(parent, false);
            ApplyRect(ledge.GetComponent<RectTransform>(), new RectSpec(new Vector2(xMin, groundLine - bandHeight), new Vector2(xMax, groundLine), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f)));
            Image face = ledge.GetComponent<Image>();
            face.color = new Color(0.30f, 0.33f, 0.36f, 1f);
            face.raycastTarget = false;

            AddLedgeStrip(ledge.transform, "Top Surface", new Color(0.55f, 0.57f, 0.59f, 1f), 26f);
            AddLedgeStrip(ledge.transform, "Top Edge", new Color(0.73f, 0.75f, 0.77f, 1f), 7f);
        }

        // Stacked dark strips below the ground that fade downward, faking a gradient so
        // the shaft mouth reads as descending into depth. topY is the ledge's bottom.
        private static void AddShaftMouthShadow(Transform parent, float topY)
        {
            Color shade = new Color(0f, 0.05f, 0.08f, 1f);
            float[] alphas = { 0.55f, 0.34f, 0.16f };
            const float stripHeight = 0.05f;
            for (int i = 0; i < alphas.Length; i++)
            {
                float top = topY - i * stripHeight;
                float bottom = top - stripHeight;
                GameObject strip = new GameObject($"Shaft Mouth Shadow {i + 1}", typeof(RectTransform), typeof(Image));
                strip.transform.SetParent(parent, false);
                ApplyRect(strip.GetComponent<RectTransform>(), new RectSpec(new Vector2(0f, bottom), new Vector2(1f, top), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f)));
                Image image = strip.GetComponent<Image>();
                Color c = shade;
                c.a = alphas[i];
                image.color = c;
                image.raycastTarget = false;
            }
        }

        private static void AddLedgeStrip(Transform parent, string name, Color color, float height)
        {
            GameObject strip = new GameObject(name, typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);
            RectTransform rect = strip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
            Image image = strip.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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

        private static RectTransform CreateChildRect(Transform parent, string name, RectSpec spec)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            ApplyRect(rect, spec);
            return rect;
        }

        private static SkinLibrary EnsureSkinLibrary()
        {
            SkinLibrary library = Object.FindFirstObjectByType<SkinLibrary>();
            if (library == null)
            {
                library = new GameObject("Skin Library").AddComponent<SkinLibrary>();
            }

            System.Collections.Generic.List<Skin> skins = new System.Collections.Generic.List<Skin>();

            Sprite[] markFrames = LoadFallingFrames();
            Sprite markStanding = LoadGameSprite("mark.png");
            skins.Add(new Skin("mark", "Mark", markStanding, markFrames.Length > 0 ? markFrames : new[] { markStanding }));

            AddKidSkin(skins, "harrison", "Harrison");
            AddKidSkin(skins, "evie", "Evie");

            library.SetSkins(skins);
            return library;
        }

        private static void AddKidSkin(System.Collections.Generic.List<Skin> skins, string id, string displayName)
        {
            Sprite sprite = LoadSkinSprite(id);
            if (sprite != null)
            {
                skins.Add(new Skin(id, displayName, sprite, new[] { sprite }));
            }
        }

        private static Sprite LoadSkinSprite(string id)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{GamePath}skins/{id}.png");
        }

        [MenuItem("Plummet/Skins/Process Dropped Art")]
        public static void ProcessDroppedArt()
        {
            const string dropDir = "Assets/Plummet/SkinDrop";
            string outDir = $"{GamePath}skins";
            Directory.CreateDirectory(dropDir);
            Directory.CreateDirectory(outDir);

            string[] files = Directory.GetFiles(dropDir, "*.png");
            int count = 0;
            foreach (string file in files)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(file)))
                {
                    continue;
                }

                Texture2D stripped = StripGreen(tex);
                string id = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                File.WriteAllBytes(Path.Combine(outDir, id + ".png"), stripped.EncodeToPNG());
                count++;
            }

            AssetDatabase.Refresh();
            ConfigureSprites();
            Debug.Log($"Plummet: processed {count} skin image(s) into {outDir}. Now run Plummet/Repair Open Scene to wire them.");
        }

        private static Texture2D StripGreen(Texture2D src)
        {
            int w = src.width;
            int h = src.height;
            Color32[] px = src.GetPixels32();

            Color32 c0 = px[0];
            Color32 c1 = px[w - 1];
            Color32 c2 = px[(h - 1) * w];
            Color32 c3 = px[h * w - 1];
            float kr = (c0.r + c1.r + c2.r + c3.r) / 4f;
            float kg = (c0.g + c1.g + c2.g + c3.g) / 4f;
            float kb = (c0.b + c1.b + c2.b + c3.b) / 4f;

            const float tol = 110f;
            const float soft = 45f;
            for (int i = 0; i < px.Length; i++)
            {
                Color32 p = px[i];
                float dr = p.r - kr;
                float dg = p.g - kg;
                float db = p.b - kb;
                float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);

                if (dist <= tol)
                {
                    p.a = 0;
                }
                else if (dist <= tol + soft)
                {
                    p.a = (byte)Mathf.RoundToInt(p.a * ((dist - tol) / soft));
                }
                else if (p.g > p.r + 25 && p.g > p.b + 25)
                {
                    p.g = (byte)Mathf.Min(p.g, (p.r + p.b) / 2 + 10);
                }

                px[i] = p;
            }

            Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels32(px);
            outTex.Apply();
            return TrimTransparent(outTex);
        }

        private static Texture2D TrimTransparent(Texture2D src)
        {
            int w = src.width;
            int h = src.height;
            Color32[] px = src.GetPixels32();

            int minX = w;
            int minY = h;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x].a > 8)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return src;
            }

            int cw = maxX - minX + 1;
            int ch = maxY - minY + 1;
            Color[] block = src.GetPixels(minX, minY, cw, ch);
            Texture2D outTex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            outTex.SetPixels(block);
            outTex.Apply();
            return outTex;
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
