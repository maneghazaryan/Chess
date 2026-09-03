using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Chess.Core.Primitives;

namespace Chess.Core.Board
{
    /// <summary>
    /// A mailbox board: a flat 64-entry array of pieces plus the incremental state that the rules
    /// need (side to move, castling rights, en passant target, clocks, Zobrist hash).
    /// </summary>
    /// <remarks>
    /// Bitboards would search faster, but a mailbox reads like the thing it models and is fast
    /// enough at the depths this engine targets. The two structures that keep it fast are the
    /// per-colour piece lists, which let move generation iterate the ~16 occupied squares instead
    /// of scanning all 64, and the undo stack, which makes rewinding a move as cheap as making one.
    /// </remarks>
    public sealed class ChessBoard : IMutableBoard
    {
        private const int InitialUndoCapacity = 256;

        private readonly Piece[] _squares = new Piece[Square.Count];

        // Per-colour occupied squares, with a reverse index so removal is O(1) via swap-remove.
        private readonly List<Square>[] _occupied =
        {
            new List<Square>(16),
            new List<Square>(16)
        };

        private readonly ReadOnlyCollection<Square>[] _occupiedReadOnly;
        private readonly int[] _occupiedSlotBySquare = new int[Square.Count];
        private readonly int[,] _pieceCounts = new int[2, 7];
        private readonly Square[] _kingSquares = new Square[2];

        private BoardStateUndo[] _undoStack = new BoardStateUndo[InitialUndoCapacity];
        private int _undoCount;

        private ulong[] _positionKeys = new ulong[InitialUndoCapacity];
        private int _positionKeyCount;

        /// <summary>
        /// Squares whose occupancy changing can invalidate castling rights: the four corners and
        /// the two king squares. Applying <c>rights &amp;= mask[from] &amp; mask[to]</c> handles a
        /// king moving, a rook moving and a rook being captured in one expression.
        /// </summary>
        private static readonly CastlingRights[] CastlingMaskBySquare = BuildCastlingMasks();

        public ChessBoard()
        {
            _occupiedReadOnly = new[]
            {
                _occupied[0].AsReadOnly(),
                _occupied[1].AsReadOnly()
            };

            Clear();
        }

        private ChessBoard(ChessBoard source)
            : this()
        {
            CopyFrom(source);
        }

        public Piece this[Square square]
        {
            get => square.IsValid ? _squares[square.Index] : Piece.None;
        }

        public PieceColor SideToMove { get; private set; }

        public CastlingRights CastlingRights { get; private set; }

        public Square EnPassantTarget { get; private set; }

        public int HalfMoveClock { get; private set; }

        public int FullMoveNumber { get; private set; }

        public ulong ZobristKey { get; private set; }

        public int PlyCount => _undoCount;

        public Move LastMove => _undoCount > 0 ? _undoStack[_undoCount - 1].Move : Move.None;

        public Square GetKingSquare(PieceColor color) => _kingSquares[(int)color];

        public IReadOnlyList<Square> GetOccupiedSquares(PieceColor color) => _occupiedReadOnly[(int)color];

        public int CountPieces(PieceColor color, PieceType type) => _pieceCounts[(int)color, (int)type];

        public IMutableBoard CreateWorkingCopy() => new ChessBoard(this);

        /// <summary>
        /// Empties the board and resets all state. The Zobrist key is rebuilt from scratch so an
        /// empty board still hashes consistently.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_squares, 0, _squares.Length);
            Array.Clear(_pieceCounts, 0, _pieceCounts.Length);
            _occupied[0].Clear();
            _occupied[1].Clear();
            _kingSquares[0] = Square.None;
            _kingSquares[1] = Square.None;

            SideToMove = PieceColor.White;
            CastlingRights = CastlingRights.None;
            EnPassantTarget = Square.None;
            HalfMoveClock = 0;
            FullMoveNumber = 1;

            _undoCount = 0;
            _positionKeyCount = 0;

            RecomputeZobristKey();
            PushPositionKey();
        }

        /// <summary>
        /// Places a piece during position setup. Not for use during play: it does not maintain the
        /// undo stack, so a position built this way cannot be rewound past its starting point.
        /// </summary>
        public void SetupPiece(Square square, Piece piece)
        {
            if (!square.IsValid)
            {
                throw new ArgumentException("Cannot place a piece on an invalid square.", nameof(square));
            }

            if (_squares[square.Index].IsSome)
            {
                RemovePiece(square);
            }

            if (piece.IsSome)
            {
                PlacePiece(square, piece);
            }
        }

