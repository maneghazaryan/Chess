using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    public enum MoveGenerationMode
    {
        /// <summary>Every legal move.</summary>
        All,

        /// <summary>Only moves that change material: captures (including en passant) and promotions.</summary>
        TacticalOnly
    }

    /// <summary>
    /// Produces the legal moves of a position. Implementations must return strictly legal moves,
    /// never merely pseudo-legal ones, so that callers never have to re-check king safety.
    /// </summary>
    public interface IMoveGenerator
    {
        /// <summary>Appends the legal moves for the side to move into <paramref name="moves"/>, which is cleared first.</summary>
        void GenerateLegalMoves(IMutableBoard board, MoveList moves, MoveGenerationMode mode = MoveGenerationMode.All);

        /// <summary>Appends the legal moves originating from <paramref name="from"/>. The list is cleared first.</summary>
        void GenerateLegalMovesFrom(IMutableBoard board, Square from, MoveList moves);

        /// <summary>True when the side to move has no legal move, which means checkmate or stalemate.</summary>
        bool HasAnyLegalMove(IMutableBoard board);
    }
}
