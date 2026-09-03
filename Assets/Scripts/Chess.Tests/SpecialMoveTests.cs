using System.Collections.Generic;
using System.Linq;
using Chess.Core.Game;
using Chess.Core.Notation;
using Chess.Core.Primitives;
using NUnit.Framework;

namespace Chess.Tests
{
    /// <summary>
    /// The rules that are easy to get almost right: castling, en passant and promotion.
    /// </summary>
    /// <remarks>
    /// Perft would catch most of these as a wrong total, but only these tests say <em>which</em>
    /// rule broke, and only these pin down the cases where the right move count hides the wrong
    /// board state.
    /// </remarks>
    [TestFixture]
    public sealed class SpecialMoveTests
    {
        private static ChessGame GameFrom(string fen)
        {
            ChessGame game = ChessGame.CreateStandard();
            game.LoadFen(fen);
            return game;
        }

        private static IReadOnlyList<Move> MovesFrom(ChessGame game, string square)
        {
            Assert.That(Square.TryParse(square, out Square parsed), Is.True, $"'{square}' is not a square.");
            return game.GetLegalMovesFrom(parsed);
        }

        private static Move Move(ChessGame game, string from, string to, PieceType promotion = PieceType.None)
        {
            Square.TryParse(from, out Square fromSquare);
            Square.TryParse(to, out Square toSquare);

            Assert.That(
                game.TryFindMove(fromSquare, toSquare, promotion, out Move move),
                Is.True,
                $"{from}{to} is not legal in '{game.ToFen()}'.");

            return move;
        }

        private static Piece At(ChessGame game, string square)
        {
            Square.TryParse(square, out Square parsed);
            return game.Position[parsed];
        }

        [Test]
        public void Castling_MovesBothKingAndRook()
        {
            ChessGame game = GameFrom("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");

            Assert.That(game.TryMakeMove(Move(game, "e1", "g1")), Is.True);

            Assert.That(At(game, "g1"), Is.EqualTo(Piece.WhiteKing), "king should land on g1");
            Assert.That(At(game, "f1"), Is.EqualTo(Piece.WhiteRook), "rook should hop to f1");
            Assert.That(At(game, "e1").IsNone, Is.True);
            Assert.That(At(game, "h1").IsNone, Is.True);
        }

        [Test]
        public void QueenSideCastling_MovesRookToD1()
        {
            ChessGame game = GameFrom("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");

            Assert.That(game.TryMakeMove(Move(game, "e1", "c1")), Is.True);

            Assert.That(At(game, "c1"), Is.EqualTo(Piece.WhiteKing));
            Assert.That(At(game, "d1"), Is.EqualTo(Piece.WhiteRook));
        }

        [Test]
        public void Castling_IsIllegalWhileInCheck()
        {
            // The black rook on e8 checks along the e-file, and a king may not castle out of check.
            ChessGame game = GameFrom("4r3/8/8/8/8/8/8/R3K2R w KQ - 0 1");

            Assert.That(MovesFrom(game, "e1").Any(move => move.IsCastle), Is.False);
        }

        [Test]
        public void Castling_IsIllegalThroughAnAttackedSquare()
        {
            // The rook on f8 attacks f1, which the king would have to cross.
            ChessGame game = GameFrom("5r2/8/8/8/8/8/8/R3K2R w KQ - 0 1");

            Assert.That(MovesFrom(game, "e1").Any(move => move.IsKingSideCastle), Is.False);
            Assert.That(MovesFrom(game, "e1").Any(move => move.IsQueenSideCastle), Is.True,
                "the queen's side is unaffected and must remain available");
        }

        [Test]
        public void Castling_IsLegalWhenOnlyTheRookPathIsAttacked()
        {
            // b1 is attacked, but the king travels e1-d1-c1 and never visits it.
            ChessGame game = GameFrom("1r6/8/8/8/8/8/8/R3K2R w KQ - 0 1");

            Assert.That(MovesFrom(game, "e1").Any(move => move.IsQueenSideCastle), Is.True);
        }

        [Test]
        public void MovingTheKing_RevokesBothCastlingRights()
        {
            ChessGame game = GameFrom("r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1");

            game.TryMakeMove(Move(game, "e1", "f1"));

            Assert.That(game.Position.CastlingRights.Has(CastlingRights.WhiteKingSide), Is.False);
            Assert.That(game.Position.CastlingRights.Has(CastlingRights.WhiteQueenSide), Is.False);
            Assert.That(game.Position.CastlingRights.Has(CastlingRights.Black), Is.True,
                "black's rights are untouched");
        }

        [Test]
        public void CapturingARookOnItsHomeSquare_RevokesThatCastlingRight()
        {
            ChessGame game = GameFrom("r3k2r/8/8/8/8/6n1/8/R3K2R b KQkq - 0 1");

            // The knight on g3 takes the h1 rook, which must also remove White's king-side right.
            Assert.That(game.TryMakeMove(Move(game, "g3", "h1")), Is.True);

            Assert.That(game.Position.CastlingRights.Has(CastlingRights.WhiteKingSide), Is.False);
            Assert.That(game.Position.CastlingRights.Has(CastlingRights.WhiteQueenSide), Is.True);
        }

