using Chess.Core.Board;

namespace Chess.Core.Game
{
    /// <summary>
    /// Classifies a position as in progress, check, checkmate, stalemate or one of the draws.
    /// Kept separate from <see cref="IChessGame"/> so the rules for ending a game can be tested
    /// and varied independently of game bookkeeping.
    /// </summary>
    public interface IGameStatusEvaluator
    {
        GameStatus Evaluate(IMutableBoard board);

        GameResult ToResult(GameStatus status, Primitives.PieceColor sideToMove);
    }
}
