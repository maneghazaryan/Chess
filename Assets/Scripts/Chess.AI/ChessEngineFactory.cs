using System;
using Chess.AI.Evaluation;
using Chess.AI.Search;

namespace Chess.AI
{
    /// <summary>
    /// Builds an engine for a difficulty level.
    /// </summary>
    /// <remarks>
    /// Strength is varied along two axes at once: how deep the search looks and how much the
    /// evaluation understands. Depth alone gives an opponent that is either trivially beatable or
    /// slow; pairing a shallow search with a material-only evaluation gives an easy opponent that
    /// also moves quickly and makes the kind of positional mistakes a beginner can spot.
    /// <para>
    /// Where the profiles come from is injectable, so the defaults in <see cref="EngineProfile"/>
    /// can be replaced by a tuning asset without this class or its callers changing.
    /// </para>
    /// </remarks>
    public sealed class ChessEngineFactory : IChessEngineFactory
    {
        private readonly Func<AiDifficulty, EngineProfile> _profileProvider;

        public ChessEngineFactory()
            : this(EngineProfile.ForDifficulty)
        {
        }

        public ChessEngineFactory(Func<AiDifficulty, EngineProfile> profileProvider)
        {
            _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
        }

        public IChessEngine Create(AiDifficulty difficulty)
        {
            EngineProfile profile = _profileProvider(difficulty);
            return AlphaBetaSearch.CreateStandard(EvaluatorFactory.Create(profile.Evaluation));
        }

        public SearchLimits GetLimits(AiDifficulty difficulty) => _profileProvider(difficulty).Limits;
    }
}
