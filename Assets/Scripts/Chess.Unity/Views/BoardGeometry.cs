using Chess.Core.Primitives;
using UnityEngine;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Converts between board squares and local positions on the board object.
    /// </summary>
    /// <remarks>
    /// A value type shared by the renderer and the input source, so that "where is e4" has exactly
    /// one answer. Positions are local to the board transform, which means the board as a whole
    /// can be moved, rotated or scaled in the scene without any of this arithmetic changing.
    /// </remarks>
    public readonly struct BoardGeometry
    {
        private const float BoardCentre = (Square.BoardSize - 1) * 0.5f;

        public BoardGeometry(float squareSize, BoardOrientation orientation)
        {
            SquareSize = squareSize <= 0f ? 1f : squareSize;
            Orientation = orientation;
        }

        public float SquareSize { get; }

        public BoardOrientation Orientation { get; }

        public BoardGeometry WithOrientation(BoardOrientation orientation) =>
            new BoardGeometry(SquareSize, orientation);

        public Vector3 ToLocalPosition(Square square)
        {
            if (!square.IsValid)
            {
                return Vector3.zero;
            }

            // Flipping the board is a change of viewpoint, not a change of coordinates: the model
            // always calls a1 a1, and only this mapping knows which corner it is drawn in.
            float column = Orientation == BoardOrientation.WhiteAtBottom
                ? square.File
                : Square.BoardSize - 1 - square.File;

            float row = Orientation == BoardOrientation.WhiteAtBottom
                ? square.Rank
                : Square.BoardSize - 1 - square.Rank;

            return new Vector3((column - BoardCentre) * SquareSize, (row - BoardCentre) * SquareSize, 0f);
        }

        public Square FromLocalPosition(Vector3 localPosition)
        {
            int column = Mathf.RoundToInt(localPosition.x / SquareSize + BoardCentre);
            int row = Mathf.RoundToInt(localPosition.y / SquareSize + BoardCentre);

            if (!Square.IsCoordinateOnBoard(column) || !Square.IsCoordinateOnBoard(row))
            {
                return Square.None;
            }

            int file = Orientation == BoardOrientation.WhiteAtBottom ? column : Square.BoardSize - 1 - column;
            int rank = Orientation == BoardOrientation.WhiteAtBottom ? row : Square.BoardSize - 1 - row;

            return Square.FromFileRank(file, rank);
        }

        /// <summary>Edge length of the whole board, for framing the camera.</summary>
        public float BoardExtent => Square.BoardSize * SquareSize;
    }
}
