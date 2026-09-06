using System;
using Chess.AI;
using Chess.Core.Game;
using Chess.Unity.Audio;
using Chess.Unity.Config;
using Chess.Unity.Controllers;
using Chess.Unity.Input;
using Chess.Unity.Players;
using Chess.Unity.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Chess.Unity.Bootstrap
{
    /// <summary>
    /// The composition root. Every object graph in the application is built here, once, and
    /// nowhere else.
    /// </summary>
    /// <remarks>
    /// This is the deliberate exception to the rule that classes should not construct their own
    /// collaborators: something has to, and confining it to one class is what lets every other
    /// class take its dependencies through its constructor. There are no singletons and no service
    /// locator, so what depends on what is legible from this one file.
    /// <para>
    /// It is also the only place where the Unity scene meets the rest of the design: it reads the
    /// serialised references, hands them to plain C# objects as interfaces, and owns their
    /// lifetime.
    /// </para>
    /// </remarks>
    public sealed class GameInstaller : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardTheme _boardTheme;
        [SerializeField] private PieceSpriteSet _pieceSprites;

        [Tooltip("Optional. Falls back to the built-in difficulty curve when empty.")]
        [SerializeField] private AiDifficultyConfig _aiDifficultyConfig;

        [Header("Scene")]
        [SerializeField] private Camera _boardCamera;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private PointerBoardInputSource _inputSource;
        [SerializeField] private GameHudView _hudView;
        [SerializeField] private PromotionDialogView _promotionView;
        [SerializeField] private MainMenuView _mainMenuView;
        [SerializeField] private ChessAudioService _audioService;

        [Header("Startup")]
        [Tooltip("Skip the menu and start immediately. Useful while iterating on the board itself.")]
        [SerializeField] private bool _startImmediately;

        [SerializeField] private GameMode _immediateMode = GameMode.HumanVsHuman;
        [SerializeField] private AiDifficulty _immediateDifficulty = AiDifficulty.Medium;

        [Tooltip("Fit the camera to the board on start.")]
        [SerializeField] private bool _frameCameraToBoard = true;

        private ChessGame _game;
        private BoardInteractionController _interactionController;
        private GameController _gameController;

        private void Awake()
        {
            EnsurePlayableScene();

            if (!ValidateSceneReferences())
            {
                enabled = false;
                return;
            }

            WarnAboutMissingArt();

            // Model. Knows nothing about Unity, so it is built first and depends on nothing here.
            _game = ChessGame.CreateStandard();

            // View. Initialised with its configuration and input source before anything renders.
            _inputSource.Initialise(_boardCamera, _boardView);
            _boardView.Initialise(_boardTheme, _pieceSprites, _inputSource);
            _hudView.Initialise(_pieceSprites);
            _promotionView.Initialise(_pieceSprites);

            // Controllers. Each is handed its collaborators as interfaces, never as components.
            _interactionController = new BoardInteractionController(_game, _boardView, _promotionView);

            IChessEngineFactory engineFactory = ResolveEngineFactory();
            var playerFactory = new PlayerFactory(
                _interactionController,
                engineFactory,
                ResolveMinimumThinkTime());

            _gameController = new GameController(_game, playerFactory, _boardView, _hudView, _audioService);

            _mainMenuView.StartGameRequested += OnStartGameRequested;
            _mainMenuView.QuitRequested += OnQuitRequested;
            _hudView.MenuRequested += OnMenuRequested;
            _hudView.LayoutChanged += FrameCameraToBoard;
        }

        private void Start()
        {
            if (_frameCameraToBoard)
            {
                FrameCameraToBoard();
            }

            _boardView.RenderPosition(_game.Position);

            if (_startImmediately)
            {
                _mainMenuView.Hide();
                _gameController.StartNewMatch(
                    new GameSetup(_immediateMode, _immediateDifficulty, Core.Primitives.PieceColor.White));
                return;
            }

            _hudView.Hide();
            _mainMenuView.Show();
        }

        private void OnDestroy()
        {
            if (_mainMenuView != null)
            {
                _mainMenuView.StartGameRequested -= OnStartGameRequested;
                _mainMenuView.QuitRequested -= OnQuitRequested;
            }

            if (_hudView != null)
            {
                _hudView.MenuRequested -= OnMenuRequested;
                _hudView.LayoutChanged -= FrameCameraToBoard;
            }

            // Disposal order mirrors construction: the controller cancels any search in flight,
            // then the interaction controller detaches from the view's events.
            _gameController?.Dispose();
            _interactionController?.Dispose();
        }

        private void OnStartGameRequested(GameSetup setup)
        {
            _mainMenuView.Hide();
            _gameController.StartNewMatch(setup);
            FrameCameraToBoard();
        }

        private void OnMenuRequested()
        {
            _hudView.Hide();
            _mainMenuView.Show();
        }

        private static void OnQuitRequested()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IChessEngineFactory ResolveEngineFactory()
        {
            return _aiDifficultyConfig != null ? _aiDifficultyConfig : (IChessEngineFactory)new ChessEngineFactory();
        }

        private TimeSpan ResolveMinimumThinkTime()
        {
            return _aiDifficultyConfig != null
                ? _aiDifficultyConfig.MinimumThinkTime
                : TimeSpan.FromSeconds(0.35);
        }

        /// <summary>
        /// Fits the board into the screen rectangle the HUD is not using, so a portrait chrome
        /// strip or a landscape side panel never sits on top of the squares.
        /// </summary>
        private void FrameCameraToBoard()
        {
            if (!_frameCameraToBoard || _boardCamera == null || !_boardCamera.orthographic || _boardView == null)
            {
                return;
            }

            _hudView.ApplyResponsiveLayout();
            Canvas.ForceUpdateCanvases();

            float left = 0f;
            float right = 0f;
            float top = 0f;
            float bottom = 0f;
            _hudView.GetBoardSafeInsets(out left, out right, out top, out bottom);

            float widthFraction = Mathf.Max(0.28f, 1f - left - right);
            float heightFraction = Mathf.Max(0.28f, 1f - top - bottom);

            float boardSize = _boardView.Geometry.BoardExtent * 1.06f;
            float aspect = _boardCamera.aspect > 0f ? _boardCamera.aspect : 1f;

            float sizeForHeight = boardSize / (2f * heightFraction);
            float sizeForWidth = boardSize / (2f * aspect * widthFraction);
            _boardCamera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

            float worldWidth = 2f * _boardCamera.orthographicSize * aspect;
            float worldHeight = 2f * _boardCamera.orthographicSize;
            float viewportCenterX = left + widthFraction * 0.5f;
            float viewportCenterY = bottom + heightFraction * 0.5f;

            Vector3 boardCentre = _boardView.transform.position;
            _boardCamera.transform.position = new Vector3(
                boardCentre.x - (viewportCenterX - 0.5f) * worldWidth,
                boardCentre.y - (viewportCenterY - 0.5f) * worldHeight,
                _boardCamera.transform.position.z);
        }

        /// <summary>
        /// Builds any missing scene objects so a single installer plus the three config assets is
        /// enough to play. Designed canvases and prefabs still win when they are already assigned.
        /// </summary>
        private void EnsurePlayableScene()
        {
            if (_boardCamera == null)
            {
                _boardCamera = Camera.main;
            }

            EnsureEventSystem();
            EnsureBoard();
            EnsureAudio();
            EnsureUserInterface();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var host = new GameObject("EventSystem");
            host.AddComponent<EventSystem>();
            host.AddComponent<InputSystemUIInputModule>();
        }

        private void EnsureBoard()
        {
            if (_boardView == null)
            {
                _boardView = FindFirstObjectByType<BoardView>();
            }

            if (_boardView == null)
            {
                var host = new GameObject("Board");
                host.transform.position = Vector3.zero;
                _boardView = host.AddComponent<BoardView>();
            }

            if (_inputSource == null)
            {
                _inputSource = _boardView.GetComponent<PointerBoardInputSource>();
            }

            if (_inputSource == null)
            {
                _inputSource = _boardView.gameObject.AddComponent<PointerBoardInputSource>();
            }
        }

        private void EnsureAudio()
        {
            if (_audioService == null)
            {
                _audioService = FindFirstObjectByType<ChessAudioService>();
            }

            if (_audioService == null)
            {
                var host = new GameObject("Audio");
                host.AddComponent<AudioSource>().playOnAwake = false;
                _audioService = host.AddComponent<ChessAudioService>();
            }
        }

        private void EnsureUserInterface()
        {
            if (_hudView != null && _mainMenuView != null && _promotionView != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = UiFactory.OverlayCanvas("Canvas");
            }

            Transform root = canvas.transform;

            if (_hudView == null)
            {
                _hudView = GameHudView.CreateDefault(root);
            }

            if (_mainMenuView == null)
            {
                _mainMenuView = MainMenuView.CreateDefault(root);
            }

            if (_promotionView == null)
            {
                _promotionView = PromotionDialogView.CreateDefault(root);
            }
        }

        private bool ValidateSceneReferences()
        {
            bool isValid = true;

            isValid &= Require(_boardTheme, nameof(_boardTheme));
            isValid &= Require(_boardView, nameof(_boardView));
            isValid &= Require(_inputSource, nameof(_inputSource));
            isValid &= Require(_hudView, nameof(_hudView));
            isValid &= Require(_promotionView, nameof(_promotionView));
            isValid &= Require(_mainMenuView, nameof(_mainMenuView));
            isValid &= Require(_audioService, nameof(_audioService));

            return isValid;
        }

        private bool Require(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(GameInstaller)} is missing its '{fieldName}' reference.", this);
            return false;
        }

        private void WarnAboutMissingArt()
        {
            // Missing art is not fatal: the game is fully playable with blank sprites, so this
            // warns rather than blocks.
            if (_pieceSprites == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameInstaller)} has no {nameof(PieceSpriteSet)}; pieces will be invisible.", this);
                return;
            }

            if (!_pieceSprites.IsComplete(out string missing))
            {
                Debug.LogWarning($"{nameof(PieceSpriteSet)} is missing sprites for: {missing}.", _pieceSprites);
            }
        }
    }
}
