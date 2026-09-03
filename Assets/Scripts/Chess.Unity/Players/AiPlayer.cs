using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Chess.AI;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// Runs the engine on a background thread and reports its move.
    /// </summary>
    /// <remarks>
    /// Searching on the main thread would freeze rendering and input for the whole think time, so
    /// <see cref="IChessEngine.FindBestMoveAsync"/> is expected to move the work off it. The
    /// engine takes its own copy of the position before doing so, which is what makes it safe for
    /// the UI to keep reading the live board while the search runs.
    /// </remarks>
    public sealed class AiPlayer : IPlayer
    {
        private readonly IChessEngine _engine;
        private readonly SearchLimits _limits;
        private readonly TimeSpan _minimumThinkTime;

        public AiPlayer(
            PieceColor color,
            string displayName,
            IChessEngine engine,
            SearchLimits limits,
            TimeSpan minimumThinkTime)
        {
            Color = color;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Computer ({color})" : displayName;
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _limits = limits;
            _minimumThinkTime = minimumThinkTime;
        }

        public PieceColor Color { get; }

        public string DisplayName { get; }

        public bool IsHuman => false;

        public async Task<Move> RequestMoveAsync(IChessGame game, CancellationToken cancellationToken)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            var clock = Stopwatch.StartNew();
            SearchResult result = await _engine.FindBestMoveAsync(game.Position, _limits, cancellationToken);
            clock.Stop();

            // An instant reply in a simple position reads as a glitch rather than as strength, so
            // shallow searches are padded out to a human-looking pause.
            TimeSpan remaining = _minimumThinkTime - clock.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }

            return result.BestMove;
        }
    }
}
