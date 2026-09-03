using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Shared behaviour for pieces that move a fixed set of single steps: the knight and the king.
    /// </summary>
    public abstract class StepMoveGeneratorBase : IPieceMoveGenerator
    {
        public abstract PieceType PieceType { get; }

        protected abstract Direction[] Steps { get; }

        public virtual void GeneratePseudoLegalMoves(IBoard board, Square from, MoveGenerationMode mode, MoveList moves)
        {
            PieceColor mover = board[from].Color;
            Direction[] steps = Steps;

            for (int i = 0; i < steps.Length; i++)
            {
                Square target = from.Offset(steps[i].FileDelta, steps[i].RankDelta);
                if (!target.IsValid)
                {
                    continue;
                }

                Piece occupant = board[target];
                if (occupant.Is(mover))
                {
                    continue;
                }

                if (occupant.IsSome)
                {
                    moves.Add(new Move(from, target, MoveFlags.Capture));
                }
                else if (mode == MoveGenerationMode.All)
                {
                    moves.Add(new Move(from, target, MoveFlags.Quiet));
                }
            }
        }
    }
}
