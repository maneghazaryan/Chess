using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Board;

namespace Chess.AI
{
    /// <summary>
    /// Chooses a move for the side to move.
    /// </summary>
    /// <remarks>
    /// The contract says nothing about minimax. A random-move engine, an opening-book engine or a
    /// networked engine all satisfy it, and the controller cannot tell the difference.
    /// </remarks>
    public interface IChessEngine
    {
        string Name { get; }

        /// <summary>
        /// Searches <paramref name="position"/> without mutating it; implementations take their
        /// own working copy. Safe to call from a background thread.
        /// </summary>
        Task<SearchResult> FindBestMoveAsync(IBoard position, SearchLimits limits, CancellationToken cancellationToken);
    }
}
