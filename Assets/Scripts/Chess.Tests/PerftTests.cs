using Chess.Core.Notation;
using NUnit.Framework;

namespace Chess.Tests
{
    /// <summary>
    /// Move generation checked against published node counts.
    /// </summary>
    /// <remarks>
    /// These six positions are the standard suite from the Chess Programming Wiki, chosen because
    /// between them they exercise every awkward rule: position two ("Kiwipete") is dense with
    /// castling and pins, position three is built around en passant and rook endgame edges,
    /// position four is full of promotions, and positions five and six catch bugs the others miss.
    /// <para>
    /// Depths are capped so the whole suite stays fast enough to run on every change. The
    /// <c>Explicit</c> deep cases exist for when move generation is actually being modified.
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class PerftTests
    {
        private const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        private const string EnPassantHeavy = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        private const string PromotionHeavy = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        private const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        private const string Position6 = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";

        [TestCase(1, 20L)]
        [TestCase(2, 400L)]
        [TestCase(3, 8_902L)]
        [TestCase(4, 197_281L)]
        public void StartPosition_MatchesPublishedNodeCounts(int depth, long expected)
        {
            AssertPerft(FenSerializer.StartPosition, depth, expected);
        }

        [TestCase(1, 48L)]
        [TestCase(2, 2_039L)]
        [TestCase(3, 97_862L)]
        public void Kiwipete_MatchesPublishedNodeCounts(int depth, long expected)
        {
            AssertPerft(Kiwipete, depth, expected);
        }

        [TestCase(1, 14L)]
        [TestCase(2, 191L)]
        [TestCase(3, 2_812L)]
        [TestCase(4, 43_238L)]
        public void EnPassantHeavyPosition_MatchesPublishedNodeCounts(int depth, long expected)
        {
            AssertPerft(EnPassantHeavy, depth, expected);
        }

        [TestCase(1, 6L)]
        [TestCase(2, 264L)]
        [TestCase(3, 9_467L)]
        public void PromotionHeavyPosition_MatchesPublishedNodeCounts(int depth, long expected)
        {
            AssertPerft(PromotionHeavy, depth, expected);
        }

        [TestCase(1, 44L)]
        [TestCase(2, 1_486L)]
        [TestCase(3, 62_379L)]
        public void Position5_MatchesPublishedNodeCounts(int depth, long expected)
        {
            AssertPerft(Position5, depth, expected);
        }

        [TestCase(1, 46L)]
        [TestCase(2, 2_079L)]
        [TestCase(3, 89_890L)]
        public void Position6_MatchesPublishedNodeCounts(int depth, long expected)
        {
            AssertPerft(Position6, depth, expected);
        }

        [Explicit("Takes several seconds. Run when move generation changes.")]
        [TestCase(FenSerializer.StartPosition, 5, 4_865_609L)]
        [TestCase(Kiwipete, 4, 4_085_603L)]
        [TestCase(EnPassantHeavy, 5, 674_624L)]
        [TestCase(PromotionHeavy, 4, 422_333L)]
        [TestCase(Position5, 4, 2_103_487L)]
        [TestCase(Position6, 4, 3_894_594L)]
        public void DeepPerft_MatchesPublishedNodeCounts(string fen, int depth, long expected)
        {
            AssertPerft(fen, depth, expected);
        }

        private static void AssertPerft(string fen, int depth, long expected)
        {
            long actual = PerftRunner.Run(fen, depth);

            // The divide breakdown is only computed on failure, and only shallowly, because it is
            // what turns "the number is wrong" into "this specific move is wrong".
            string diagnostics = actual == expected
                ? string.Empty
                : "\n" + PerftRunner.Divide(fen, System.Math.Min(depth, 2));

            Assert.That(actual, Is.EqualTo(expected), $"perft({depth}) for '{fen}'.{diagnostics}");
        }
    }
}
