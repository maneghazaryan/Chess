using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Game;
using Chess.Core.Primitives;
using Chess.Unity.Audio;
using Chess.Unity.Players;
using Chess.Unity.Views;

namespace Chess.Unity.Controllers
{
    /// <summary>
    /// Drives a match: asks each player for a move in turn, applies it to the model, and keeps the
    /// views in step.
    /// </summary>
    /// <remarks>
    /// The turn loop contains no branch on game mode and no branch on human versus AI. Both are
    /// resolved by <see cref="IPlayerFactory"/> before the loop starts, so this class reads as the
    /// rules of turn taking and nothing else.
    /// <para>
    /// Plain C# rather than a MonoBehaviour, so it can be constructed with test doubles and driven
    /// without a scene. <c>GameInstaller</c> owns its lifetime.
    /// </para>
    /// </remarks>
    public sealed class GameController : IDisposable
    {
        private readonly IChessGame _game;
        private readonly IPlayerFactory _playerFactory;
        private readonly IBoardView _boardView;
        private readonly IGameHudView _hudView;
        private readonly IChessAudioService _audio;

        private readonly IPlayer[] _players = new IPlayer[2];
        private readonly List<Piece> _capturedByWhite = new List<Piece>();
        private readonly List<Piece> _capturedByBlack = new List<Piece>();

        private CancellationTokenSource _matchCancellation;
        private GameSetup _setup = GameSetup.Default;
        private bool _isDisposed;

        public GameController(
            IChessGame game,
            IPlayerFactory playerFactory,
            IBoardView boardView,
            IGameHudView hudView,
            IChessAudioService audio)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
            _hudView = hudView ?? throw new ArgumentNullException(nameof(hudView));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));

            _hudView.RestartRequested += OnRestartRequested;
            _hudView.UndoRequested += OnUndoRequested;
        }

        /// <summary>Raised when a match ends or the player asks to return to the menu.</summary>
        public event Action MatchEnded;

        /// <summary>Abandons any match in progress and starts a new one from the initial position.</summary>
        public void StartNewMatch(GameSetup setup)
        {
            _setup = setup;

            CancelCurrentMatch();

            _game.Reset();
            _players[(int)PieceColor.White] = _playerFactory.Create(PieceColor.White, setup);
            _players[(int)PieceColor.Black] = _playerFactory.Create(PieceColor.Black, setup);

            _capturedByWhite.Clear();
            _capturedByBlack.Clear();

            _boardView.SetOrientation(
                setup.PerspectiveColor == PieceColor.White
                    ? BoardOrientation.WhiteAtBottom
                    : BoardOrientation.BlackAtBottom);

            _boardView.RenderPosition(_game.Position);
            _boardView.ClearAllHighlights();

            _hudView.Show();
            _hudView.ClearMoveList();
            _hudView.SetCapturedPieces(_capturedByWhite, _capturedByBlack);
            _hudView.SetStatus(_game.Status, _game.Result);

            _matchCancellation = new CancellationTokenSource();
            _ = RunMatchAsync(_matchCancellation.Token);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _hudView.RestartRequested -= OnRestartRequested;
            _hudView.UndoRequested -= OnUndoRequested;
            CancelCurrentMatch();
        }

        private async Task RunMatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!_game.IsGameOver && !cancellationToken.IsCancellationRequested)
                {
                    await PlayOneTurnAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The match was restarted or torn down; the next one has already been set up.
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            FinishMatch();
        }

        private async Task PlayOneTurnAsync(CancellationToken cancellationToken)
        {
            IPlayer player = _players[(int)_game.SideToMove];

            _hudView.SetActivePlayer(player.Color, player.DisplayName);
            _hudView.SetThinking(!player.IsHuman, player.DisplayName);
            _hudView.SetUndoAvailable(player.IsHuman && _game.CanUndo);

            Move move = await player.RequestMoveAsync(_game, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _hudView.SetThinking(false, player.DisplayName);

            if (!_game.TryMakeMove(move))
            {
                // A player that offers an illegal move is a bug, not a user error. Rejecting it
                // and asking again keeps the match alive rather than corrupting the position.
                _audio.PlayIllegalMove();
                return;
            }

            MoveRecord record = _game.History[_game.History.Count - 1];
            await ApplyMoveToViewsAsync(record, cancellationToken);
        }

        private async Task ApplyMoveToViewsAsync(MoveRecord record, CancellationToken cancellationToken)
        {
            _audio.PlayMove(record);
            RecordCapture(record);

            await _boardView.AnimateMoveAsync(record);
            cancellationToken.ThrowIfCancellationRequested();

            _boardView.ShowLastMove(record.Move.From, record.Move.To);
            RefreshCheckHighlight();

            _hudView.AppendMove(record);
            _hudView.SetCapturedPieces(_capturedByWhite, _capturedByBlack);
            _hudView.SetStatus(_game.Status, _game.Result);
        }

        private void FinishMatch()
        {
            _hudView.SetThinking(false, string.Empty);
            _hudView.SetStatus(_game.Status, _game.Result);
            _hudView.SetUndoAvailable(false);
            _audio.PlayGameEnd(_game.Result);

            MatchEnded?.Invoke();
        }

        private void OnRestartRequested() => StartNewMatch(_setup);

        /// <summary>
        /// Undo steps back a full move rather than a ply when an AI is playing, so that the human
        /// gets their own turn back instead of handing the position straight to the computer.
        /// </summary>
        private void OnUndoRequested()
        {
            CancelCurrentMatch();

            int pliesToUndo = _setup.Mode == GameMode.HumanVsAi ? 2 : 1;
            for (int i = 0; i < pliesToUndo && _game.CanUndo; i++)
            {
                _game.TryUndoLastMove();
            }

            RebuildViewsFromModel();

            _matchCancellation = new CancellationTokenSource();
            _ = RunMatchAsync(_matchCancellation.Token);
        }

        private void RebuildViewsFromModel()
        {
            RecomputeCapturedPieces();

            _boardView.RenderPosition(_game.Position);
            _boardView.ClearAllHighlights();

            if (_game.History.Count > 0)
            {
                Move lastMove = _game.History[_game.History.Count - 1].Move;
                _boardView.ShowLastMove(lastMove.From, lastMove.To);
            }

            RefreshCheckHighlight();

            _hudView.ClearMoveList();
            for (int i = 0; i < _game.History.Count; i++)
            {
                _hudView.AppendMove(_game.History[i]);
            }

            _hudView.SetCapturedPieces(_capturedByWhite, _capturedByBlack);
            _hudView.SetStatus(_game.Status, _game.Result);
        }

        private void RefreshCheckHighlight()
        {
            if (_game.Status == GameStatus.Check || _game.Status == GameStatus.Checkmate)
            {
                _boardView.ShowCheck(_game.Position.GetKingSquare(_game.SideToMove));
            }
        }

        private void RecordCapture(in MoveRecord record)
        {
            if (!record.IsCapture)
            {
                return;
            }

            List<Piece> captor = record.MovingColor == PieceColor.White ? _capturedByWhite : _capturedByBlack;
            captor.Add(record.CapturedPiece);
        }

        private void RecomputeCapturedPieces()
        {
            _capturedByWhite.Clear();
            _capturedByBlack.Clear();

            for (int i = 0; i < _game.History.Count; i++)
            {
                RecordCapture(_game.History[i]);
            }
        }

        private void CancelCurrentMatch()
        {
            if (_matchCancellation == null)
            {
                return;
            }

            _matchCancellation.Cancel();
            _matchCancellation.Dispose();
            _matchCancellation = null;
        }
    }
}
