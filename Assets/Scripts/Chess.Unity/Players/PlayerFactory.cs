using System;
using Chess.AI;
using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// Creates the two players a match needs.
    /// </summary>
    /// <remarks>
    /// This is the only place in the application that reads <see cref="GameMode"/>. Everything
    /// downstream sees two <see cref="IPlayer"/> instances and does not care which is which,
    /// which is what keeps the turn loop free of mode branching.
    /// </remarks>
    public sealed class PlayerFactory : IPlayerFactory
    {
        private readonly IHumanMoveSource _humanMoveSource;
        private readonly IChessEngineFactory _engineFactory;
        private readonly TimeSpan _minimumAiThinkTime;

        public PlayerFactory(
            IHumanMoveSource humanMoveSource,
            IChessEngineFactory engineFactory,
            TimeSpan minimumAiThinkTime)
        {
            _humanMoveSource = humanMoveSource ?? throw new ArgumentNullException(nameof(humanMoveSource));
            _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
            _minimumAiThinkTime = minimumAiThinkTime;
        }

        public IPlayer Create(PieceColor color, in GameSetup setup)
        {
            if (setup.IsHuman(color))
            {
                return new HumanPlayer(color, DescribeHuman(color, setup), _humanMoveSource);
            }

            return new AiPlayer(
                color,
                $"Computer ({setup.Difficulty})",
                _engineFactory.Create(setup.Difficulty),
                _engineFactory.GetLimits(setup.Difficulty),
                _minimumAiThinkTime);
        }

        private static string DescribeHuman(PieceColor color, in GameSetup setup)
        {
            return setup.Mode == GameMode.HumanVsHuman ? $"Player ({color})" : "You";
        }
    }
}
