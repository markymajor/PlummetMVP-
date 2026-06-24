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
        [SerializeField] private float baseScrollSpeed = 5.5f;
        [SerializeField] private float maxScrollSpeed = 12f;
        [SerializeField] private float speedIncreasePerSecond = 0.06f;
        [Tooltip("Gentle scroll speed used on the home/attract screen so the shaft is alive behind the menu before the run begins.")]
        [SerializeField] private float attractScrollSpeed = 2.5f;
        [Tooltip("Brief forgiving window at the start of each run: the corridor stays centered and wide, difficulty doesn't ramp, and no obstacles spawn, so the trapdoor drop never lands straight into a hazard.")]
        [SerializeField] private float graceDuration = 1.2f;

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

        /// <summary>
        /// True only while Playing: the shaft is STATIC on the home screen and during the
        /// trapdoor drop, and only begins scrolling once the run starts. So the drop reads
        /// as the player falling into a still shaft, then the world rushing up past him.
        /// </summary>
        public bool IsScrolling => State == GameState.Playing;

        /// <summary>
        /// Hold the corridor centered and wide: on the home screen (so the static shaft the
        /// player drops into is a fair, open mouth) and through the grace window (so the
        /// first scrolling moments stay fair). Lets the same corridor carry from drop to run.
        /// </summary>
        public bool HoldCorridorOpen => State == GameState.Start || InGrace;

        /// <summary>
        /// True during the brief grace window at the start of a run. While true the
        /// corridor stays centered and wide and obstacles hold off, giving a fair
        /// landing before difficulty and hazards kick in.
        /// </summary>
        public bool InGrace => State == GameState.Playing && runTime < graceDuration;

        /// <summary>
        /// Difficulty progress in the 0..1 range, derived from the current scroll speed.
        /// Other systems should read this instead of duplicating the speed constants.
        /// </summary>
        public float DifficultyT => Mathf.InverseLerp(baseScrollSpeed, maxScrollSpeed, ScrollSpeed);

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
            ValidateReferences();
            EnsureFullCameraRender();
            DisableLegacyFullScreenMatte();
        }

        private void ValidateReferences()
        {
            if (player == null)
            {
                Debug.LogError("GameManager is missing its PlayerController reference.", this);
            }

            if (scoreManager == null)
            {
                Debug.LogError("GameManager is missing its ScoreManager reference.", this);
            }

            if (uiManager == null)
            {
                Debug.LogError("GameManager is missing its UIManager reference.", this);
            }

            if (obstacleSpawner == null)
            {
                Debug.LogError("GameManager is missing its ObstacleSpawner reference.", this);
            }

            if (pathManager == null)
            {
                Debug.LogError("GameManager is missing its PathManager reference.", this);
            }
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
                // Pointer "tap to drop" is handled by the full-screen Play Button on the
                // Start Panel, which respects UI layering (the Players/Back buttons block
                // it) and is inactive on the Choose Player / instruction screens. Only the
                // keyboard shortcut is handled here, so taps meant for cards or buttons
                // can never start the run.
                if (State == GameState.Start && StartInputPressed())
                {
                    uiManager.BeginStartFlow();
                }

                return;
            }

            runTime += Time.deltaTime;
            // Hold difficulty flat during the grace window, then ramp.
            float rampTime = Mathf.Max(0f, runTime - graceDuration);
            ScrollSpeed = Mathf.Min(maxScrollSpeed, baseScrollSpeed + rampTime * speedIncreasePerSecond);
        }

        public void StartRun()
        {
            // When the run begins straight from the home/attract screen the shaft is
            // already scrolling, so keep that corridor instead of regenerating it: a
            // ResetPath here would snap the walls to a fresh layout mid-drop. A fresh
            // run from Game Over (RestartRun, state != Start) still regenerates it.
            bool fromAttract = State == GameState.Start;

            State = GameState.Playing;
            runTime = 0f;
            ScrollSpeed = baseScrollSpeed;

            player.gameObject.SetActive(true);
            player.ResetPlayer();
            if (pathManager != null && !fromAttract)
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
            ScrollSpeed = attractScrollSpeed;
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

        // Keyboard only — pointer/touch "tap to drop" goes through the Start Panel's
        // Play Button so it can't fire for taps on the Choose Player cards or buttons.
        private bool StartInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
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
            // Keep the scene-configured light shaft background with a solid clear; don't
            // force black (that would paint the shaft centre black under URP).
            camera.clearFlags = CameraClearFlags.SolidColor;
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
