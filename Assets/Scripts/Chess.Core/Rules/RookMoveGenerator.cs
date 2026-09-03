using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    public sealed class RookMoveGenerator : SlidingMoveGeneratorBase
    {
        public override PieceType PieceType => PieceType.Rook;

        protected override Direction[] Rays => Directions.Orthogonal;
    }
}
