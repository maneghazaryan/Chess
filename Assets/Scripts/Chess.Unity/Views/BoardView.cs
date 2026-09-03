using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chess.Core.Board;
using Chess.Core.Game;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using Chess.Unity.Input;
using UnityEngine;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Renders the board and its pieces in 2D, and reports which square the user clicked.
    /// </summary>
    /// <remarks>
    /// Strictly passive. It exposes no notion of whose turn it is, what is legal, or what a click
    /// means; it draws what it is handed and raises <see cref="SquareClicked"/> for someone else
    /// to interpret. The one piece of intelligence it does own is the animation, because that is
    /// presentation, not rules.
    /// </remarks>
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        [Header("Configuration")]
        [SerializeField] private BoardTheme _theme;
        [SerializeField] private PieceSpriteSet _pieceSprites;

        [Header("Scene references")]
        [Tooltip("Parent for the sixty-four square sprites. Created automatically if left empty.")]
        [SerializeField] private Transform _squareRoot;

        [Tooltip("Parent for the piece sprites. Created automatically if left empty.")]
        [SerializeField] private Transform _pieceRoot;

        [SerializeField] private HighlightLayer _highlightLayer;

        [Tooltip("Supplies square clicks. Any IBoardInputSource component on this object is used if empty.")]
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        /// <summary>Pieces draw above squares and highlights; the moving piece is lifted one higher still.</summary>
        private const int PieceSortingOrder = 10;

        private readonly SquareView[] _squareViews = new SquareView[Square.Count];
        private readonly PieceView[] _pieceViewsBySquare = new PieceView[Square.Count];
        private readonly List<PieceView> _pieceViewPool = new List<PieceView>();

        private IBoardInputSource _inputSource;
        private BoardGeometry _geometry;
        private bool _isBuilt;

        public event Action<Square> SquareClicked;

        public BoardOrientation Orientation { get; private set; } = BoardOrientation.WhiteAtBottom;

        public BoardTheme Theme => _theme;

        public BoardGeometry Geometry => _geometry;

        public void Initialise(BoardTheme theme, PieceSpriteSet pieceSprites, IBoardInputSource inputSource)
        {
            _theme = theme != null ? theme : _theme;
            _pieceSprites = pieceSprites != null ? pieceSprites : _pieceSprites;
            AttachInputSource(inputSource);
            EnsureBuilt();
        }

        public void SetOrientation(BoardOrientation orientation)
        {
            Orientation = orientation;
            _geometry = _geometry.WithOrientation(orientation);

            EnsureBuilt();
            RepositionSquares();
            RepositionPieces();
            _highlightLayer.SetGeometry(_geometry);
        }

        public void RenderPosition(IBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            EnsureBuilt();
            ReleaseAllPieceViews();

            for (int i = 0; i < Square.Count; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board[square];

                if (piece.IsSome)
                {
                    SpawnPieceView(piece, square);
                }
            }
        }

        public Task AnimateMoveAsync(MoveRecord record)
        {
            EnsureBuilt();

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!isActiveAndEnabled)
            {
                // Without an active MonoBehaviour there is no coroutine host, so fall back to
                // snapping. This keeps teardown and edit-mode use from deadlocking the turn loop.
                ApplyMoveInstantly(record);
                completion.TrySetResult(true);
                return completion.Task;
            }

            StartCoroutine(AnimateMoveRoutine(record, completion));
            return completion.Task;
        }

        public void ShowSelection(Square square)
        {
            _highlightLayer.ClearTransient();
            _highlightLayer.ShowTransient(square, HighlightKind.Selection);
        }

        public void ShowLegalMoves(IReadOnlyList<Move> moves)
        {
            if (moves == null)
            {
                return;
            }

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                HighlightKind kind = move.IsCapture ? HighlightKind.CaptureTarget : HighlightKind.LegalMove;
                _highlightLayer.ShowTransient(move.To, kind);
            }
        }

        public void ShowLastMove(Square from, Square to)
        {
            _highlightLayer.ClearAll();
            _highlightLayer.ShowPersistent(from, HighlightKind.LastMove);
            _highlightLayer.ShowPersistent(to, HighlightKind.LastMove);
        }

        public void ShowCheck(Square kingSquare) =>
            _highlightLayer.ShowPersistent(kingSquare, HighlightKind.Check);

        public void ClearSelectionAndLegalMoves() => _highlightLayer.ClearTransient();

        public void ClearAllHighlights() => _highlightLayer.ClearAll();

        public void SetInteractable(bool interactable)
        {
            if (_inputSource != null)
            {
                _inputSource.IsEnabled = interactable;
            }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnDestroy()
        {
            if (_inputSource != null)
            {
                _inputSource.SquarePressed -= OnSquarePressed;
            }
        }

        private void EnsureBuilt()
        {
            if (_isBuilt)
            {
                return;
            }

            if (_theme == null)
            {
                Debug.LogError($"{nameof(BoardView)} needs a {nameof(BoardTheme)} before it can render.", this);
                return;
            }

            _geometry = new BoardGeometry(_theme.SquareSize, Orientation);

            _squareRoot = EnsureChild(_squareRoot, "Squares");
            _pieceRoot = EnsureChild(_pieceRoot, "Pieces");

            if (_highlightLayer == null)
            {
                Transform highlightRoot = EnsureChild(null, "Highlights");
                _highlightLayer = highlightRoot.gameObject.AddComponent<HighlightLayer>();
            }

            _highlightLayer.Initialise(_theme, _geometry);

            if (_inputSource == null && _inputSourceBehaviour is IBoardInputSource configured)
            {
                AttachInputSource(configured);
            }

            BuildSquares();
            _isBuilt = true;
        }

        private void BuildSquares()
        {
            for (int i = 0; i < Square.Count; i++)
            {
                Square square = Square.FromIndex(i);

                var instance = new GameObject($"Square {square}", typeof(SpriteRenderer), typeof(SquareView));
                instance.transform.SetParent(_squareRoot, false);

                var view = instance.GetComponent<SquareView>();
                view.Initialise(
                    square,
                    _theme.SquareSprite,
                    _theme.ColorForSquare(square.IsLight),
                    _theme.SquareSize,
                    sortingOrder: 0);

                instance.transform.localPosition = _geometry.ToLocalPosition(square);
                _squareViews[i] = view;
            }
        }

        private void RepositionSquares()
        {
            for (int i = 0; i < _squareViews.Length; i++)
            {
                if (_squareViews[i] != null)
                {
                    _squareViews[i].transform.localPosition = _geometry.ToLocalPosition(Square.FromIndex(i));
                }
            }
        }

        private void RepositionPieces()
        {
            for (int i = 0; i < _pieceViewsBySquare.Length; i++)
            {
                PieceView view = _pieceViewsBySquare[i];
                if (view != null)
                {
                    view.SnapTo(view.Square, _geometry.ToLocalPosition(view.Square));
                }
            }
        }

        private IEnumerator AnimateMoveRoutine(MoveRecord record, TaskCompletionSource<bool> completion)
        {
            Move move = record.Move;
            PieceView mover = _pieceViewsBySquare[move.From.Index];

            if (mover == null)
            {
                // The view has drifted from the model, which can happen after an undo raced with
                // an animation. A full rebuild is the honest recovery.
                ApplyMoveInstantly(record);
                completion.TrySetResult(true);
                yield break;
            }

            RemoveCapturedPiece(record);

            // Lift the moving piece above the others so it never slides underneath one.
            mover.SetSortingOrder(PieceSortingOrder + 1);

            _pieceViewsBySquare[move.From.Index] = null;
            _pieceViewsBySquare[move.To.Index] = mover;

            if (move.IsCastle)
            {
                StartCoroutine(AnimateCastlingRook(record.MovingColor, move.IsKingSideCastle));
            }

            yield return mover.AnimateTo(
                move.To,
                _geometry.ToLocalPosition(move.To),
                _theme.MoveAnimationDuration,
                _theme.MoveAnimationCurve);

            mover.SetSortingOrder(PieceSortingOrder);

            if (move.IsPromotion)
            {
                Piece promoted = Piece.Create(record.MovingColor, move.PromotionPieceType);
                mover.SetPiece(promoted, SpriteFor(promoted), _theme.SquareSize);
            }

            completion.TrySetResult(true);
        }

        private IEnumerator AnimateCastlingRook(PieceColor color, bool kingSide)
        {
            ChessBoard.GetCastlingRookSquares(color, kingSide, out Square from, out Square to);

            PieceView rook = _pieceViewsBySquare[from.Index];
            if (rook == null)
            {
                yield break;
            }

            _pieceViewsBySquare[from.Index] = null;
            _pieceViewsBySquare[to.Index] = rook;

            yield return rook.AnimateTo(
                to,
                _geometry.ToLocalPosition(to),
                _theme.MoveAnimationDuration,
                _theme.MoveAnimationCurve);
        }

        private void ApplyMoveInstantly(MoveRecord record)
        {
            RemoveCapturedPiece(record);

            Move move = record.Move;
            PieceView mover = _pieceViewsBySquare[move.From.Index];
            if (mover == null)
            {
                return;
            }

            _pieceViewsBySquare[move.From.Index] = null;
            _pieceViewsBySquare[move.To.Index] = mover;
            mover.SnapTo(move.To, _geometry.ToLocalPosition(move.To));

            if (move.IsPromotion)
            {
                Piece promoted = Piece.Create(record.MovingColor, move.PromotionPieceType);
                mover.SetPiece(promoted, SpriteFor(promoted), _theme.SquareSize);
            }

            if (move.IsCastle)
            {
                ChessBoard.GetCastlingRookSquares(record.MovingColor, move.IsKingSideCastle, out Square from, out Square to);
                PieceView rook = _pieceViewsBySquare[from.Index];
                if (rook != null)
                {
                    _pieceViewsBySquare[from.Index] = null;
                    _pieceViewsBySquare[to.Index] = rook;
                    rook.SnapTo(to, _geometry.ToLocalPosition(to));
                }
            }
        }

        private void RemoveCapturedPiece(in MoveRecord record)
        {
            if (!record.IsCapture || !record.CapturedPieceSquare.IsValid)
            {
                return;
            }

            // For en passant the captured pawn is not on the destination square, which is why the
            // record carries its square explicitly rather than assuming Move.To.
            ReleasePieceView(record.CapturedPieceSquare);
        }

        private void SpawnPieceView(Piece piece, Square square)
        {
            PieceView view = RentPieceView();
            view.Initialise(
                piece,
                square,
                SpriteFor(piece),
                _geometry.ToLocalPosition(square),
                _theme.SquareSize,
                PieceSortingOrder);

            view.gameObject.SetActive(true);
            _pieceViewsBySquare[square.Index] = view;
        }

        private PieceView RentPieceView()
        {
            for (int i = 0; i < _pieceViewPool.Count; i++)
            {
                if (!_pieceViewPool[i].gameObject.activeSelf)
                {
                    return _pieceViewPool[i];
                }
            }

            var instance = new GameObject("Piece", typeof(SpriteRenderer), typeof(PieceView));
            instance.transform.SetParent(_pieceRoot, false);
            instance.SetActive(false);

            var created = instance.GetComponent<PieceView>();
            _pieceViewPool.Add(created);
            return created;
        }

        private void ReleasePieceView(Square square)
        {
            PieceView view = _pieceViewsBySquare[square.Index];
            if (view == null)
            {
                return;
            }

            view.gameObject.SetActive(false);
            _pieceViewsBySquare[square.Index] = null;
        }

        private void ReleaseAllPieceViews()
        {
            for (int i = 0; i < _pieceViewsBySquare.Length; i++)
            {
                if (_pieceViewsBySquare[i] != null)
                {
                    _pieceViewsBySquare[i].gameObject.SetActive(false);
                    _pieceViewsBySquare[i] = null;
                }
            }
        }

        private void AttachInputSource(IBoardInputSource inputSource)
        {
            if (inputSource == null || ReferenceEquals(inputSource, _inputSource))
            {
                return;
            }

            if (_inputSource != null)
            {
                _inputSource.SquarePressed -= OnSquarePressed;
            }

            _inputSource = inputSource;
            _inputSource.SquarePressed += OnSquarePressed;
        }

        private void OnSquarePressed(Square square) => SquareClicked?.Invoke(square);

        private Transform EnsureChild(Transform existing, string childName)
        {
            if (existing != null)
            {
                return existing;
            }

            Transform found = transform.Find(childName);
            if (found != null)
            {
                return found;
            }

            var created = new GameObject(childName);
            created.transform.SetParent(transform, false);
            return created.transform;
        }

        private Sprite SpriteFor(Piece piece) => _pieceSprites != null ? _pieceSprites.Get(piece) : null;
    }
}
