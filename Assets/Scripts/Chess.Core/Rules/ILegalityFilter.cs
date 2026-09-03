using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Removes pseudo-legal moves that would leave the mover's own king in check.
    /// </summary>
    public interface ILegalityFilter
    {
        /// <summary>Removes illegal moves from <paramref name="moves"/> in place.</summary>
        void FilterInPlace(IMutableBoard board, MoveList moves);

        bool IsLegal(IMutableBoard board, in Move move);
    }
}