        [Test]
        public void EnPassant_RemovesThePawnBesideTheCapturingPawn()
        {
            ChessGame game = GameFrom("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");

            Move enPassant = Move(game, "e5", "d6");
            Assert.That(enPassant.IsEnPassant, Is.True);
            Assert.That(game.TryMakeMove(enPassant), Is.True);

            Assert.That(At(game, "d6"), Is.EqualTo(Piece.WhitePawn), "the capturing pawn lands on d6");
            Assert.That(At(game, "d5").IsNone, Is.True, "the captured pawn stood on d5, not d6");
        }

        [Test]
        public void EnPassant_IsOnlyAvailableImmediately()
        {
            ChessGame game = GameFrom("4k3/7p/8/3pP3/8/8/8/4K3 w - d6 0 1");

            game.TryMakeMove(Move(game, "e1", "e2"));
            game.TryMakeMove(Move(game, "h7", "h6"));

            Assert.That(MovesFrom(game, "e5").Any(move => move.IsEnPassant), Is.False);
        }

        [Test]
        public void EnPassant_IsIllegalWhenItWouldExposeTheKing()
        {
            // White king on h5, black rook on a5. Capturing en passant vacates e5 and would open
            // the rank, which the legality filter must reject even though the pawn itself is not
            // conventionally "pinned".
            ChessGame game = GameFrom("8/8/8/r2pP2K/8/8/8/4k3 w - d6 0 1");

            Assert.That(MovesFrom(game, "e5").Any(move => move.IsEnPassant), Is.False);
        }

        [Test]
        public void EnPassantTarget_IsSetOnlyByADoublePush()
        {
            ChessGame game = GameFrom("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1");

            game.TryMakeMove(Move(game, "e2", "e4"));
            Assert.That(game.Position.EnPassantTarget.ToString(), Is.EqualTo("e3"));

            game.TryMakeMove(Move(game, "e8", "d8"));
            Assert.That(game.Position.EnPassantTarget.IsValid, Is.False,
                "the target must be cleared once the chance has passed");
        }

        [Test]
        public void Promotion_OffersAllFourPieces()
        {
            // The black king sits on h8 rather than e8, which would block the pawn's path.
            ChessGame game = GameFrom("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");

            IReadOnlyList<Move> moves = MovesFrom(game, "e7");
            PieceType[] offered = moves
                .Where(move => move.IsPromotion)
                .Select(move => move.PromotionPieceType)
                .OrderBy(type => (int)type)
                .ToArray();

            Assert.That(offered, Is.EqualTo(new[]
            {
                PieceType.Knight,
                PieceType.Bishop,
                PieceType.Rook,
                PieceType.Queen
            }));
        }

        [Test]
        public void Promotion_PlacesTheChosenPiece()
        {
            ChessGame game = GameFrom("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");

            Assert.That(game.TryMakeMove(Move(game, "e7", "e8", PieceType.Knight)), Is.True);
            Assert.That(At(game, "e8"), Is.EqualTo(Piece.WhiteKnight));
        }

        [Test]
        public void CapturingPromotion_IsGeneratedAsBothCaptureAndPromotion()
        {
            ChessGame game = GameFrom("5r2/4P3/8/8/8/8/8/4K2k w - - 0 1");

            Move capture = Move(game, "e7", "f8", PieceType.Queen);
            Assert.That(capture.IsCapture, Is.True);
            Assert.That(capture.IsPromotion, Is.True);

            Assert.That(game.TryMakeMove(capture), Is.True);
            Assert.That(At(game, "f8"), Is.EqualTo(Piece.WhiteQueen));
        }

        [Test]
        public void UndoingEveryMove_RestoresTheStartingPosition()
        {
            ChessGame game = ChessGame.CreateStandard();
            string startFen = game.ToFen();
            ulong startKey = game.Position.ZobristKey;

            // A sequence that exercises a double push, a capture and castling.
            foreach ((string from, string to) in new[]
                     {
                         ("e2", "e4"), ("e7", "e5"),
                         ("g1", "f3"), ("b8", "c6"),
                         ("f1", "c4"), ("g8", "f6"),
                         ("e1", "g1")
                     })
            {
                Assert.That(game.TryMakeMove(Move(game, from, to)), Is.True, $"{from}{to}");
            }

            while (game.CanUndo)
            {
                Assert.That(game.TryUndoLastMove(), Is.True);
            }

            Assert.That(game.ToFen(), Is.EqualTo(startFen));
            Assert.That(game.Position.ZobristKey, Is.EqualTo(startKey),
                "the incremental hash must return to its starting value");
        }

        [Test]
        public void Fen_RoundTripsThroughSerialisation()
        {
            const string fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 3 12";

            ChessGame game = GameFrom(fen);

            Assert.That(game.ToFen(), Is.EqualTo(fen));
        }
    }
}
