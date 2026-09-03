using System;
using Chess.AI.Evaluation;

namespace Chess.AI
{
    /// <summary>
    /// A complete description of how strong an engine should be: how deep and how long it may
    /// search, and how much it understands about the positions it reaches.
    /// </summary>
    public readonly struct EngineProfile
    {
        public EngineProfile(EvaluationProfile evaluation, SearchLimits limits)
        {
            Evaluation = evaluation;
            Limits = limits;
        }

        public EvaluationProfile Evaluation { get; }

        public SearchLimits Limits { get; }

        public static EngineProfile ForDifficulty(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    return new EngineProfile(
                        EvaluationProfile.MaterialOnly,
                        new SearchLimits(2, TimeSpan.FromSeconds(0.5)));

                case AiDifficulty.Medium:
                    return new EngineProfile(
                        EvaluationProfile.MaterialAndPosition,
                        new SearchLimits(4, TimeSpan.FromSeconds(2)));

                case AiDifficulty.Hard:
                    return new EngineProfile(
                        EvaluationProfile.Tapered,
                        new SearchLimits(6, TimeSpan.FromSeconds(5)));

                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
        }
    }
}
