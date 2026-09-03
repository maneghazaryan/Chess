using Chess.Core.Primitives;

namespace Chess.Core.Game
{
    /// <summary>
    /// Everything the presentation layer needs to describe a move that was played, captured at the
    /// moment it was made. Views and the move list read this instead of re-deriving context from
    /// the board, which by then has already moved on.
    /// </summary>
    public readonly struct MoveRecord
    {
        public MoveRecord(
            Move move,
            Piece movingPiece,
            Piece capturedPiece,
            Square capturedPieceSquare,
            string standardNotation,
            GameStatus resultingStatus,
            int fullMoveNumber)
        {
            Move = move;
            MovingPiece = movingPiece;
            CapturedPiece = capturedPiece;
            CapturedPieceSquare = capturedPieceSquare;
            StandardNotation = standardNotation;
            ResultingStatus = resultingStatus;
            FullMoveNumber = fullMoveNumber;
        }

        public Move Move { get; }

        public Piece MovingPiece { get; }

        /// <summary><see cref="Piece.None"/> when the move was not a capture.</summary>
        public Piece CapturedPiece { get; }

        /// <summary>
        /// Where the captured piece stood. This differs from <see cref="Primitives.Move.To"/> for
        /// en passant, which is exactly why the view needs it to know which sprite to remove.
        /// </summary>
        public Square CapturedPieceSquare { get; }

        /// <summary>SAN, for example "Nf3", "exd5", "O-O" or "e8=Q+".</summary>
        public string StandardNotation { get; }

        public GameStatus ResultingStatus { get; }

        public int FullMoveNumber { get; }

        public PieceColor MovingColor => MovingPiece.Color;

        public bool IsCapture => CapturedPiece.IsSome;

        public override string ToString() => StandardNotation ?? Move.ToUci();
    }
}
