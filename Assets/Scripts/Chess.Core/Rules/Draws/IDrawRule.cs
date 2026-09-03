using Chess.Core.Board;
using Chess.Core.Game;

namespace Chess.Core.Rules.Draws
{
    /// <summary>
    /// One automatic drawing condition. Rules are registered as a collection and evaluated in
    /// order, so adding a new drawing condition never touches the status evaluator.
    /// </summary>
    public interface IDrawRule
    {
        /// <summary>The status to report when <see cref="IsDraw"/> holds.</summary>
        GameStatus Status { get; }

        bool IsDraw(IBoard board);
    }
}