        /// <summary>Finalises a position built with <see cref="SetupPiece"/> and friends.</summary>
        public void CompleteSetup(
            PieceColor sideToMove,
            CastlingRights castlingRights,
            Square enPassantTarget,
            int halfMoveClock,
            int fullMoveNumber)
        {
            SideToMove = sideToMove;
            CastlingRights = castlingRights;
            EnPassantTarget = enPassantTarget;
            HalfMoveClock = Math.Max(0, halfMoveClock);
            FullMoveNumber = Math.Max(1, fullMoveNumber);

            _undoCount = 0;
            _positionKeyCount = 0;
            RecomputeZobristKey();
            PushPositionKey();
        }

        public void CopyFrom(IBoard source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Clear();

            for (int i = 0; i < Square.Count; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = source[square];
                if (piece.IsSome)
                {
                    PlacePiece(square, piece);
                }
            }

            SideToMove = source.SideToMove;
            CastlingRights = source.CastlingRights;
            EnPassantTarget = source.EnPassantTarget;
            HalfMoveClock = source.HalfMoveClock;
            FullMoveNumber = source.FullMoveNumber;

            _undoCount = 0;
            _positionKeyCount = 0;
            RecomputeZobristKey();

            CopyPositionHistoryFrom(source);
        }

        public void MakeMove(in Move move)
        {
            if (!move.IsValid)
            {
                throw new ArgumentException($"Cannot make an invalid move: {move}.", nameof(move));
            }

            Piece moving = _squares[move.From.Index];
            if (moving.IsNone)
            {
                throw new InvalidOperationException($"No piece on {move.From} to play {move}.");
            }

            PushUndo(move);

            PieceColor mover = moving.Color;
            Square capturedSquare = ResolveCapturedSquare(move, mover);
            Piece captured = capturedSquare.IsValid ? _squares[capturedSquare.Index] : Piece.None;

            _undoStack[_undoCount - 1].CapturedPiece = captured;
            _undoStack[_undoCount - 1].CapturedPieceSquare = capturedSquare;

            if (captured.IsSome)
            {
                RemovePiece(capturedSquare);
            }

            MovePiece(move.From, move.To);

            if (move.IsPromotion)
            {
                RemovePiece(move.To);
                PlacePiece(move.To, Piece.Create(mover, move.PromotionPieceType));
            }

            if (move.IsCastle)
            {
                GetCastlingRookSquares(mover, move.IsKingSideCastle, out Square rookFrom, out Square rookTo);
                MovePiece(rookFrom, rookTo);
            }

            UpdateCastlingRights(move);
            UpdateEnPassantTarget(move, moving, mover);

            bool resetsClock = moving.Type == PieceType.Pawn || captured.IsSome;
            HalfMoveClock = resetsClock ? 0 : HalfMoveClock + 1;

            if (mover == PieceColor.Black)
            {
                FullMoveNumber++;
            }

            SideToMove = mover.Opponent();
            ZobristKey ^= Zobrist.SideToMoveKey;

            PushPositionKey();
        }

        public void UnmakeMove()
        {
            if (_undoCount == 0)
            {
                throw new InvalidOperationException("There is no move to unmake.");
            }

            if (_undoStack[_undoCount - 1].IsNullMove)
            {
                throw new InvalidOperationException("The last state change was a null move; call UnmakeNullMove.");
            }

            BoardStateUndo undo = _undoStack[--_undoCount];
            _positionKeyCount--;

            SideToMove = SideToMove.Opponent();
            if (SideToMove == PieceColor.Black)
            {
                FullMoveNumber--;
            }

            Move move = undo.Move;
            PieceColor mover = SideToMove;

            if (move.IsCastle)
            {
                GetCastlingRookSquares(mover, move.IsKingSideCastle, out Square rookFrom, out Square rookTo);
                MovePiece(rookTo, rookFrom);
            }

            if (move.IsPromotion)
            {
                RemovePiece(move.To);
                PlacePiece(move.To, Piece.Create(mover, PieceType.Pawn));
            }

            MovePiece(move.To, move.From);

            if (undo.CapturedPiece.IsSome)
            {
                PlacePiece(undo.CapturedPieceSquare, undo.CapturedPiece);
            }

            CastlingRights = undo.CastlingRights;
            EnPassantTarget = undo.EnPassantTarget;
            HalfMoveClock = undo.HalfMoveClock;

            // The incremental XORs above are exact, but restoring the saved key is both cheaper
            // and immune to any drift, so the stored value wins.
            ZobristKey = undo.ZobristKey;
        }

