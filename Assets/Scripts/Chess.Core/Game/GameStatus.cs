namespace Chess.Core.Game
{
    public enum GameStatus
    {
        InProgress,
        Check,
        Checkmate,
        Stalemate,
        DrawByFiftyMoveRule,
        DrawByThreefoldRepetition,
        DrawByInsufficientMaterial
    }

    public enum GameResult
    {
        Undecided,
        WhiteWins,
        BlackWins,
        Draw
    }

    public static class GameStatusExtensions
    {
        public static bool IsGameOver(this GameStatus status)
        {
            return status != GameStatus.InProgress && status != GameStatus.Check;
        }

        public static bool IsDraw(this GameStatus status)
        {
            switch (status)
            {
                case GameStatus.Stalemate:
                case GameStatus.DrawByFiftyMoveRule:
                case GameStatus.DrawByThreefoldRepetition:
                case GameStatus.DrawByInsufficientMaterial:
                    return true;
                default:
                    return false;
            }
        }
    }
}
