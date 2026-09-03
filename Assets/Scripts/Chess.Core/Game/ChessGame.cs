using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Chess.Core.Board;
using Chess.Core.Notation;
using Chess.Core.Primitives;
using Chess.Core.Rules;

namespace Chess.Core.Game
{
    /// <summary>
    /// The Model root: owns the position, applies the rules, keeps the history and announces
    /// changes.
    /// </summary>
    /// <remarks>
    /// The legal move list is recomputed once per position change and cached, because both the UI
    /// (to highlight destinations) and move validation read it repeatedly between moves.
    /// </remarks>
    public sealed class ChessGame : IChessGame
    {
        private readonly ChessBoard _board;
        private readonly IMoveGenerator _moveGenerator;
        private readonly IGameStatusEvaluator _statusEvaluator;

        private readonly MoveList _legalMoves = new MoveList();
        private readonly List<Move> _legalMovesView = new List<Move>();
        private readonly ReadOnlyCollection<Move> _legalMovesReadOnly;

        private readonly List<MoveRecord> _history = new List<MoveRecord>();
        private readonly ReadOnlyCollection<MoveRecord> _historyReadOnly;

        public ChessGame(IMoveGenerator moveGenerator, IGameStatusEvaluator statusEvaluator)
        {
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _statusEvaluator = statusEvaluator ?? throw new ArgumentNullException(nameof(statusEvaluator));

            _board = new ChessBoard();
            _legalMovesReadOnly = _legalMovesView.AsReadOnly();
            _historyReadOnly = _history.AsReadOnly();

            LoadFenInternal(FenSerializer.StartPosition, raiseReset: false);
        }

        /// <summary>Builds a game wired with the standard rule set.</summary>
        public static ChessGame CreateStandard()
        {
            var attackService = new AttackService();
            MoveGenerator moveGenerator = MoveGenerator.CreateStandard(attackService);
            GameStatusEvaluator statusEvaluator = GameStatusEvaluator.CreateStandard(moveGenerator, attackService);

            return new ChessGame(moveGenerator, statusEvaluator);
        }

        public IBoard Position => _board;

        public PieceColor SideToMove => _board.SideToMove;

        public GameStatus Status { get; private set; }

        public GameResult Result { get; private set; }

        public bool IsGameOver => Status.IsGameOver();

        public IReadOnlyList<MoveRecord> History => _historyReadOnly;

        public IReadOnlyList<Move> LegalMoves => _legalMovesReadOnly;

        public bool CanUndo => _history.Count > 0;

        public event Action<MoveRecord> MoveMade;

        public event Action<MoveRecord> MoveUndone;

        public event Action<GameStatus> StatusChanged;

        public event Action GameReset;

        public IReadOnlyList<Move> GetLegalMovesFrom(Square from)
        {
            var result = new List<Move>(4);

            for (int i = 0; i < _legalMovesView.Count; i++)
            {
                if (_legalMovesView[i].From == from)
                {
                    result.Add(_legalMovesView[i]);
                }
            }

            return result;
        }

        public bool TryFindMove(Square from, Square to, PieceType promotionType, out Move move)
        {
            for (int i = 0; i < _legalMovesView.Count; i++)
            {
                Move candidate = _legalMovesView[i];
                if (candidate.From != from || candidate.To != to)
                {
                    continue;
                }

                // Four legal moves share an origin and destination when a pawn promotes, so the
                // requested piece has to match. A caller that does not care passes None and takes
                // whichever promotion comes first only if the move is not a promotion at all.
                if (candidate.IsPromotion && promotionType != PieceType.None &&
                    candidate.PromotionPieceType != promotionType)
                {
                    continue;
                }

                move = candidate;
                return true;
            }

            move = Move.None;
            return false;
        }

        public bool TryMakeMove(in Move move)
        {
            if (IsGameOver || !_legalMoves.Contains(move))
            {
                return false;
            }

            Piece moving = _board[move.From];
            Square capturedSquare = ResolveCapturedSquare(move, moving.Color);
            Piece captured = capturedSquare.IsValid ? _board[capturedSquare] : Piece.None;
            int fullMoveNumber = _board.FullMoveNumber;

            // SAN disambiguation needs the pre-move position, so the body is built before the
            // move is played and the check or mate suffix appended afterwards.
            string notationBody = SanFormatter.Format(_board, move, _legalMovesView);

            _board.MakeMove(move);
            RefreshPositionState();

            var record = new MoveRecord(
                move,
                moving,
                captured,
                capturedSquare,
                SanFormatter.AppendStatusSuffix(notationBody, Status),
                Status,
                fullMoveNumber);

            _history.Add(record);

            MoveMade?.Invoke(record);
            StatusChanged?.Invoke(Status);
            return true;
        }

        public bool TryUndoLastMove()
        {
            if (_history.Count == 0)
            {
                return false;
            }

            MoveRecord record = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);

            _board.UnmakeMove();
            RefreshPositionState();

            MoveUndone?.Invoke(record);
            StatusChanged?.Invoke(Status);
            return true;
        }

        public void Reset() => LoadFen(FenSerializer.StartPosition);

        public void LoadFen(string fen) => LoadFenInternal(fen, raiseReset: true);

        public string ToFen() => FenSerializer.Serialize(_board);

        private void LoadFenInternal(string fen, bool raiseReset)
        {
            FenSerializer.Populate(_board, fen);
            _history.Clear();
            RefreshPositionState();

            if (raiseReset)
            {
                GameReset?.Invoke();
                StatusChanged?.Invoke(Status);
            }
        }

        private void RefreshPositionState()
        {
            _moveGenerator.GenerateLegalMoves(_board, _legalMoves);

            _legalMovesView.Clear();
            for (int i = 0; i < _legalMoves.Count; i++)
            {
                _legalMovesView.Add(_legalMoves[i]);
            }

            Status = _statusEvaluator.Evaluate(_board);
            Result = _statusEvaluator.ToResult(Status, _board.SideToMove);
        }

        private Square ResolveCapturedSquare(in Move move, PieceColor mover)
        {
            if (move.IsEnPassant)
            {
                return move.To.Offset(0, -mover.PawnDirection());
            }

            return _board[move.To].IsSome ? move.To : Square.None;
        }
    }
}
