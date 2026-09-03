using System;
using System.Collections.Generic;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Game
{
    /// <summary>
    /// The Model root. Owns the position, the rules and the history, and is the only chess type
    /// that controllers are allowed to touch.
    /// </summary>
    /// <remarks>
    /// Deliberately free of any Unity dependency: it raises plain C# events and knows nothing
    /// about who is playing, how the board is drawn, or whether an opponent is human.
    /// </remarks>
    public interface IChessGame
    {
        IBoard Position { get; }

        PieceColor SideToMove { get; }

        GameStatus Status { get; }

        GameResult Result { get; }

        bool IsGameOver { get; }

        IReadOnlyList<MoveRecord> History { get; }

        /// <summary>Every legal move in the current position.</summary>
        IReadOnlyList<Move> LegalMoves { get; }

        /// <summary>Legal moves whose origin is <paramref name="from"/>, for move highlighting.</summary>
        IReadOnlyList<Move> GetLegalMovesFrom(Square from);

        /// <summary>
        /// Finds the legal move matching an origin and destination. Promotions are ambiguous by
        /// this pair alone, so <paramref name="promotionType"/> disambiguates them.
        /// </summary>
        bool TryFindMove(Square from, Square to, PieceType promotionType, out Move move);

        /// <summary>Plays the move if it is legal. Returns false and changes nothing otherwise.</summary>
        bool TryMakeMove(in Move move);

        bool CanUndo { get; }

        bool TryUndoLastMove();

        void Reset();

        void LoadFen(string fen);

        string ToFen();

        event Action<MoveRecord> MoveMade;

        event Action<MoveRecord> MoveUndone;

        event Action<GameStatus> StatusChanged;

        event Action GameReset;
    }
}
