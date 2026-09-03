using Chess.Core.Game;
using Chess.Core.Notation;
using Chess.Core.Primitives;
using NUnit.Framework;

namespace Chess.Tests
{
    /// <summary>
    /// FEN parsing and SAN formatting, including the disambiguation rules that make SAN awkward.
    /// </summary>
    [TestFixture]
    public sealed class NotationTests
    {
        private static ChessGame GameFrom(string fen)
        {
            ChessGame game = ChessGame.CreateStandard();
            game.LoadFen(fen);
            return game;
        }

        private static string NotationFor(ChessGame game, string from, string to, PieceType promotion = PieceType.None)
        {
            Square.TryParse(from, out Square fromSquare);
            Square.TryParse(to, out Square toSquare);

            Assert.That(game.TryFindMove(fromSquare, toSquare, promotion, out Move move), Is.True,
                $"{from}{to} is not legal in '{game.ToFen()}'.");
            Assert.That(game.TryMakeMove(move), Is.True);

            return game.History[game.History.Count - 1].StandardNotation;
        }

        [Test]
        public void StartPosition_ParsesToTheExpectedState()
        {
            ChessGame game = ChessGame.CreateStandard();

            Assert.That(game.SideToMove, Is.EqualTo(PieceColor.White));
            Assert.That(game.Position.CastlingRights, Is.EqualTo(CastlingRights.All));
            Assert.That(game.Position.EnPassantTarget.IsValid, Is.False);
            Assert.That(game.Position.FullMoveNumber, Is.EqualTo(1));
            Assert.That(game.LegalMoves.Count, Is.EqualTo(20));
        }

        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("4k3/8/8/3pP3/8/8/8/4K3 w - d6 5 42")]
        [TestCase("8/8/8/8/8/8/8/4K2k b - - 0 1")]
        public void Fen_RoundTrips(string fen)
        {
            ChessGame game = GameFrom(fen);

            Assert.That(game.ToFen(), Is.EqualTo(fen));
        }

        [Test]
        public void PawnMove_IsJustTheDestination()
        {
            ChessGame game = ChessGame.CreateStandard();

            Assert.That(NotationFor(game, "e2", "e4"), Is.EqualTo("e4"));
        }

        [Test]
        public void PawnCapture_NamesTheOriginFile()
        {
            ChessGame game = GameFrom("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1");

            Assert.That(NotationFor(game, "e4", "d5"), Is.EqualTo("exd5"));
        }

        [Test]
        public void PieceCapture_UsesAnX()
        {
            // The bishop is on g2 rather than c1: d5 is a light square, and c1 is dark.
            ChessGame game = GameFrom("4k3/8/8/3p4/8/8/6B1/4K3 w - - 0 1");

            Assert.That(NotationFor(game, "g2", "d5"), Is.EqualTo("Bxd5"));
        }

        [Test]
        public void Castling_UsesTheOhNotation()
        {
            ChessGame kingSide = GameFrom("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");
            Assert.That(NotationFor(kingSide, "e1", "g1"), Is.EqualTo("O-O"));

            ChessGame queenSide = GameFrom("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");
            Assert.That(NotationFor(queenSide, "e1", "c1"), Is.EqualTo("O-O-O"));
        }

        [Test]
        public void Promotion_NamesThePieceAfterAnEquals()
        {
            ChessGame game = GameFrom("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");

            Assert.That(NotationFor(game, "e7", "e8", PieceType.Queen), Is.EqualTo("e8=Q+"),
                "the new queen also checks the king on h8, so the suffix belongs there too");
        }

        [Test]
        public void TwoKnightsReachingOneSquare_AreDistinguishedByFile()
        {
            // Knights on b1 and f1 can both reach d2.
            ChessGame game = GameFrom("4k3/8/8/8/8/8/8/1N2KN2 w - - 0 1");

            Assert.That(NotationFor(game, "b1", "d2"), Is.EqualTo("Nbd2"));
        }

        [Test]
        public void TwoRooksOnOneFile_AreDistinguishedByRank()
        {
            // Rooks on a1 and a5 share a file, so the file cannot separate them.
            ChessGame game = GameFrom("4k3/8/8/R7/8/8/8/R3K3 w - - 0 1");

            Assert.That(NotationFor(game, "a1", "a3"), Is.EqualTo("R1a3"));
        }

        [Test]
        public void ASinglePieceNeedsNoDisambiguation()
        {
            // The pawn is there only to keep king and knight against king from being an
            // immediate insufficient-material draw, which would leave no legal moves to name.
            ChessGame game = GameFrom("4k3/8/8/8/8/8/P7/1N2K3 w - - 0 1");

            Assert.That(NotationFor(game, "b1", "d2"), Is.EqualTo("Nd2"));
        }

        [Test]
        public void MalformedFen_IsRejected()
        {
            ChessGame game = ChessGame.CreateStandard();

            Assert.Throws<System.FormatException>(() => game.LoadFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR x KQkq -"));
            Assert.Throws<System.ArgumentException>(() => game.LoadFen("   "));
        }

        [Test]
        public void SquareCoordinates_MapToTheExpectedIndices()
        {
            Assert.That(Square.FromIndex(0).ToString(), Is.EqualTo("a1"));
            Assert.That(Square.FromIndex(63).ToString(), Is.EqualTo("h8"));
            Assert.That(Square.FromIndex(7).ToString(), Is.EqualTo("h1"));
            Assert.That(Square.FromIndex(56).ToString(), Is.EqualTo("a8"));

            Assert.That(default(Square).IsValid, Is.False, "an uninitialised square must not be a1");
            Assert.That(Square.FromIndex(64), Is.EqualTo(Square.None));
        }

        [Test]
        public void SquareColours_MatchTheBoard()
        {
            Square.TryParse("a1", out Square a1);
            Square.TryParse("h1", out Square h1);

            Assert.That(a1.IsLight, Is.False, "a1 is dark");
            Assert.That(h1.IsLight, Is.True, "h1 is light");
        }
    }
}
