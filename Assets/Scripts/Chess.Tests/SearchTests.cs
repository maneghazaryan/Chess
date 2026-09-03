using System.Threading;
using Chess.AI;
using Chess.AI.Evaluation;
using Chess.AI.Search;
using Chess.Core.Board;
using Chess.Core.Game;
using Chess.Core.Notation;
using Chess.Core.Primitives;
using NUnit.Framework;

namespace Chess.Tests
{
    /// <summary>
    /// That the search finds forced wins, avoids losing material, and respects its limits.
    /// </summary>
    /// <remarks>
    /// Deliberately asserts on outcomes rather than on node counts. Node counts change whenever
    /// ordering or pruning is tuned, so a test that pinned them would fail on every improvement;
    /// "does it find the mate" stays true no matter how the search is rewritten.
    /// </remarks>
    [TestFixture]
    public sealed class SearchTests
    {
        private static SearchResult Search(string fen, int depth)
        {
            ChessBoard board = FenSerializer.Parse(fen);
            return AlphaBetaSearch.CreateStandard().FindBestMove(board, SearchLimits.Depth(depth));
        }

        [Test]
        public void FindsMateInOne()
        {
            SearchResult result = Search("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1", depth: 2);

            Assert.That(result.BestMove.ToUci(), Is.EqualTo("a1a8"));
            Assert.That(result.Score, Is.GreaterThan(EvaluationConstants.MateThreshold));
        }

        [Test]
        public void FindsMateInTwo()
        {
            // Doubled rooks against a back rank guarded by one rook. 1. Rd8+ forces Rxd8, the only
            // legal reply, and 2. Rxd8 is mate. The point of the test is that the first move looks
            // like it simply loses a rook, so only a search that sees three plies ahead plays it.
            SearchResult result = Search("2r3k1/1b3ppp/8/8/8/8/3R4/3R2K1 w - - 0 1", depth: 4);

            // Either rook may lead; the other delivers the mate.
            Assert.That(result.BestMove.ToUci(), Is.EqualTo("d1d8").Or.EqualTo("d2d8"), $"got {result}");
            Assert.That(result.Score, Is.EqualTo(EvaluationConstants.MateScore - 3),
                $"expected a mate on the third ply, got {result}");
        }

        [Test]
        public void PrefersTheFasterMate()
        {
            // Mate is available in one; a search allowed to look four plies deep must still choose
            // it rather than a slower forced win, which is what the ply term in the mate score is
            // there to guarantee.
            SearchResult result = Search("6k1/5ppp/8/8/8/8/8/R6K w - - 0 1", depth: 4);

            Assert.That(result.BestMove.ToUci(), Is.EqualTo("a1a8"));
            Assert.That(result.Score, Is.EqualTo(EvaluationConstants.MateScore - 1),
                "a mate delivered on the first ply scores mate-minus-one");
        }

        [Test]
        public void RecognisesBeingMated()
        {
            SearchResult result = Search("R5k1/5ppp/8/8/8/8/8/4K3 b - - 0 1", depth: 3);

            Assert.That(result.Score, Is.LessThan(-EvaluationConstants.MateThreshold));
        }

        [Test]
        public void CapturesAFreePiece()
        {
            // The black queen on d5 is undefended. Pawns keep this from being an insufficient
            // material draw after the capture.
            SearchResult result = Search("4k3/pppp4/8/3q4/4B3/8/PPPP4/4K3 w - - 0 1", depth: 4);

            Assert.That(result.BestMove.ToUci(), Is.EqualTo("e4d5"));
            Assert.That(result.Score, Is.GreaterThan(200),
                "winning a queen for a bishop is worth roughly a bishop");
        }

        [Test]
        public void QuiescenceRefusesADefendedPawn()
        {
            // Nxd6 wins a pawn but the knight is recaptured by either the c7 or the e7 pawn. Only
            // a search that keeps looking past the horizon sees the recapture.
            SearchResult result = Search("4k3/2p1p3/3p4/8/4N3/8/4P3/4K3 w - - 0 1", depth: 4);

            Assert.That(result.BestMove.ToUci(), Is.Not.EqualTo("e4d6"));
        }

        [Test]
        public void AlwaysReturnsALegalMove()
        {
            ChessGame game = ChessGame.CreateStandard();
            AlphaBetaSearch engine = AlphaBetaSearch.CreateStandard();

            for (int ply = 0; ply < 20 && !game.IsGameOver; ply++)
            {
                SearchResult result = engine.FindBestMove(game.Position, SearchLimits.Depth(3));

                Assert.That(result.HasMove, Is.True, $"no move produced at ply {ply}");
                Assert.That(game.TryMakeMove(result.BestMove), Is.True,
                    $"illegal move {result.BestMove.ToUci()} at ply {ply} in '{game.ToFen()}'");
            }
        }

        [Test]
        public void SearchDoesNotMutateThePositionItWasGiven()
        {
            ChessGame game = ChessGame.CreateStandard();
            string before = game.ToFen();
            ulong keyBefore = game.Position.ZobristKey;

            AlphaBetaSearch.CreateStandard().FindBestMove(game.Position, SearchLimits.Depth(4));

            Assert.That(game.ToFen(), Is.EqualTo(before));
            Assert.That(game.Position.ZobristKey, Is.EqualTo(keyBefore));
        }

        [Test]
        public void RespectsItsDepthLimit()
        {
            SearchResult result = Search(FenSerializer.StartPosition, depth: 3);

            Assert.That(result.DepthReached, Is.EqualTo(3));
        }

        [Test]
        public void CancellationStopsTheSearchAndStillYieldsAMove()
        {
            ChessBoard board = FenSerializer.Parse(FenSerializer.StartPosition);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            SearchResult result = AlphaBetaSearch
                .CreateStandard()
                .FindBestMove(board, SearchLimits.Depth(8), cancellation.Token);

            Assert.That(result.HasMove, Is.True,
                "an interrupted search must still fall back to a legal move");
        }

        [Test]
        public void DeeperSearchesDoNotDegradeTacticalChoices()
        {
            const string fen = "4k3/pppp4/8/3q4/4B3/8/PPPP4/4K3 w - - 0 1";

            for (int depth = 1; depth <= 5; depth++)
            {
                SearchResult result = Search(fen, depth);
                Assert.That(result.BestMove.ToUci(), Is.EqualTo("e4d5"), $"at depth {depth}");
            }
        }

        [Test]
        public void EveryDifficultyProducesALegalMove()
        {
            var factory = new ChessEngineFactory();
            ChessGame game = ChessGame.CreateStandard();

            foreach (AiDifficulty difficulty in System.Enum.GetValues(typeof(AiDifficulty)))
            {
                IChessEngine engine = factory.Create(difficulty);
                SearchResult result = ((AlphaBetaSearch)engine)
                    .FindBestMove(game.Position, factory.GetLimits(difficulty));

                Assert.That(game.LegalMoves, Contains.Item(result.BestMove), $"{difficulty}");
            }
        }
    }
}
