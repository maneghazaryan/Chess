using System;
using System.Collections.Generic;
using Chess.AI;
using Chess.AI.Evaluation;
using UnityEngine;

namespace Chess.Unity.Config
{
    /// <summary>
    /// Designer-tunable AI strength.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IChessEngineFactory"/> directly, so an asset can be dropped in
    /// wherever the code-default factory would go and the difficulty curve can be balanced by
    /// playing rather than by recompiling. Falls back to
    /// <see cref="EngineProfile.ForDifficulty"/> for any level left unconfigured.
    /// </remarks>
    [CreateAssetMenu(fileName = "AiDifficultyConfig", menuName = "Chess/AI Difficulty Config")]
    public sealed class AiDifficultyConfig : ScriptableObject, IChessEngineFactory
    {
        [Serializable]
        private struct DifficultyEntry
        {
            public AiDifficulty Difficulty;

            [Tooltip("Hard ceiling on iterative deepening. Each extra ply costs roughly six times the nodes.")]
            [Min(1)] public int MaxSearchDepth;

            [Tooltip("Wall-clock budget. Whichever of depth or time runs out first stops the search.")]
            [Min(0.05f)] public float MaxSearchSeconds;

            [Tooltip("How much the engine understands beyond raw material.")]
            public EvaluationProfile Evaluation;
        }

        [SerializeField]
        private List<DifficultyEntry> _entries = new List<DifficultyEntry>
        {
            new DifficultyEntry
            {
                Difficulty = AiDifficulty.Easy,
                MaxSearchDepth = 2,
                MaxSearchSeconds = 0.5f,
                Evaluation = EvaluationProfile.MaterialOnly
            },
            new DifficultyEntry
            {
                Difficulty = AiDifficulty.Medium,
                MaxSearchDepth = 4,
                MaxSearchSeconds = 2f,
                Evaluation = EvaluationProfile.MaterialAndPosition
            },
            new DifficultyEntry
            {
                Difficulty = AiDifficulty.Hard,
                MaxSearchDepth = 6,
                MaxSearchSeconds = 5f,
                Evaluation = EvaluationProfile.Tapered
            }
        };

        [Tooltip("Shortest pause before the computer replies, so easy moves do not appear instantly.")]
        [SerializeField, Min(0f)] private float _minimumThinkSeconds = 0.35f;

        private ChessEngineFactory _factory;

        public TimeSpan MinimumThinkTime => TimeSpan.FromSeconds(_minimumThinkSeconds);

        public IChessEngine Create(AiDifficulty difficulty) => Factory.Create(difficulty);

        public SearchLimits GetLimits(AiDifficulty difficulty) => Factory.GetLimits(difficulty);

        private ChessEngineFactory Factory => _factory ??= new ChessEngineFactory(ResolveProfile);

        private EngineProfile ResolveProfile(AiDifficulty difficulty)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Difficulty != difficulty)
                {
                    continue;
                }

                DifficultyEntry entry = _entries[i];
                return new EngineProfile(
                    entry.Evaluation,
                    new SearchLimits(entry.MaxSearchDepth, TimeSpan.FromSeconds(entry.MaxSearchSeconds)));
            }

            return EngineProfile.ForDifficulty(difficulty);
        }

        private void OnDisable()
        {
            // Assets survive play mode, so the cached factory (and the transposition table inside
            // the engines it built) must not leak from one session into the next.
            _factory = null;
        }
    }
}
