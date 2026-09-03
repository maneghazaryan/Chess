using Chess.Core.Primitives;

namespace Chess.Core.Board
{
    /// <summary>
    /// The state a move destroys and that therefore cannot be reconstructed from the move alone.
    /// One of these is pushed per <see cref="IMutableBoard.MakeMove"/> so unmaking is exact.
    /// </summary>
    internal struct BoardStateUndo
    {
        public Move Move;
        public Piece CapturedPiece;
        public Square CapturedPieceSquare;
        public CastlingRights CastlingRights;
        public Square EnPassantTarget;
        public int HalfMoveClock;
        public ulong ZobristKey;
        public bool IsNullMove;
    }
}
