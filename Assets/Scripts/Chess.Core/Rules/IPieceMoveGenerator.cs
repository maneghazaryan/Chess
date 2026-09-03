using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Generates the pseudo-legal moves of a single piece type. Pseudo-legal means the move obeys
    /// the piece's movement rules but may leave its own king in check; filtering that out is the
    /// job of <see cref="ILegalityFilter"/>.
    /// </summary>
    /// <remarks>
    /// One implementation per piece type, looked up by <see cref="PieceType"/>. Supporting a new
    /// piece means adding a class and registering it, never editing an existing generator.
    /// </remarks>
    public interface IPieceMoveGenerator
    {
        PieceType PieceType { get; }

        void GeneratePseudoLegalMoves(IBoard board, Square from, MoveGenerationMode mode, MoveList moves);
    }
}
