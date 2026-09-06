using System;
using System.Collections.Generic;
using Chess.Core.Game;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// The in-game interface: whose turn it is, the game status, the score sheet, captured
    /// material and the game control buttons.
    /// </summary>
    /// <remarks>
    /// Every serialised reference is optional. A missing label is simply not written to, so the
    /// game is playable against a bare canvas and the UI can be assembled piece by piece without
    /// ever hitting a null reference.
    /// </remarks>
    public sealed class GameHudView : MonoBehaviour, IGameHudView
    {
        [Header("Status")]
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private GameObject _thinkingIndicator;
        [SerializeField] private TMP_Text _thinkingLabel;

        [Header("Panels")]
        [SerializeField] private GameObject _root;
        [SerializeField] private MoveListView _moveListView;
        [SerializeField] private CapturedPiecesView _whiteCapturesView;
        [SerializeField] private CapturedPiecesView _blackCapturesView;

        [Header("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _menuButton;

        [Header("Game over")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TMP_Text _gameOverLabel;

        public event Action RestartRequested;

        public event Action UndoRequested;

        public event Action MenuRequested;

        public void Initialise(PieceSpriteSet pieceSprites)
        {
            if (_whiteCapturesView != null)
            {
                _whiteCapturesView.Initialise(pieceSprites);
            }

            if (_blackCapturesView != null)
            {
                _blackCapturesView.Initialise(pieceSprites);
            }
        }

        public void SetActivePlayer(PieceColor color, string playerName)
        {
            if (_turnLabel != null)
            {
                _turnLabel.text = $"{color} to move  -  {playerName}";
            }
        }

        public void SetStatus(GameStatus status, GameResult result)
        {
            string description = Describe(status, result);

            if (_statusLabel != null)
            {
                _statusLabel.text = description;
            }

            bool isOver = status.IsGameOver();

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(isOver);
            }

            if (_gameOverLabel != null && isOver)
            {
                _gameOverLabel.text = description;
            }
        }

        public void SetThinking(bool isThinking, string playerName)
        {
            if (_thinkingIndicator != null)
            {
                _thinkingIndicator.SetActive(isThinking);
            }

            if (_thinkingLabel != null && isThinking)
            {
                _thinkingLabel.text = $"{playerName} is thinking...";
            }
        }

        public void AppendMove(MoveRecord record)
        {
            if (_moveListView != null)
            {
                _moveListView.Append(record);
            }
        }

        public void RemoveLastMove()
        {
            if (_moveListView != null)
            {
                _moveListView.RemoveLast();
            }
        }

        public void ClearMoveList()
        {
            if (_moveListView != null)
            {
                _moveListView.Clear();
            }
        }

        public void SetCapturedPieces(IReadOnlyList<Piece> capturedByWhite, IReadOnlyList<Piece> capturedByBlack)
        {
            if (_whiteCapturesView != null)
            {
                _whiteCapturesView.SetCapturedPieces(
                    capturedByWhite,
                    CapturedPiecesView.MaterialAdvantageInPawns(capturedByWhite, capturedByBlack, PieceColor.White));
            }

            if (_blackCapturesView != null)
            {
                _blackCapturesView.SetCapturedPieces(
                    capturedByBlack,
                    CapturedPiecesView.MaterialAdvantageInPawns(capturedByWhite, capturedByBlack, PieceColor.Black));
            }
        }

        public void SetUndoAvailable(bool available)
        {
            if (_undoButton != null)
            {
                _undoButton.interactable = available;
            }
        }

        public void Show() => SetRootActive(true);

        public void Hide() => SetRootActive(false);

        private void Awake()
        {
            Bind(_restartButton, () => RestartRequested?.Invoke());
            Bind(_undoButton, () => UndoRequested?.Invoke());
            Bind(_menuButton, () => MenuRequested?.Invoke());
        }

        private void SetRootActive(bool active)
        {
            GameObject target = _root != null ? _root : gameObject;
            target.SetActive(active);
        }

        private static void Bind(Button button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private static string Describe(GameStatus status, GameResult result)
        {
            switch (status)
            {
                case GameStatus.Check:
                    return "Check";

                case GameStatus.Checkmate:
                    return result == GameResult.WhiteWins ? "Checkmate - White wins" : "Checkmate - Black wins";

                case GameStatus.Stalemate:
                    return "Draw - stalemate";

                case GameStatus.DrawByFiftyMoveRule:
                    return "Draw - fifty-move rule";

                case GameStatus.DrawByThreefoldRepetition:
                    return "Draw - threefold repetition";

                case GameStatus.DrawByInsufficientMaterial:
                    return "Draw - insufficient material";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Builds a complete HUD so the game is playable before a designed canvas exists.
        /// </summary>
        public static GameHudView CreateDefault(Transform parent)
        {
            RectTransform root = UiFactory.CreateRect(parent, "Hud");
            UiFactory.StretchFill(root);
            root.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<GameHudView>();
            view._root = root.gameObject;

            RectTransform top = UiFactory.CreateRect(root, "TopBar");
            UiFactory.Stretch(top, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -72f), new Vector2(-300f, -16f));
            UiFactory.PanelImage(top, UiFactory.Panel);
            UiFactory.Horizontal(top, 16f, 12);

            view._turnLabel = UiFactory.Label(top, "Turn", "White to move", 24f);
            UiFactory.Size(view._turnLabel, 420f, 0f);
            view._statusLabel = UiFactory.Label(top, "Status", string.Empty, 22f);
            UiFactory.Size(view._statusLabel, 280f, 0f);

            RectTransform thinkingRect = UiFactory.CreateRect(top, "Thinking");
            UiFactory.Size(thinkingRect, 260f, 0f);
            view._thinkingIndicator = thinkingRect.gameObject;
            view._thinkingLabel = UiFactory.Label(thinkingRect, "Label", string.Empty, 20f);
            UiFactory.StretchFill((RectTransform)view._thinkingLabel.transform);
            view._thinkingIndicator.SetActive(false);

            RectTransform right = UiFactory.CreateRect(root, "RightPanel");
            UiFactory.Stretch(right, new Vector2(1f, 0f), Vector2.one, new Vector2(-292f, 16f), new Vector2(-16f, -16f));
            UiFactory.PanelImage(right, UiFactory.Panel);
            UiFactory.Vertical(right, 10f, 12);

            TMP_Text moveHeader = UiFactory.Label(right, "MovesHeader", "Moves", 18f, TextAlignmentOptions.Center);
            UiFactory.Size(moveHeader, 0f, 24f);

            var moveList = UiFactory.CreateRect(right, "MoveList").gameObject.AddComponent<MoveListView>();
            UiFactory.Size(moveList, 0f, 0f).flexibleHeight = 1f;
            var moveScroll = UiFactory.VerticalScroll(moveList.transform, "Scroll", out RectTransform moveContent);
            UiFactory.StretchFill((RectTransform)moveScroll.transform);
            moveList.Bind(moveContent, moveScroll);
            view._moveListView = moveList;

            view._whiteCapturesView = CreateCaptures(right, "WhiteCaptures", "White took");
            view._blackCapturesView = CreateCaptures(right, "BlackCaptures", "Black took");

            RectTransform buttons = UiFactory.CreateRect(right, "Buttons");
            UiFactory.Size(buttons, 0f, 44f);
            UiFactory.Horizontal(buttons, 8f, 0);
            view._undoButton = UiFactory.TextButton(buttons, "Undo", "Undo", UiFactory.Button);
            view._restartButton = UiFactory.TextButton(buttons, "Restart", "Restart", UiFactory.Button);
            view._menuButton = UiFactory.TextButton(buttons, "Menu", "Menu", UiFactory.ButtonAccent);

            RectTransform gameOver = UiFactory.CreateRect(root, "GameOver");
            UiFactory.Stretch(gameOver, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240f, -70f), new Vector2(240f, 70f));
            UiFactory.PanelImage(gameOver, UiFactory.PanelSolid);
            view._gameOverLabel = UiFactory.Label(gameOver, "Label", string.Empty, 30f, TextAlignmentOptions.Center);
            UiFactory.StretchFill((RectTransform)view._gameOverLabel.transform);
            gameOver.gameObject.SetActive(false);
            view._gameOverPanel = gameOver.gameObject;

            root.gameObject.SetActive(true);
            return view;
        }

        private static CapturedPiecesView CreateCaptures(Transform parent, string objectName, string caption)
        {
            RectTransform root = UiFactory.CreateRect(parent, objectName);
            UiFactory.Size(root, 0f, 56f);
            UiFactory.Vertical(root, 2f, 0);

            TMP_Text header = UiFactory.Label(root, "Header", caption, 16f);
            UiFactory.Size(header, 0f, 18f);

            RectTransform row = UiFactory.CreateRect(root, "Row");
            UiFactory.Size(row, 0f, 32f);
            UiFactory.Horizontal(row, 4f, 0);

            RectTransform icons = UiFactory.CreateRect(row, "Icons");
            UiFactory.Size(icons, 0f, 32f).flexibleWidth = 1f;
            UiFactory.Horizontal(icons, 2f, 0);

            TMP_Text advantage = UiFactory.Label(row, "Advantage", string.Empty, 18f, TextAlignmentOptions.MidlineRight);
            UiFactory.Size(advantage, 40f, 32f);

            var view = root.gameObject.AddComponent<CapturedPiecesView>();
            view.Bind(icons, advantage);
            return view;
        }
    }
}
