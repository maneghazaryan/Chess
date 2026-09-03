using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    public sealed class KnightMoveGenerator : StepMoveGeneratorBase
    {
        public override PieceType PieceType => PieceType.Knight;

        protected override Direction[] Steps => Directions.Knight;
    }
}
