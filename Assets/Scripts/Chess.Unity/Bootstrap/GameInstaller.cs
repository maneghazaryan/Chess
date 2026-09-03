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
        /// Sizes an orthographic camera so the whole board fits with a small margin, which saves
        /// re-framing by hand whenever the square size changes.
        /// </summary>
        private void FrameCameraToBoard()
        {
            if (_boardCamera == null || !_boardCamera.orthographic || _boardTheme == null)
            {
                return;
            }

            const float marginFraction = 1.1f;
            float boardExtent = _boardView.Geometry.BoardExtent * 0.5f * marginFraction;

            float aspect = _boardCamera.aspect > 0f ? _boardCamera.aspect : 1f;
            _boardCamera.orthographicSize = aspect >= 1f ? boardExtent : boardExtent / aspect;

            Vector3 boardCentre = _boardView.transform.position;
            _boardCamera.transform.position = new Vector3(
                boardCentre.x, boardCentre.y, _boardCamera.transform.position.z);
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
