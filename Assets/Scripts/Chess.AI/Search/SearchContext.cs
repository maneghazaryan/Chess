using System;
using System.Diagnostics;
using System.Threading;
using Chess.Core.Board;

namespace Chess.AI.Search
{
    /// <summary>
    /// The mutable state threaded through one search: the working board, the node counter, the
    /// buffers and the two ways a search can be stopped early.
    /// </summary>
    /// <remarks>
    /// Bundling these into one object keeps the recursive signatures down to the four arguments
    /// that actually vary per node (depth, ply, alpha, beta), which is what makes the negamax
    /// routine readable next to the textbook pseudocode.
    /// </remarks>
    public sealed class SearchContext
    {
        /// <summary>The clock is consulted only every so many nodes, since it is not free to read.</summary>
        private const int NodesBetweenTimeChecks = 2048;

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _nextTimeCheckAtNode;

        public SearchContext(IMutableBoard board, TimeSpan timeBudget, CancellationToken cancellationToken)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            TimeBudget = timeBudget;
            CancellationToken = cancellationToken;
            Pool = new MoveListPool();
            _nextTimeCheckAtNode = NodesBetweenTimeChecks;
        }

        public IMutableBoard Board { get; }

        public MoveListPool Pool { get; }

        public TimeSpan TimeBudget { get; }

        public CancellationToken CancellationToken { get; }

        public long Nodes { get; private set; }

        public TimeSpan Elapsed => _clock.Elapsed;

        /// <summary>
        /// Set once the search has run out of time or been cancelled. Every score produced after
        /// this point is meaningless and must be discarded rather than compared.
        /// </summary>
        public bool IsAborted { get; private set; }

        public void CountNode() => Nodes++;

        public bool ShouldAbort()
        {
            if (IsAborted)
            {
                return true;
            }

            if (Nodes < _nextTimeCheckAtNode)
            {
                return false;
            }

            _nextTimeCheckAtNode = Nodes + NodesBetweenTimeChecks;

            if (CancellationToken.IsCancellationRequested || _clock.Elapsed >= TimeBudget)
            {
                IsAborted = true;
            }

            return IsAborted;
        }
    }
}
