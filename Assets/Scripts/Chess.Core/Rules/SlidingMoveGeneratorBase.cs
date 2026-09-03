using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Shared behaviour for pieces that slide along rays until they hit something: the bishop, the
    /// rook and the queen. Subclasses differ only in which rays they use.
    /// </summary>
    public abstract class SlidingMoveGeneratorBase : IPieceMoveGenerator
    {
        public abstract PieceType PieceType { get; }

        protected abstract Direction[] Rays { get; }

        public void GeneratePseudoLegalMoves(IBoard board, Square from, MoveGenerationMode mode, MoveList moves)
        {
            PieceColor mover = board[from].Color;
            Direction[] rays = Rays;

            for (int i = 0; i < rays.Length; i++)
            {
                Direction ray = rays[i];
                Square target = from.Offset(ray.FileDelta, ray.RankDelta);

                while (target.IsValid)
                {
                    Piece occupant = board[target];

                    if (occupant.IsSome)
                    {
                        if (!occupant.Is(mover))
                        {
                            moves.Add(new Move(from, target, MoveFlags.Capture));
                        }

                        break;
                    }

                    if (mode == MoveGenerationMode.All)
                    {
                        moves.Add(new Move(from, target, MoveFlags.Quiet));
                    }

                    target = target.Offset(ray.FileDelta, ray.RankDelta);
                }
            }
        }
    }
}
