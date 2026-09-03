namespace Chess.AI
{
    /// <summary>
    /// Builds a configured engine for a difficulty level.
    /// </summary>
    public interface IChessEngineFactory
    {
        IChessEngine Create(AiDifficulty difficulty);

        SearchLimits GetLimits(AiDifficulty difficulty);
    }
}
