using System;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// Turns the event-driven world of UI clicks into the awaitable move the turn loop expects.
    /// </summary>
    /// <remarks>
    /// The adaptation is a <see cref="TaskCompletionSource{TResult}"/>: the task is handed to the
    /// caller immediately and completed later, when the click arrives. That is what allows a
    /// human and an AI to satisfy the same <see cref="IPlayer"/> contract despite one taking
    /// milliseconds of CPU and the other taking minutes of wall time.
    /// </remarks>
    public sealed class HumanPlayer : IPlayer
    {
        private readonly IHumanMoveSource _moveSource;

        public HumanPlayer(PieceColor color, string displayName, IHumanMoveSource moveSource)
        {
            Color = color;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? color.ToString() : displayName;
            _moveSource = moveSource ?? throw new ArgumentNullException(nameof(moveSource));
        }

        public PieceColor Color { get; }

        public string DisplayName { get; }

        public bool IsHuman => true;

        public async Task<Move> RequestMoveAsync(IChessGame game, CancellationToken cancellationToken)
        {
            // RunContinuationsAsynchronously keeps the continuation off the click handler's stack,
            // so the turn loop never resumes in the middle of Unity's input dispatch.
            var completion = new TaskCompletionSource<Move>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnMoveChosen(Move move) => completion.TrySetResult(move);

            _moveSource.MoveChosen += OnMoveChosen;
            int turnSession = _moveSource.BeginTurn(Color);

            try
            {
                using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
                {
                    return await completion.Task;
                }
            }
            finally
            {
                _moveSource.MoveChosen -= OnMoveChosen;
                _moveSource.EndTurn(turnSession);
            }
        }
    }
}
