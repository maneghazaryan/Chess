using System;
using Chess.Core.Primitives;

namespace Chess.AI
{
    public readonly struct SearchResult
    {
        public SearchResult(Move bestMove, int score, int depthReached, long nodesSearched, TimeSpan elapsed)
        {
            BestMove = bestMove;
            Score = score;
            DepthReached = depthReached;
            NodesSearched = nodesSearched;
            Elapsed = elapsed;
        }

        public Move BestMove { get; }

        /// <summary>Centipawns from the searching side's point of view. Positive means good for it.</summary>
        public int Score { get; }

        public int DepthReached { get; }

        public long NodesSearched { get; }

        public TimeSpan Elapsed { get; }

        public bool HasMove => BestMove.IsValid;

        public long NodesPerSecond =>
            Elapsed.TotalSeconds > 0 ? (long)(NodesSearched / Elapsed.TotalSeconds) : 0;

        public override string ToString() =>
            $"{BestMove.ToUci()} score={Score} depth={DepthReached} nodes={NodesSearched} in {Elapsed.TotalMilliseconds:F0}ms";
    }
}
