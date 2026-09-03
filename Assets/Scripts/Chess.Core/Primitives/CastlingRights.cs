using System;

namespace Chess.Core.Primitives
{
    [Flags]
    public enum CastlingRights : byte
    {
        None = 0,
        WhiteKingSide = 1 << 0,
        WhiteQueenSide = 1 << 1,
        BlackKingSide = 1 << 2,
        BlackQueenSide = 1 << 3,

        White = WhiteKingSide | WhiteQueenSide,
        Black = BlackKingSide | BlackQueenSide,
        All = White | Black
    }

    public static class CastlingRightsExtensions
    {
        public static CastlingRights KingSideFor(PieceColor color)
        {
            return color == PieceColor.White ? CastlingRights.WhiteKingSide : CastlingRights.BlackKingSide;
        }

        public static CastlingRights QueenSideFor(PieceColor color)
        {
            return color == PieceColor.White ? CastlingRights.WhiteQueenSide : CastlingRights.BlackQueenSide;
        }

        public static CastlingRights AllFor(PieceColor color)
        {
            return color == PieceColor.White ? CastlingRights.White : CastlingRights.Black;
        }

        public static bool Has(this CastlingRights rights, CastlingRights flag)
        {
            return (rights & flag) == flag && flag != CastlingRights.None;
        }
    }
}
