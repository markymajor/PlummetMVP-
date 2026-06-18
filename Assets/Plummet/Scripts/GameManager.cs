using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Plummet
{
    public enum GameState
    {
        Start,
        Playing,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Difficulty")]
        [SerializeField] private float baseScrollSpeed = 3.25f;
        [SerializeField] private float maxScrollSpeed = 8.5f;
        [SerializeField] private float speedIncreasePerSecond = 0.045f;

        [Header("Scene References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private ObstacleSpawner obstacleSpawner;
        [SerializeField] private PathManager pathManager;

        private float runTime;

        public GameState State { get; private set; } = GameState.Start;
        public float ScrollSpeed { get; private set; }
        public bool IsPlaying => State == GameState.Playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            EnsureFullCameraRender();
            DisableLegacyFullScreenMatte();
        }

        private void Start()
        {
            ScrollSpeed = baseScrollSpeed;
            ShowStartScreen();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                if (State == GameState.Start && StartInputPressed())
                {
                    uiManager.BeginStartFlow();
                }

                return;
            }

            runTime += Time.deltaTime;
            ScrollSpeed = Mathf.Min(maxScrollSpeed, baseScrollSpeed + runTime * speedIncreasePerSecond);
        }

        public void StartRun()
        {
            State = GameState.Playing;
            runTime = 0f;
            ScrollSpeed = baseScrollSpeed;

            player.gameObject.SetActive(true);
            player.ResetPlayer();
            if (pathManager != null)
            {
                pathManager.ResetPath();
            }

            scoreManager.ResetScore();
            obstacleSpawner.ResetSpawner();
            uiManager.ShowHud();
        }

        public void TriggerGameOver()
        {
            if (State == GameState.GameOver)
            {
                return;
            }

            State = GameState.GameOver;
            scoreManager.SaveHighScore();
            uiManager.ShowGameOver(scoreManager.Score, scoreManager.HighScore);
        }

        public void RestartRun()
        {
            obstacleSpawner.ReleaseAll();
            StartRun();
        }

        public void ShowStartScreen()
        {
            State = GameState.Start;
            obstacleSpawner.ReleaseAll();
            if (pathManager != null)
            {
                pathManager.ResetPath();
            }

            player.ResetPlayer();
            player.gameObject.SetActive(false);
            scoreManager.ResetScore();
            uiManager.ShowStart();
        }

        public void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private bool StartInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool keyboard = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            return keyboard || mouse || touch;
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
        }

        private static void EnsureFullCameraRender()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            PortraitViewportFitter oldFitter = camera.GetComponent<PortraitViewportFitter>();
            if (oldFitter != null)
            {
                oldFitter.enabled = false;
            }

            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.backgroundColor = Color.black;
        }

        private static void DisableLegacyFullScreenMatte()
        {
            GameObject oldMatte = GameObject.Find("Portrait Letterbox Matte");
            if (oldMatte != null)
            {
                oldMatte.SetActive(false);
            }
        }
    }
}
