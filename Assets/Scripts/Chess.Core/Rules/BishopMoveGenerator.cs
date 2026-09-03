using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    public sealed class BishopMoveGenerator : SlidingMoveGeneratorBase
    {
        public override PieceType PieceType => PieceType.Bishop;

        protected override Direction[] Rays => Directions.Diagonal;
    }
}
