using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    public sealed class QueenMoveGenerator : SlidingMoveGeneratorBase
    {
        public override PieceType PieceType => PieceType.Queen;

        protected override Direction[] Rays => Directions.All;
    }
}
