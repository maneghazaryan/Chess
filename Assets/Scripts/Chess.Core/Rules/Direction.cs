namespace Chess.Core.Rules
{
    /// <summary>A single step expressed in files and ranks.</summary>
    public readonly struct Direction
    {
        public Direction(int fileDelta, int rankDelta)
        {
            FileDelta = fileDelta;
            RankDelta = rankDelta;
        }

        public int FileDelta { get; }

        public int RankDelta { get; }
    }

    /// <summary>
    /// The movement vectors of each piece. Expressing them as file/rank pairs and letting
    /// <see cref="Primitives.Square.Offset"/> reject off-board results avoids the wrap-around bugs
    /// that plague raw index arithmetic on a flat array.
    /// </summary>
    public static class Directions
    {
        public static readonly Direction[] Orthogonal =
        {
            new Direction(1, 0),
            new Direction(-1, 0),
            new Direction(0, 1),
            new Direction(0, -1)
        };

        public static readonly Direction[] Diagonal =
        {
            new Direction(1, 1),
            new Direction(1, -1),
            new Direction(-1, 1),
            new Direction(-1, -1)
        };

        /// <summary>All eight compass directions, shared by the queen and the king.</summary>
        public static readonly Direction[] All =
        {
            new Direction(1, 0),
            new Direction(-1, 0),
            new Direction(0, 1),
            new Direction(0, -1),
            new Direction(1, 1),
            new Direction(1, -1),
            new Direction(-1, 1),
            new Direction(-1, -1)
        };

        public static readonly Direction[] Knight =
        {
            new Direction(1, 2),
            new Direction(2, 1),
            new Direction(2, -1),
            new Direction(1, -2),
            new Direction(-1, -2),
            new Direction(-2, -1),
            new Direction(-2, 1),
            new Direction(-1, 2)
        };
    }
}
