using Chess.Core.Board;
using Chess.Core.Game;

namespace Chess.Core.Rules.Draws
{
    /// <summary>
    /// A draw once the same position occurs three times with the same side to move, the same
    /// castling rights and the same en passant possibility.
    /// </summary>
    /// <remarks>
    /// All four of those conditions are folded into the Zobrist key, so comparing hashes is a
    /// faithful test rather than an approximation.
    /// </remarks>
    public sealed class ThreefoldRepetitionRule : IDrawRule
    {
        public const int RepetitionLimit = 3;

        public GameStatus Status => GameStatus.DrawByThreefoldRepetition;

        public bool IsDraw(IBoard board) => board.GetRepetitionCount() >= RepetitionLimit;
    }
}
