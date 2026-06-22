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
        [SerializeField] private Button playButton;
        [SerializeField] private Button distanceNextButton;
        [SerializeField] private Button speedNextButton;
        [SerializeField] private Button distanceBackButton;
        [SerializeField] private Button speedBackButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button shareButton;
        [SerializeField] private IntroTransition introTransition;
        [SerializeField] private GameObject chooseSkinPanel;
        [SerializeField] private Button chooseSkinButton;
        [SerializeField] private Button chooseSkinBackButton;
        [SerializeField] private Button chooseSkinSelectButton;
        [SerializeField] private SkinPickerUI skinPicker;
        [Tooltip("The standing character on the Start Panel (also the trapdoor falling actor). Re-skinned to the selected character.")]
        [SerializeField] private Image startCharacterImage;

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
            RefreshStartCharacterSkin();

            if (introTransition != null)
            {
                introTransition.ResetIntro();
            }
        }

        /// <summary>
        /// Shows the selected character on the standing start preview (which is also
        /// the trapdoor falling actor), so the kid's pick is the one standing on the
        /// land and dropping through. Safe to call before a library exists.
        /// </summary>
        // On-screen height (px, 1080x1920 ref) for the standing/falling start character,
        // matching the in-shaft player's normalized height so all three read the same size.
        private const float StartCharacterHeight = 514f;

        public void RefreshStartCharacterSkin()
        {
            if (startCharacterImage == null || SkinLibrary.Instance == null)
            {
                return;
            }

            Skin skin = SkinLibrary.Instance.Selected;
            if (skin == null || skin.Standing == null)
            {
                return;
            }

            startCharacterImage.sprite = skin.Standing;
            startCharacterImage.preserveAspect = true;
            SizeToHeight(startCharacterImage.rectTransform, skin.Standing, StartCharacterHeight);

            // Keep the trapdoor falling actor on this skin's falling pose, so the drop
            // shows the chosen character at the same normalized size.
            if (introTransition != null)
            {
                introTransition.SetFallingSprite(skin.FirstFrame != null ? skin.FirstFrame : skin.Standing);
            }
        }

        // Size a rect so a preserve-aspect sprite renders at a fixed height regardless of
        // the art's aspect ratio (width follows the sprite's aspect).
        private static void SizeToHeight(RectTransform rect, Sprite sprite, float height)
        {
            if (rect == null || sprite == null || sprite.rect.height <= 0f)
            {
                return;
            }

            float aspect = sprite.rect.width / sprite.rect.height;
            rect.sizeDelta = new Vector2(height * aspect, height);
        }

        public void BeginStartFlow()
        {
            if (startPanel != null && !startPanel.activeSelf)
            {
                return;
            }

            if (introTransition != null)
            {
                if (introTransition.IsPlaying)
                {
                    return;
                }

                introTransition.Play(() => GameManager.Instance.StartRun());
                return;
            }

            GameManager.Instance.StartRun();
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
            finalScoreText.text = $"Score {score}\nBest {highScore}";
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
