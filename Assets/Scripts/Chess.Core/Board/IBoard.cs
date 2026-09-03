using System.Collections.Generic;
using Chess.Core.Primitives;

namespace Chess.Core.Board
{
    /// <summary>
    /// A read-only view of a chess position. Evaluators, views and any other consumer that must
    /// not mutate the position depends on this rather than on <see cref="IMutableBoard"/>.
    /// </summary>
    public interface IBoard
    {
        Piece this[Square square] { get; }

        PieceColor SideToMove { get; }

        CastlingRights CastlingRights { get; }

        /// <summary>
        /// The square a pawn would land on when capturing en passant, or <see cref="Square.None"/>.
        /// Set only immediately after a double pawn push.
        /// </summary>
        Square EnPassantTarget { get; }

        /// <summary>Plies since the last capture or pawn move, for the fifty-move rule.</summary>
        int HalfMoveClock { get; }

        /// <summary>Starts at 1 and increments after each black move.</summary>
        int FullMoveNumber { get; }

        /// <summary>Incrementally maintained Zobrist hash of the current position.</summary>
        ulong ZobristKey { get; }

        Square GetKingSquare(PieceColor color);

        /// <summary>Squares occupied by the given colour. Order is unspecified and may change after any move.</summary>
        IReadOnlyList<Square> GetOccupiedSquares(PieceColor color);

        int CountPieces(PieceColor color, PieceType type);

        /// <summary>
        /// How many times the current position has already appeared in this game, including now.
        /// A value of three means a threefold repetition draw is claimable.
        /// </summary>
        int GetRepetitionCount();

        /// <summary>
        /// Produces an independent mutable copy. The AI uses this so a background search can never
        /// observe or disturb the position the UI is displaying.
        /// </summary>
        IMutableBoard CreateWorkingCopy();
    }
}
