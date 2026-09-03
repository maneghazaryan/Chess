using System;

namespace Chess.AI
{
    /// <summary>
    /// The budget a search must respect. Iterative deepening means either limit can stop the
    /// search at any point and still leave a usable best move from the last completed depth.
    /// </summary>
    public readonly struct SearchLimits
    {
        public const int MaxSupportedDepth = 64;

        public SearchLimits(int maxDepth, TimeSpan maxTime)
        {
            MaxDepth = Math.Clamp(maxDepth, 1, MaxSupportedDepth);
            MaxTime = maxTime <= TimeSpan.Zero ? TimeSpan.MaxValue : maxTime;
        }

        public int MaxDepth { get; }

        public TimeSpan MaxTime { get; }

        public static SearchLimits Depth(int depth) => new SearchLimits(depth, TimeSpan.MaxValue);

        public static SearchLimits Time(TimeSpan time) => new SearchLimits(MaxSupportedDepth, time);

        public static readonly SearchLimits Default =
            new SearchLimits(4, TimeSpan.FromSeconds(2));
    }
}