        public void MakeNullMove()
        {
            PushUndo(Move.None);
            _undoStack[_undoCount - 1].IsNullMove = true;

            if (EnPassantTarget.IsValid)
            {
                ZobristKey ^= Zobrist.EnPassant(EnPassantTarget);
                EnPassantTarget = Square.None;
            }

            if (SideToMove == PieceColor.Black)
            {
                FullMoveNumber++;
            }

            SideToMove = SideToMove.Opponent();
            ZobristKey ^= Zobrist.SideToMoveKey;
            HalfMoveClock++;

            PushPositionKey();
        }

        public void UnmakeNullMove()
        {
            if (_undoCount == 0 || !_undoStack[_undoCount - 1].IsNullMove)
            {
                throw new InvalidOperationException("The last state change was not a null move.");
            }

            BoardStateUndo undo = _undoStack[--_undoCount];
            _positionKeyCount--;

            SideToMove = SideToMove.Opponent();
            if (SideToMove == PieceColor.Black)
            {
                FullMoveNumber--;
            }

            CastlingRights = undo.CastlingRights;
            EnPassantTarget = undo.EnPassantTarget;
            HalfMoveClock = undo.HalfMoveClock;
            ZobristKey = undo.ZobristKey;
        }

        public int GetRepetitionCount()
        {
            if (_positionKeyCount == 0)
            {
                return 0;
            }

            int count = 1;

            // Only positions since the last irreversible move (pawn push or capture) can repeat,
            // and only every second ply has the same side to move.
            int reachable = Math.Min(HalfMoveClock, _positionKeyCount - 1);
            for (int back = 2; back <= reachable; back += 2)
            {
                if (_positionKeys[_positionKeyCount - 1 - back] == ZobristKey)
                {
                    count++;
                }
            }

            return count;
        }

        private Square ResolveCapturedSquare(in Move move, PieceColor mover)
        {
            if (move.IsEnPassant)
            {
                return move.To.Offset(0, -mover.PawnDirection());
            }

            return _squares[move.To.Index].IsSome ? move.To : Square.None;
        }

        private void UpdateCastlingRights(in Move move)
        {
            CastlingRights updated = CastlingRights
                                     & CastlingMaskBySquare[move.From.Index]
                                     & CastlingMaskBySquare[move.To.Index];

            if (updated == CastlingRights)
            {
                return;
            }

            ZobristKey ^= Zobrist.Castling(CastlingRights);
            ZobristKey ^= Zobrist.Castling(updated);
            CastlingRights = updated;
        }

        private void UpdateEnPassantTarget(in Move move, Piece moving, PieceColor mover)
        {
            Square previous = EnPassantTarget;

            EnPassantTarget = move.IsDoublePawnPush && moving.Type == PieceType.Pawn
                ? move.From.Offset(0, mover.PawnDirection())
                : Square.None;

            if (previous == EnPassantTarget)
            {
                return;
            }

            ZobristKey ^= Zobrist.EnPassant(previous);
            ZobristKey ^= Zobrist.EnPassant(EnPassantTarget);
        }

        public static void GetCastlingRookSquares(PieceColor color, bool kingSide, out Square from, out Square to)
        {
            int rank = color == PieceColor.White ? 0 : 7;
            from = Square.FromFileRank(kingSide ? 7 : 0, rank);
            to = Square.FromFileRank(kingSide ? 5 : 3, rank);
        }

        public static Square GetCastlingKingDestination(PieceColor color, bool kingSide)
        {
            int rank = color == PieceColor.White ? 0 : 7;
            return Square.FromFileRank(kingSide ? 6 : 2, rank);
        }

        public static Square GetKingStartSquare(PieceColor color)
        {
            return Square.FromFileRank(4, color == PieceColor.White ? 0 : 7);
        }

