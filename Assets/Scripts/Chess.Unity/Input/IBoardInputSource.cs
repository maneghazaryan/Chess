using System;
using Chess.Core.Primitives;

namespace Chess.Unity.Input
{
    /// <summary>
    /// Turns raw device input into board squares.
    /// </summary>
    /// <remarks>
    /// Isolating this means adding touch, gamepad or keyboard navigation is a new implementation
    /// rather than a change to the board view.
    /// </remarks>
    public interface IBoardInputSource
    {
        event Action<Square> SquarePressed;

        bool IsEnabled { get; set; }
    }
}
