using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Answers "is this square under attack". Check detection, checkmate detection and the
    /// castling rules are all expressed in terms of this one question.
    /// </summary>
    public interface IAttackService
    {
        bool IsSquareAttacked(IBoard board, Square square, PieceColor byColor);

        bool IsInCheck(IBoard board, PieceColor color);
    }
}
