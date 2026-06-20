using UnityEngine;
using UnityEngine.UI;

namespace Plummet
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string OpeningInstructionsSeenKey = "PlummetOpeningInstructionsSeen";

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

        private bool shouldShowOpeningInstructions;
        private bool showingOpeningInstructions;

        private void Awake()
        {
            shouldShowOpeningInstructions = PlayerPrefs.GetInt(OpeningInstructionsSeenKey, 0) == 0;
            EnsurePanelsUsePortraitFrame();
            WireButton(playButton, OnPlayPressed);
            WireButton(distanceNextButton, OnDistanceNextPressed);
            WireButton(speedNextButton, OnSpeedNextPressed);
            WireButton(distanceBackButton, OnDistanceBackPressed);
            WireButton(speedBackButton, OnSpeedBackPressed);
            WireButton(resetButton, OnResetPressed);
            WireButton(homeButton, OnHomePressed);
            WireButton(shareButton, OnSharePressed);
        }

        public void ShowStart()
        {
            EnsurePanelsUsePortraitFrame();
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

            if (introTransition != null)
            {
                introTransition.ResetIntro();
            }
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
            startPanel.SetActive(false);
            SetInstructionPanels(true, false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowInstructionSpeed()
        {
            EnsurePanelsUsePortraitFrame();
            startPanel.SetActive(false);
            SetInstructionPanels(false, true);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowHud()
        {
            EnsurePanelsUsePortraitFrame();
            startPanel.SetActive(false);
            SetInstructionPanels(false, false);
            hudPanel.SetActive(true);
            gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(int score, int highScore)
        {
            EnsurePanelsUsePortraitFrame();
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
                MarkOpeningInstructionsSeen();
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

        private static void MarkOpeningInstructionsSeen()
        {
            PlayerPrefs.SetInt(OpeningInstructionsSeenKey, 1);
            PlayerPrefs.Save();
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
