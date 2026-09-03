using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Game;
using Chess.Core.Primitives;
using Chess.Unity.Players;
using Chess.Unity.Views;

namespace Chess.Unity.Controllers
{
    /// <summary>
    /// Interprets square clicks as select-then-move, and resolves promotions.
    /// </summary>
    /// <remarks>
    /// Sits between a view that only knows "the user clicked e4" and a model that only accepts
    /// complete legal moves. All the selection state lives here rather than in the view, so the
    /// view stays a pure renderer and this logic stays testable against fakes.
    /// </remarks>
    public sealed class BoardInteractionController : IHumanMoveSource, IDisposable
    {
        private readonly IChessGame _game;
        private readonly IBoardView _boardView;
        private readonly IPromotionView _promotionView;

        private CancellationTokenSource _promotionCancellation;
        private Square _selectedSquare = Square.None;
        private PieceColor _activeColor;
        private bool _isAcceptingInput;
        private bool _isAwaitingPromotion;

        public BoardInteractionController(IChessGame game, IBoardView boardView, IPromotionView promotionView)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
            _promotionView = promotionView ?? throw new ArgumentNullException(nameof(promotionView));

            _boardView.SquareClicked += OnSquareClicked;
        }

        public event Action<Move> MoveChosen;

        public void BeginTurn(PieceColor color)
        {
            _activeColor = color;
            _isAcceptingInput = true;
            _boardView.SetInteractable(true);
        }

        public void EndTurn()
        {
            _isAcceptingInput = false;
            _boardView.SetInteractable(false);
            CancelPendingPromotion();
            ClearSelection();
        }

        public void Dispose()
        {
            _boardView.SquareClicked -= OnSquareClicked;
            CancelPendingPromotion();
        }

        // Async void is the correct shape for an event handler that must await the promotion
        // dialog. Everything it can throw is caught below so no exception escapes into Unity's
        // event dispatch, where it would be unobservable.
        private async void OnSquareClicked(Square square)
        {
            if (!_isAcceptingInput || _isAwaitingPromotion || !square.IsValid)
            {
                return;
            }

            try
            {
                await HandleClickAsync(square);
            }
            catch (OperationCanceledException)
            {
                // The turn ended while the promotion dialog was open; nothing to report.
            }
        }

        private async Task HandleClickAsync(Square square)
        {
            if (_selectedSquare.IsValid)
            {
                if (square == _selectedSquare)
                {
                    ClearSelection();
                    return;
                }

                if (_game.TryFindMove(_selectedSquare, square, PieceType.None, out Move move))
                {
                    await CommitMoveAsync(move);
                    return;
                }
            }

            // Not a destination, so treat it as picking up a piece. Clicking an empty square or an
            // opponent's piece simply clears the selection.
            if (_game.Position[square].Is(_activeColor))
            {
                Select(square);
            }
            else
            {
                ClearSelection();
            }
        }

        private async Task CommitMoveAsync(Move move)
        {
            if (move.IsPromotion)
            {
                PieceType chosen = await AskForPromotionPieceAsync();
                if (chosen == PieceType.None)
                {
                    ClearSelection();
                    return;
                }

                // Re-resolve rather than patching the flags, so the move that reaches the model is
                // always one the generator actually produced.
                if (!_game.TryFindMove(move.From, move.To, chosen, out move))
                {
                    ClearSelection();
                    return;
                }
            }

            ClearSelection();
            MoveChosen?.Invoke(move);
        }

        private async Task<PieceType> AskForPromotionPieceAsync()
        {
            _isAwaitingPromotion = true;
            _promotionCancellation = new CancellationTokenSource();

            try
            {
                return await _promotionView.RequestPromotionAsync(_activeColor, _promotionCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return PieceType.None;
            }
            finally
            {
                _isAwaitingPromotion = false;
                _promotionCancellation?.Dispose();
                _promotionCancellation = null;
            }
        }

        private void Select(Square square)
        {
            IReadOnlyList<Move> legalMoves = _game.GetLegalMovesFrom(square);
            if (legalMoves.Count == 0)
            {
                ClearSelection();
                return;
            }

            _selectedSquare = square;
            _boardView.ShowSelection(square);
            _boardView.ShowLegalMoves(legalMoves);
        }

        private void ClearSelection()
        {
            _selectedSquare = Square.None;
            _boardView.ClearSelectionAndLegalMoves();
        }

        private void CancelPendingPromotion()
        {
            if (_promotionCancellation == null)
            {
                return;
            }

            _promotionCancellation.Cancel();
            _promotionView.Hide();
        }
    }
}
