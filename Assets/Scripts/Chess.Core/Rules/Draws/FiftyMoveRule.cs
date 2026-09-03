using Chess.Core.Board;
using Chess.Core.Game;

namespace Chess.Core.Rules.Draws
{
    /// <summary>
    /// A draw once one hundred plies (fifty moves by each side) pass with no capture and no pawn
    /// move, since without either the position cannot make progress.
    /// </summary>
    public sealed class FiftyMoveRule : IDrawRule
    {
        public const int PlyLimit = 100;

        public GameStatus Status => GameStatus.DrawByFiftyMoveRule;

        public bool IsDraw(IBoard board) => board.HalfMoveClock >= PlyLimit;
    }
}
