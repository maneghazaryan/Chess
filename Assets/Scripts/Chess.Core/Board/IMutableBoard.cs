using Chess.Core.Primitives;

namespace Chess.Core.Board
{
    /// <summary>
    /// A position that can be advanced and rewound.
    /// </summary>
    /// <remarks>
    /// Search performance hinges on <see cref="MakeMove"/>/<see cref="UnmakeMove"/> being far
    /// cheaper than cloning the position at every node, so all state that a move destroys
    /// (captured piece, castling rights, en passant square, half-move clock, hash) is pushed onto
    /// an undo stack rather than recomputed.
    /// </remarks>
    public interface IMutableBoard : IBoard
    {
        /// <summary>Number of moves currently on the undo stack.</summary>
        int PlyCount { get; }

        /// <summary>The most recently made move, or <see cref="Move.None"/> at the root.</summary>
        Move LastMove { get; }

        void MakeMove(in Move move);

        /// <summary>Rewinds the most recent <see cref="MakeMove"/>. Throws when the stack is empty.</summary>
        void UnmakeMove();

        /// <summary>
        /// Advances the side to move without moving a piece. Used by null-move style search
        /// techniques and by "is this square defended if I do nothing" style queries.
        /// </summary>
        void MakeNullMove();

        void UnmakeNullMove();
    }
}
