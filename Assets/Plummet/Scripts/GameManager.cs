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
        Dropping,
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
        [Tooltip("Gravity for the trapdoor drop: the scroll accelerates from 0 at this rate (world units/s^2) until it reaches baseScrollSpeed, at which point the run begins. So the home->run transition is one continuous accelerating fall.")]
        [SerializeField] private float dropAcceleration = 8f;
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
        public bool IsScrolling => State == GameState.Dropping || State == GameState.Playing;

        /// <summary>
        /// Hold the corridor centered and wide: on the home screen and through the drop +
        /// grace window, so the open mouth the player drops into stays fair and the same
        /// corridor carries continuously from the static home into the scrolling run.
        /// </summary>
        public bool HoldCorridorOpen => State == GameState.Start || State == GameState.Dropping || InGrace;

        /// <summary>0..1 progress of the trapdoor drop, by how far the scroll has accelerated
        /// from rest toward the run's base speed. Drives the player's standing->falling pose.</summary>
        public float DropProgress => State == GameState.Dropping ? Mathf.Clamp01(ScrollSpeed / Mathf.Max(0.01f, baseScrollSpeed)) : (State == GameState.Start ? 0f : 1f);

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
            if (State == GameState.Dropping)
            {
                // The drop IS the scroll accelerating from rest under gravity. When it
                // reaches the run's base speed, the fall has carried the player into the
                // shaft and the run takes over - one continuous accelerating fall.
                ScrollSpeed += dropAcceleration * Time.deltaTime;
                if (ScrollSpeed >= baseScrollSpeed)
                {
                    ScrollSpeed = baseScrollSpeed;
                    BeginRun();
                }

                return;
            }

            if (!IsPlaying)
            {
                // Keyboard shortcut to drop; pointer taps go through the Start Panel's Play
                // Button so they can't fire on the Choose Player cards/buttons.
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

        /// <summary>Begin the trapdoor drop: the shaft starts scrolling from rest and
        /// accelerates (gravity) into the run, while the pinned player tips into the dive.</summary>
        public void BeginDrop()
        {
            if (State != GameState.Start)
            {
                return;
            }

            State = GameState.Dropping;
            ScrollSpeed = 0f;
            player.gameObject.SetActive(true);
            player.BeginDrop();
            uiManager.HideStartChrome();
        }

        // Drop finished: hand straight into the run WITHOUT resetting the player or the
        // corridor - the player is already pinned in its falling pose and the corridor it
        // fell into carries on scrolling, so there is no snap. Grace keeps it fair.
        private void BeginRun()
        {
            State = GameState.Playing;
            runTime = 0f;
            ScrollSpeed = baseScrollSpeed;

            player.gameObject.SetActive(true);
            player.BeginRun();
            scoreManager.ResetScore();
            obstacleSpawner.ResetSpawner();
            uiManager.ShowHud();
        }

        // Direct run start (a Game Over retry): regenerate everything and skip the drop.
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
            ScrollSpeed = 0f; // static home: shaft and surface sit still
            obstacleSpawner.ReleaseAll();
            if (pathManager != null)
            {
                pathManager.ResetPath();
            }

            // The REAL player stands on the ledge (no UI stand-in), pinned at its run
            // position, so the drop is one continuous character with no hand-off.
            player.gameObject.SetActive(true);
            player.ShowStanding();
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