        private void PlacePiece(Square square, Piece piece)
        {
            int index = square.Index;
            _squares[index] = piece;

            List<Square> list = _occupied[(int)piece.Color];
            _occupiedSlotBySquare[index] = list.Count;
            list.Add(square);

            _pieceCounts[(int)piece.Color, (int)piece.Type]++;

            if (piece.Type == PieceType.King)
            {
                _kingSquares[(int)piece.Color] = square;
            }

            ZobristKey ^= Zobrist.PieceSquare(piece, square);
        }

        private void RemovePiece(Square square)
        {
            int index = square.Index;
            Piece piece = _squares[index];
            if (piece.IsNone)
            {
                return;
            }

            ZobristKey ^= Zobrist.PieceSquare(piece, square);

            List<Square> list = _occupied[(int)piece.Color];
            int slot = _occupiedSlotBySquare[index];
            int lastSlot = list.Count - 1;

            if (slot != lastSlot)
            {
                Square moved = list[lastSlot];
                list[slot] = moved;
                _occupiedSlotBySquare[moved.Index] = slot;
            }

            list.RemoveAt(lastSlot);

            _pieceCounts[(int)piece.Color, (int)piece.Type]--;

            if (piece.Type == PieceType.King)
            {
                _kingSquares[(int)piece.Color] = Square.None;
            }

            _squares[index] = Piece.None;
        }

        private void MovePiece(Square from, Square to)
        {
            Piece piece = _squares[from.Index];
            RemovePiece(from);
            PlacePiece(to, piece);
        }

        private void PushUndo(in Move move)
        {
            if (_undoCount == _undoStack.Length)
            {
                Array.Resize(ref _undoStack, _undoStack.Length * 2);
            }

            _undoStack[_undoCount++] = new BoardStateUndo
            {
                Move = move,
                CapturedPiece = Piece.None,
                CapturedPieceSquare = Square.None,
                CastlingRights = CastlingRights,
                EnPassantTarget = EnPassantTarget,
                HalfMoveClock = HalfMoveClock,
                ZobristKey = ZobristKey,
                IsNullMove = false
            };
        }

        private void PushPositionKey()
        {
            if (_positionKeyCount == _positionKeys.Length)
            {
                Array.Resize(ref _positionKeys, _positionKeys.Length * 2);
            }

            _positionKeys[_positionKeyCount++] = ZobristKey;
        }

        private void CopyPositionHistoryFrom(IBoard source)
        {
            // Only ChessBoard exposes its key history. Copying it matters because a search that
            // starts from a position one repetition deep must still see the third one as a draw.
            if (source is ChessBoard other)
            {
                if (_positionKeys.Length < other._positionKeyCount)
                {
                    _positionKeys = new ulong[other._positionKeys.Length];
                }

                Array.Copy(other._positionKeys, _positionKeys, other._positionKeyCount);
                _positionKeyCount = other._positionKeyCount;
            }
            else
            {
                _positionKeyCount = 0;
                PushPositionKey();
            }
        }

        private void RecomputeZobristKey()
        {
            ulong key = 0UL;

            for (int i = 0; i < Square.Count; i++)
            {
                Piece piece = _squares[i];
                if (piece.IsSome)
                {
                    key ^= Zobrist.PieceSquare(piece, Square.FromIndex(i));
                }
            }

            key ^= Zobrist.Castling(CastlingRights);
            key ^= Zobrist.EnPassant(EnPassantTarget);

            if (SideToMove == PieceColor.Black)
            {
                key ^= Zobrist.SideToMoveKey;
            }

            ZobristKey = key;
        }

        private static CastlingRights[] BuildCastlingMasks()
        {
            var masks = new CastlingRights[Square.Count];
            for (int i = 0; i < masks.Length; i++)
            {
                masks[i] = CastlingRights.All;
            }

            masks[Square.FromFileRank(0, 0).Index] &= ~CastlingRights.WhiteQueenSide; // a1
            masks[Square.FromFileRank(7, 0).Index] &= ~CastlingRights.WhiteKingSide;  // h1
            masks[Square.FromFileRank(4, 0).Index] &= ~CastlingRights.White;          // e1
            masks[Square.FromFileRank(0, 7).Index] &= ~CastlingRights.BlackQueenSide; // a8
            masks[Square.FromFileRank(7, 7).Index] &= ~CastlingRights.BlackKingSide;  // h8
            masks[Square.FromFileRank(4, 7).Index] &= ~CastlingRights.Black;          // e8

            return masks;
        }
    }
}
