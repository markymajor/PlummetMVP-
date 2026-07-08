using UnityEngine;
using UnityEngine.UI;

namespace Plummet
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject instructionDistancePanel;
        [SerializeField] private GameObject instructionSpeedPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text finalBestText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button distanceNextButton;
        [SerializeField] private Button speedNextButton;
        [SerializeField] private Button distanceBackButton;
        [SerializeField] private Button speedBackButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button shareButton;
        [SerializeField] private GameObject chooseSkinPanel;
        [SerializeField] private Button chooseSkinButton;
        [SerializeField] private Button chooseSkinBackButton;
        [SerializeField] private Button chooseSkinSelectButton;
        [SerializeField] private SkinPickerUI skinPicker;
        [Tooltip("The RESCUED! screen's bounced character: shows the selected skin's falling frame above the firefighter's net (the world player hides on game over).")]
        [SerializeField] private Image rescuedPlayerImage;
        [Tooltip("Tilt (degrees) of the rescued character, so it reads as bouncing off the net rather than pasted upright.")]
        [SerializeField] private float rescuedPlayerTilt = 35f;

        private bool shouldShowOpeningInstructions = true;
        private bool showingOpeningInstructions;

        private void Awake()
        {
            EnsurePanelsUsePortraitFrame();
            WireButton(playButton, OnPlayPressed);
            WireButton(distanceNextButton, OnDistanceNextPressed);
            WireButton(speedNextButton, OnSpeedNextPressed);
            WireButton(distanceBackButton, OnDistanceBackPressed);
            WireButton(speedBackButton, OnSpeedBackPressed);
            WireButton(resetButton, OnResetPressed);
            WireButton(homeButton, OnHomePressed);
            WireButton(shareButton, OnSharePressed);
            WireButton(chooseSkinButton, ShowChooseSkin);
            WireButton(chooseSkinBackButton, OnChooseSkinBack);
            WireButton(chooseSkinSelectButton, OnChooseSkinSelect);
        }

        public void ShowChooseSkin()
        {
            EnsurePanelsUsePortraitFrame();
            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }

            SetInstructionPanels(false, false);
            if (hudPanel != null)
            {
                hudPanel.SetActive(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (chooseSkinPanel != null)
            {
                chooseSkinPanel.SetActive(true);
            }
        }

        private void OnChooseSkinBack()
        {
            // Return without changing the saved skin (the pending pick is discarded).
            if (chooseSkinPanel != null)
            {
                chooseSkinPanel.SetActive(false);
            }

            ShowStart();
        }

        private void OnChooseSkinSelect()
        {
            // Confirm the pending pick, apply it, and return to the start screen.
            if (skinPicker != null)
            {
                skinPicker.Commit();
            }

            if (chooseSkinPanel != null)
            {
                chooseSkinPanel.SetActive(false);
            }

            ShowStart();
        }

        private void HideChooseSkin()
        {
            if (chooseSkinPanel != null)
            {
                chooseSkinPanel.SetActive(false);
            }
        }

        public void ShowStart()
        {
            EnsurePanelsUsePortraitFrame();
            HideChooseSkin();
            if (shouldShowOpeningInstructions && HasInstructionPanels())
            {
                shouldShowOpeningInstructions = false;
                showingOpeningInstructions = true;
                ShowInstructionDistance();
                return;
            }

            startPanel.SetActive(true);
            SetInstructionPanels(false, false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void BeginStartFlow()
        {
            if (startPanel != null && !startPanel.activeSelf)
            {
                return;
            }

            // Seamless drop: the real pinned player tips into the dive while the shaft
            // accelerates from rest into the run - no UI actor, no hand-off.
            GameManager.Instance.BeginDrop();
        }

        /// <summary>Hide the home-screen chrome (title/prompt/buttons) when the drop begins.</summary>
        public void HideStartChrome()
        {
            if (startPanel != null)
            {
                startPanel.SetActive(false);
            }
        }

        public void ShowInstructionDistance()
        {
            EnsurePanelsUsePortraitFrame();
            HideChooseSkin();
            startPanel.SetActive(false);
            SetInstructionPanels(true, false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowInstructionSpeed()
        {
            EnsurePanelsUsePortraitFrame();
            HideChooseSkin();
            startPanel.SetActive(false);
            SetInstructionPanels(false, true);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowHud()
        {
            EnsurePanelsUsePortraitFrame();
            HideChooseSkin();
            startPanel.SetActive(false);
            SetInstructionPanels(false, false);
            hudPanel.SetActive(true);
            gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(int score, int highScore)
        {
            EnsurePanelsUsePortraitFrame();
            HideChooseSkin();
            startPanel.SetActive(false);
            SetInstructionPanels(false, false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(true);
            if (finalScoreText != null)
            {
                finalScoreText.text = score.ToString("N0");
            }

            if (finalBestText != null)
            {
                finalBestText.text = $"Best {highScore:N0}";
            }

            ShowRescuedPlayer();
        }

        // The bounced character on the net: the selected skin's falling frame, sized to
        // match the in-game character (world size projected through the camera onto the
        // UI reference resolution) and tilted so it reads as mid-bounce.
        private void ShowRescuedPlayer()
        {
            if (rescuedPlayerImage == null)
            {
                return;
            }

            Skin skin = SkinLibrary.Instance != null ? SkinLibrary.Instance.Selected : null;
            Sprite falling = skin != null ? skin.FirstFrame : null;
            if (falling == null)
            {
                rescuedPlayerImage.enabled = false;
                return;
            }

            rescuedPlayerImage.enabled = true;
            rescuedPlayerImage.sprite = falling;
            rescuedPlayerImage.color = Color.white; // the baked placeholder is translucent
            rescuedPlayerImage.preserveAspect = true;
            rescuedPlayerImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rescuedPlayerTilt);

            // Match the in-game size: the world player's visible body length is 1.66u;
            // convert to reference-resolution pixels via the camera projection, then scale
            // the full quad so the sprite's visible longest dimension lands on that.
            Camera cam = Camera.main;
            float ortho = cam != null && cam.orthographic ? cam.orthographicSize : 5.5f;
            float referenceHeight = 1920f;
            CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = FindFirstObjectByType<CanvasScaler>();
            }

            if (scaler != null && scaler.referenceResolution.y > 0f)
            {
                referenceHeight = scaler.referenceResolution.y;
            }

            float targetPx = 1.66f / (2f * ortho) * referenceHeight;
            float visibleLongest = VisibleLongestDimension(falling);
            float quadLongest = Mathf.Max(falling.bounds.size.x, falling.bounds.size.y);
            float k = visibleLongest > 0.0001f ? targetPx * (quadLongest / visibleLongest) : targetPx;
            float aspect = falling.rect.height > 0f ? falling.rect.width / falling.rect.height : 1f;
            rescuedPlayerImage.rectTransform.sizeDelta = aspect >= 1f
                ? new Vector2(k, k / aspect)
                : new Vector2(k * aspect, k);
        }

        // Longest side of the sprite's visible (tight-mesh) bbox, world units.
        private static float VisibleLongestDimension(Sprite sprite)
        {
            Vector2[] verts = sprite.vertices;
            if (verts == null || verts.Length == 0)
            {
                return Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            }

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].x < minX) minX = verts[i].x;
                if (verts[i].x > maxX) maxX = verts[i].x;
                if (verts[i].y < minY) minY = verts[i].y;
                if (verts[i].y > maxY) maxY = verts[i].y;
            }

            return Mathf.Max(maxX - minX, maxY - minY);
        }

        public void SetScore(int score, int highScore)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0");
            }

            if (highScoreText != null)
            {
                highScoreText.text = highScore > 0 ? $"Best {highScore:N0}" : string.Empty;
            }
        }

        public void OnPlayPressed()
        {
            BeginStartFlow();
        }

        public void OnDistanceNextPressed()
        {
            ShowInstructionSpeed();
        }

        public void OnSpeedNextPressed()
        {
            if (showingOpeningInstructions)
            {
                showingOpeningInstructions = false;
                ShowStart();
                return;
            }

            GameManager.Instance.StartRun();
        }

        public void OnDistanceBackPressed()
        {
            showingOpeningInstructions = false;
            ShowStart();
        }

        public void OnSpeedBackPressed()
        {
            ShowInstructionDistance();
        }

        public void OnResetPressed()
        {
            GameManager.Instance.RestartRun();
        }

        public void OnHomePressed()
        {
            GameManager.Instance.ShowStartScreen();
        }

        public void OnSharePressed()
        {
            Debug.Log("Share placeholder: wire native sharing later.");
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void SetInstructionPanels(bool distanceVisible, bool speedVisible)
        {
            if (instructionDistancePanel != null)
            {
                instructionDistancePanel.SetActive(distanceVisible);
            }

            if (instructionSpeedPanel != null)
            {
                instructionSpeedPanel.SetActive(speedVisible);
            }
        }

        private bool HasInstructionPanels()
        {
            return instructionDistancePanel != null && instructionSpeedPanel != null;
        }

        private void EnsurePanelsUsePortraitFrame()
        {
            PortraitScreenFrame screenFrame = FindFirstObjectByType<PortraitScreenFrame>();
            if (screenFrame == null)
            {
                return;
            }

            RectTransform frameRect = screenFrame.GetComponent<RectTransform>();
            if (frameRect == null)
            {
                return;
            }

            PlacePanelInFrame(startPanel, frameRect);
            PlacePanelInFrame(instructionDistancePanel, frameRect);
            PlacePanelInFrame(instructionSpeedPanel, frameRect);
            PlacePanelInFrame(hudPanel, frameRect);
            PlacePanelInFrame(gameOverPanel, frameRect);
            screenFrame.ApplyFrame();
        }

        private static void PlacePanelInFrame(GameObject panel, RectTransform frame)
        {
            if (panel == null)
            {
                return;
            }

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            if (rect.parent != frame)
            {
                rect.SetParent(frame, false);
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
