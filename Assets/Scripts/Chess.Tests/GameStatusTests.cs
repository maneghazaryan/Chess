using Chess.Core.Game;
using Chess.Core.Primitives;
using NUnit.Framework;

namespace Chess.Tests
{
    /// <summary>
    /// How and when a game ends: checkmate, stalemate and each of the automatic draws.
    /// </summary>
    [TestFixture]
    public sealed class GameStatusTests
    {
        private static ChessGame GameFrom(string fen)
        {
            ChessGame game = ChessGame.CreateStandard();
            game.LoadFen(fen);
            return game;
        }

        private static bool Play(ChessGame game, string from, string to)
        {
            Square.TryParse(from, out Square fromSquare);
            Square.TryParse(to, out Square toSquare);

            return game.TryFindMove(fromSquare, toSquare, PieceType.None, out Move move)
                   && game.TryMakeMove(move);
        }

        [Test]
        public void BackRankMate_IsReportedAsCheckmate()
        {
            ChessGame game = GameFrom("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");

            Assert.That(Play(game, "a1", "a8"), Is.True);

            Assert.That(game.Status, Is.EqualTo(GameStatus.Checkmate));
            Assert.That(game.Result, Is.EqualTo(GameResult.WhiteWins));
            Assert.That(game.IsGameOver, Is.True);
        }

        [Test]
        public void Checkmate_IsRecordedWithAHashInNotation()
        {
            ChessGame game = GameFrom("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");

            Play(game, "a1", "a8");

            Assert.That(game.History[0].StandardNotation, Is.EqualTo("Ra8#"));
        }

        [Test]
        public void Check_IsRecordedWithAPlusInNotation()
        {
            ChessGame game = GameFrom("4k3/8/8/8/8/8/8/R3K3 w - - 0 1");

            Play(game, "a1", "a8");

            Assert.That(game.Status, Is.EqualTo(GameStatus.Check));
            Assert.That(game.IsGameOver, Is.False, "check is not an ending");
            Assert.That(game.History[0].StandardNotation, Is.EqualTo("Ra8+"));
        }

        [Test]
        public void Stalemate_IsADrawRatherThanALoss()
        {
            // Black's king on a8 has no legal move and is not in check.
            ChessGame game = GameFrom("k7/8/1Q6/8/8/8/8/K7 b - - 0 1");

            Assert.That(game.Status, Is.EqualTo(GameStatus.Stalemate));
            Assert.That(game.Result, Is.EqualTo(GameResult.Draw));
        }

        [Test]
        public void FiftyMoveRule_DrawsAtOneHundredReversiblePlies()
        {
            // The half-move clock is loaded at 99, so one more quiet move reaches the limit.
            ChessGame game = GameFrom("4k3/8/8/8/8/8/R7/4K2R w - - 99 60");

            Assert.That(game.Status, Is.EqualTo(GameStatus.InProgress));

            Assert.That(Play(game, "h1", "h2"), Is.True);

            Assert.That(game.Status, Is.EqualTo(GameStatus.DrawByFiftyMoveRule));
            Assert.That(game.Result, Is.EqualTo(GameResult.Draw));
        }

        [Test]
        public void FiftyMoveRule_ClockResetsOnAPawnMove()
        {
            ChessGame game = GameFrom("4k3/8/8/8/8/8/P7/4K3 w - - 99 60");

            Assert.That(Play(game, "a2", "a3"), Is.True);

            Assert.That(game.Position.HalfMoveClock, Is.EqualTo(0));
            Assert.That(game.Status, Is.EqualTo(GameStatus.InProgress));
        }

