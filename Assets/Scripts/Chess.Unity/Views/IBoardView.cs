using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chess.Core.Board;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Unity.Views
{
    public enum BoardOrientation
    {
        WhiteAtBottom,
        BlackAtBottom
    }

    /// <summary>
    /// Renders the board and reports raw square clicks. Entirely passive: it holds no game state,
    /// makes no rules decisions, and never calls into the model.
    /// </summary>
    public interface IBoardView
    {
        /// <summary>Raised when the user clicks a square. The controller decides what it means.</summary>
        event Action<Square> SquareClicked;

        BoardOrientation Orientation { get; }

        void SetOrientation(BoardOrientation orientation);

        /// <summary>Rebuilds every piece from scratch. Used on start, reset and undo.</summary>
        void RenderPosition(IBoard board);

        /// <summary>Animates a single move and resolves once the animation has finished.</summary>
        Task AnimateMoveAsync(MoveRecord record);

        void ShowSelection(Square square);

        void ShowLegalMoves(IReadOnlyList<Move> moves);

        void ShowLastMove(Square from, Square to);

        void ShowCheck(Square kingSquare);

        void ClearSelectionAndLegalMoves();

        void ClearAllHighlights();

        /// <summary>Gates click reporting while the AI thinks or an animation plays.</summary>
        void SetInteractable(bool interactable);
    }
}
