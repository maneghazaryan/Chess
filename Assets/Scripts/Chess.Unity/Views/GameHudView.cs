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
    }
}
