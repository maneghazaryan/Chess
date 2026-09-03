using Chess.AI;
using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// The choices made in the main menu, handed to the player factory to build a match.
    /// </summary>
    public readonly struct GameSetup
    {
        public GameSetup(GameMode mode, AiDifficulty difficulty, PieceColor humanColor)
        {
            Mode = mode;
            Difficulty = difficulty;
            HumanColor = humanColor;
        }

        public GameMode Mode { get; }

        public AiDifficulty Difficulty { get; }

        /// <summary>Which side the human takes in <see cref="GameMode.HumanVsAi"/>. Ignored otherwise.</summary>
        public PieceColor HumanColor { get; }

        /// <summary>Which way up the board should be drawn, given who is playing.</summary>
        public PieceColor PerspectiveColor =>
            Mode == GameMode.HumanVsAi ? HumanColor : PieceColor.White;

        public bool IsHuman(PieceColor color)
        {
            switch (Mode)
            {
                case GameMode.HumanVsHuman: return true;
                case GameMode.AiVsAi: return false;
                default: return color == HumanColor;
            }
        }

        public static readonly GameSetup Default =
            new GameSetup(GameMode.HumanVsHuman, AiDifficulty.Medium, PieceColor.White);
    }
}
