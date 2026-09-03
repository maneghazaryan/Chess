using Chess.Core.Board;
using Chess.Core.Game;
using Chess.Core.Rules.Draws;

namespace Chess.AI.Search
{
    /// <summary>
    /// Recognises drawn positions inside the search.
    /// </summary>
    /// <remarks>
    /// This deliberately does not reuse the game's <see cref="ThreefoldRepetitionRule"/>. Over the
    /// board a draw needs three occurrences, but inside a search the <em>second</em> occurrence
    /// should already score as a draw: if a line has repeated once, either side can simply repeat
    /// it again, so treating it as drawn is what lets the engine both find perpetual checks and
    /// avoid shuffling into one when it is winning. The fifty-move and insufficient-material rules
    /// are shared with the game unchanged.
    /// </remarks>
    public sealed class SearchDrawDetector
    {
        private const int SearchRepetitionLimit = 2;

        private readonly InsufficientMaterialRule _insufficientMaterial = new InsufficientMaterialRule();

        public bool IsDraw(IBoard board)
        {
            return board.HalfMoveClock >= FiftyMoveRule.PlyLimit
                   || board.GetRepetitionCount() >= SearchRepetitionLimit
                   || _insufficientMaterial.IsDraw(board);
        }

        /// <summary>Kept public so callers can name the reason when reporting a search result.</summary>
        public GameStatus Classify(IBoard board)
        {
            if (board.HalfMoveClock >= FiftyMoveRule.PlyLimit)
            {
                return GameStatus.DrawByFiftyMoveRule;
            }

            if (board.GetRepetitionCount() >= SearchRepetitionLimit)
            {
                return GameStatus.DrawByThreefoldRepetition;
            }

            return _insufficientMaterial.IsDraw(board)
                ? GameStatus.DrawByInsufficientMaterial
                : GameStatus.InProgress;
        }
    }
}
