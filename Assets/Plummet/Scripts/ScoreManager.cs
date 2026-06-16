using UnityEngine;

namespace Plummet
{
    public sealed class ScoreManager : MonoBehaviour
    {
        private const string HighScoreKey = "PlummetHighScore";

        [SerializeField] private float scoreMultiplier = 10f;
        [SerializeField] private UIManager uiManager;

        public int Score { get; private set; }
        public int HighScore { get; private set; }

        private float rawScore;

        private void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            rawScore += GameManager.Instance.ScrollSpeed * scoreMultiplier * Time.deltaTime;
            Score = Mathf.FloorToInt(rawScore);
            uiManager.SetScore(Score, HighScore);
        }

        public void ResetScore()
        {
            rawScore = 0f;
            Score = 0;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            uiManager.SetScore(Score, HighScore);
        }

        public void SaveHighScore()
        {
            if (Score <= HighScore)
            {
                return;
            }

            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }
    }
}
