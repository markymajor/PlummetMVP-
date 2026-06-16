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

        private void Awake()
        {
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

            if (instructionDistancePanel == null || instructionSpeedPanel == null)
            {
                GameManager.Instance.StartRun();
                return;
            }

            ShowInstructionDistance();
        }

        public void ShowInstructionDistance()
        {
            startPanel.SetActive(false);
            SetInstructionPanels(true, false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowInstructionSpeed()
        {
            startPanel.SetActive(false);
            SetInstructionPanels(false, true);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowHud()
        {
            startPanel.SetActive(false);
            SetInstructionPanels(false, false);
            hudPanel.SetActive(true);
            gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(int score, int highScore)
        {
            startPanel.SetActive(false);
            SetInstructionPanels(false, false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(true);
            finalScoreText.text = $"Score {score}\nBest {highScore}";
        }

        public void SetScore(int score, int highScore)
        {
            scoreText.text = score.ToString("N0");
            highScoreText.text = string.Empty;
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
            GameManager.Instance.StartRun();
        }

        public void OnDistanceBackPressed()
        {
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
    }
}
