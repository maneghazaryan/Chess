using Chess.Core.Primitives;
using UnityEngine;

namespace Chess.Unity.Config
{
    /// <summary>
    /// Maps each of the twelve pieces to its sprite.
    /// </summary>
    /// <remarks>
    /// An asset rather than a folder convention or a <c>Resources.Load</c> call, so swapping piece
    /// sets is a matter of assigning a different asset and any missing sprite is visible in the
    /// inspector instead of failing at runtime.
    /// <para>
    /// Author sprites at 256x256 with transparency and import them as Sprite (2D and UI) with
    /// Pixels Per Unit 256, so that one sprite fills exactly one board square.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "PieceSpriteSet", menuName = "Chess/Piece Sprite Set")]
    public sealed class PieceSpriteSet : ScriptableObject
    {
        [Header("White")]
        [SerializeField] private Sprite _whitePawn;
        [SerializeField] private Sprite _whiteKnight;
        [SerializeField] private Sprite _whiteBishop;
        [SerializeField] private Sprite _whiteRook;
        [SerializeField] private Sprite _whiteQueen;
        [SerializeField] private Sprite _whiteKing;

        [Header("Black")]
        [SerializeField] private Sprite _blackPawn;
        [SerializeField] private Sprite _blackKnight;
        [SerializeField] private Sprite _blackBishop;
        [SerializeField] private Sprite _blackRook;
        [SerializeField] private Sprite _blackQueen;
        [SerializeField] private Sprite _blackKing;

        public Sprite Get(Piece piece)
        {
            return piece.IsNone ? null : Get(piece.Color, piece.Type);
        }

        public Sprite Get(PieceColor color, PieceType type)
        {
            return color == PieceColor.White ? GetWhite(type) : GetBlack(type);
        }

        /// <summary>Reports which sprites are still unassigned, for the installer to warn about.</summary>
        public bool IsComplete(out string missingDescription)
        {
            var missing = new System.Text.StringBuilder();

            for (PieceType type = PieceType.Pawn; type <= PieceType.King; type++)
            {
                if (GetWhite(type) == null)
                {
                    missing.Append($"White {type}, ");
                }

                if (GetBlack(type) == null)
                {
                    missing.Append($"Black {type}, ");
                }
            }

            missingDescription = missing.ToString().TrimEnd(' ', ',');
            return missing.Length == 0;
        }

        private Sprite GetWhite(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return _whitePawn;
                case PieceType.Knight: return _whiteKnight;
                case PieceType.Bishop: return _whiteBishop;
                case PieceType.Rook: return _whiteRook;
                case PieceType.Queen: return _whiteQueen;
                case PieceType.King: return _whiteKing;
                default: return null;
            }
        }

        private Sprite GetBlack(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return _blackPawn;
                case PieceType.Knight: return _blackKnight;
                case PieceType.Bishop: return _blackBishop;
                case PieceType.Rook: return _blackRook;
                case PieceType.Queen: return _blackQueen;
                case PieceType.King: return _blackKing;
                default: return null;
            }
        }
    }
}