        [Test]
        public void ThreefoldRepetition_DrawsOnTheThirdOccurrence()
        {
            // Rooks shuffle back and forth, reaching the same position three times. Castling
            // rights are already gone, because a rook leaving h1 would revoke them and so change
            // the position's identity, making the first return a different position rather than a
            // repetition.
            ChessGame game = GameFrom("4k2r/8/8/8/8/8/8/4K2R w - - 0 1");

            Assert.That(Play(game, "h1", "g1"), Is.True);
            Assert.That(Play(game, "h8", "g8"), Is.True);
            Assert.That(Play(game, "g1", "h1"), Is.True);
            Assert.That(Play(game, "g8", "h8"), Is.True);

            Assert.That(game.Status, Is.EqualTo(GameStatus.InProgress),
                "twice is not yet a repetition draw");

            Assert.That(Play(game, "h1", "g1"), Is.True);
            Assert.That(Play(game, "h8", "g8"), Is.True);
            Assert.That(Play(game, "g1", "h1"), Is.True);
            Assert.That(Play(game, "g8", "h8"), Is.True);

            Assert.That(game.Status, Is.EqualTo(GameStatus.DrawByThreefoldRepetition));
            Assert.That(game.Result, Is.EqualTo(GameResult.Draw));
        }

        [TestCase("4k3/8/8/8/8/8/8/4K3 w - - 0 1", true, "bare kings")]
        [TestCase("4k3/8/8/8/8/8/8/2B1K3 w - - 0 1", true, "king and bishop against king")]
        [TestCase("4k3/8/8/8/8/8/8/2N1K3 w - - 0 1", true, "king and knight against king")]
        [TestCase("4kb2/8/8/8/8/8/8/2B1K3 w - - 0 1", true, "bishops both on dark squares (c1 and f8)")]
        [TestCase("2b1k3/8/8/8/8/8/8/2B1K3 w - - 0 1", false, "bishops on opposite colours (c1 dark, c8 light)")]
        [TestCase("4k3/8/8/8/8/8/8/R3K3 w - - 0 1", false, "a rook can mate")]
        [TestCase("4k3/7p/8/8/8/8/8/4K3 w - - 0 1", false, "a pawn can promote")]
        [TestCase("4k3/8/8/8/8/8/8/1NN1K3 w - - 0 1", false, "two knights can mate, if not force it")]
        public void InsufficientMaterial_MatchesTheFideList(string fen, bool expectedDraw, string reason)
        {
            ChessGame game = GameFrom(fen);

            bool isInsufficientMaterialDraw = game.Status == GameStatus.DrawByInsufficientMaterial;

            Assert.That(isInsufficientMaterialDraw, Is.EqualTo(expectedDraw), reason);
        }

        [Test]
        public void BishopColour_DecidesWhetherBishopVersusBishopIsADraw()
        {
            // Two bishops that can never contest the same square cannot combine to mate, so FIDE
            // calls it a draw. One bishop of each colour still leaves mate theoretically possible.
            ChessGame sameColour = GameFrom("4kb2/8/8/8/8/8/8/2B1K3 w - - 0 1");
            ChessGame oppositeColour = GameFrom("2b1k3/8/8/8/8/8/8/2B1K3 w - - 0 1");

            Assert.That(sameColour.Status, Is.EqualTo(GameStatus.DrawByInsufficientMaterial));
            Assert.That(oppositeColour.Status, Is.Not.EqualTo(GameStatus.DrawByInsufficientMaterial));
        }

        [Test]
        public void CheckmateOnTheFiftiethMove_CountsAsAWinNotADraw()
        {
            ChessGame game = GameFrom("6k1/5ppp/8/8/8/8/8/R3K3 w - - 99 60");

            Assert.That(Play(game, "a1", "a8"), Is.True);

            Assert.That(game.Status, Is.EqualTo(GameStatus.Checkmate),
                "mate must be resolved before the fifty-move rule");
        }

        [Test]
        public void GameOver_RefusesFurtherMoves()
        {
            ChessGame game = GameFrom("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");
            Play(game, "a1", "a8");

            Square.TryParse("f7", out Square from);
            Square.TryParse("f6", out Square to);

            Assert.That(game.TryFindMove(from, to, PieceType.None, out Move move), Is.False,
                "a mated side has no legal moves at all");
            Assert.That(game.TryMakeMove(move), Is.False);
        }
    }
}
