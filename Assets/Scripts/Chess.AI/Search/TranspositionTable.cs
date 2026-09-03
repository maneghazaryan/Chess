using System;
using Chess.AI.Evaluation;
using Chess.Core.Primitives;

namespace Chess.AI.Search
{
    /// <summary>
    /// What kind of bound a stored score represents.
    /// </summary>
    public enum TranspositionNodeType : byte
    {
        /// <summary>No entry.</summary>
        None = 0,

        /// <summary>The search examined every move; the score is the true value of the position.</summary>
        Exact,

        /// <summary>A beta cutoff occurred, so the true value is at least the stored score.</summary>
        LowerBound,

        /// <summary>No move beat alpha, so the true value is at most the stored score.</summary>
        UpperBound
    }

    internal struct TranspositionEntry
    {
        public ulong Key;
        public Move BestMove;
        public int Score;
        public short Depth;
        public TranspositionNodeType NodeType;
    }

    /// <summary>
    /// Caches search results by Zobrist key so a position reached by a different move order does
    /// not have to be searched again.
    /// </summary>
    /// <remarks>
    /// Transpositions are extremely common in chess, so this both saves work directly and, just as
    /// importantly, supplies the hash move that drives move ordering on re-searches during
    /// iterative deepening.
    /// <para>
    /// Collisions are possible because the table is indexed by the low bits of the key. Storing
    /// the full key and comparing it makes a wrong hit require a full 64-bit collision, which is
    /// rare enough to ignore in a game engine of this size.
    /// </para>
    /// </remarks>
    public sealed class TranspositionTable
    {
        public const int DefaultSizeInEntries = 1 << 20;

        private readonly TranspositionEntry[] _entries;
        private readonly ulong _indexMask;

        public TranspositionTable(int sizeInEntries = DefaultSizeInEntries)
        {
            int rounded = RoundDownToPowerOfTwo(sizeInEntries);
            if (rounded < 1024)
            {
                rounded = 1024;
            }

            _entries = new TranspositionEntry[rounded];
            _indexMask = (ulong)(rounded - 1);
        }

        public int Capacity => _entries.Length;

        public void Clear() => Array.Clear(_entries, 0, _entries.Length);

        /// <summary>
        /// Looks up a position. Always reports the stored best move when the key matches, even
        /// when the entry is too shallow to cut off, because a move from a shallower search is
        /// still an excellent ordering hint.
        /// </summary>
        public bool TryProbe(
            ulong key,
            int depth,
            int ply,
            int alpha,
            int beta,
            out int score,
            out Move hashMove)
        {
            score = 0;
            hashMove = Move.None;

            ref TranspositionEntry entry = ref _entries[key & _indexMask];
            if (entry.Key != key || entry.NodeType == TranspositionNodeType.None)
            {
                return false;
            }

            hashMove = entry.BestMove;

            if (entry.Depth < depth)
            {
                return false;
            }

            int stored = FromTableScore(entry.Score, ply);

            switch (entry.NodeType)
            {
                case TranspositionNodeType.Exact:
                    score = stored;
                    return true;

                case TranspositionNodeType.LowerBound when stored >= beta:
                    score = stored;
                    return true;

                case TranspositionNodeType.UpperBound when stored <= alpha:
                    score = stored;
                    return true;

                default:
                    return false;
            }
        }

        public void Store(ulong key, int depth, int ply, int score, int originalAlpha, int beta, in Move bestMove)
        {
            ref TranspositionEntry entry = ref _entries[key & _indexMask];

            // Depth-preferred replacement: a deeper result is more valuable than a newer one, but
            // an entry for a different position is always replaced so the table cannot stagnate.
            if (entry.Key == key && entry.Depth > depth)
            {
                return;
            }

            TranspositionNodeType nodeType;
            if (score <= originalAlpha)
            {
                nodeType = TranspositionNodeType.UpperBound;
            }
            else if (score >= beta)
            {
                nodeType = TranspositionNodeType.LowerBound;
            }
            else
            {
                nodeType = TranspositionNodeType.Exact;
            }

            entry.Key = key;
            entry.BestMove = bestMove;
            entry.Score = ToTableScore(score, ply);
            entry.Depth = (short)depth;
            entry.NodeType = nodeType;
        }

        /// <summary>
        /// Mate scores encode distance from the node they were found at, but the table is shared
        /// across the whole tree. Converting to distance-from-mate on the way in and back to
        /// distance-from-root on the way out keeps a mate found at one depth from being reported
        /// as a different distance when the position is reached again elsewhere.
        /// </summary>
        private static int ToTableScore(int score, int ply)
        {
            if (score > EvaluationConstants.MateThreshold)
            {
                return score + ply;
            }

            if (score < -EvaluationConstants.MateThreshold)
            {
                return score - ply;
            }

            return score;
        }

        private static int FromTableScore(int score, int ply)
        {
            if (score > EvaluationConstants.MateThreshold)
            {
                return score - ply;
            }

            if (score < -EvaluationConstants.MateThreshold)
            {
                return score + ply;
            }

            return score;
        }

        private static int RoundDownToPowerOfTwo(int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            int result = 1;
            while (result <= value / 2)
            {
                result <<= 1;
            }

            return result;
        }
    }
}
