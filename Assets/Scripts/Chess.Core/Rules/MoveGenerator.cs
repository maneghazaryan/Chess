using System;
using System.Collections.Generic;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Produces legal moves by dispatching to a per-piece-type generator and then filtering for
    /// king safety.
    /// </summary>
    /// <remarks>
    /// This class contains no knowledge of how any individual piece moves. Adding a piece type is
    /// a matter of writing an <see cref="IPieceMoveGenerator"/> and passing it to the constructor.
    /// </remarks>
    public sealed class MoveGenerator : IMoveGenerator
    {
        private readonly IPieceMoveGenerator[] _generatorsByPieceType = new IPieceMoveGenerator[7];
        private readonly ILegalityFilter _legalityFilter;

        // Reused across calls to keep move generation allocation-free inside the search.
        private readonly MoveList _scratch = new MoveList();

        public MoveGenerator(IEnumerable<IPieceMoveGenerator> pieceGenerators, ILegalityFilter legalityFilter)
        {
            if (pieceGenerators == null)
            {
                throw new ArgumentNullException(nameof(pieceGenerators));
            }

            _legalityFilter = legalityFilter ?? throw new ArgumentNullException(nameof(legalityFilter));

            foreach (IPieceMoveGenerator generator in pieceGenerators)
            {
                int index = (int)generator.PieceType;
                if (_generatorsByPieceType[index] != null)
                {
                    throw new ArgumentException(
                        $"Two generators were registered for {generator.PieceType}.", nameof(pieceGenerators));
                }

                _generatorsByPieceType[index] = generator;
            }

            for (int type = (int)PieceType.Pawn; type <= (int)PieceType.King; type++)
            {
                if (_generatorsByPieceType[type] == null)
                {
                    throw new ArgumentException(
                        $"No generator was registered for {(PieceType)type}.", nameof(pieceGenerators));
                }
            }
        }

        /// <summary>Builds a generator wired with the standard chess pieces.</summary>
        public static MoveGenerator CreateStandard(IAttackService attackService)
        {
            if (attackService == null)
            {
                throw new ArgumentNullException(nameof(attackService));
            }

            var pieceGenerators = new IPieceMoveGenerator[]
            {
                new PawnMoveGenerator(),
                new KnightMoveGenerator(),
                new BishopMoveGenerator(),
                new RookMoveGenerator(),
                new QueenMoveGenerator(),
                new KingMoveGenerator(attackService)
            };

            return new MoveGenerator(pieceGenerators, new LegalityFilter(attackService));
        }

        public void GenerateLegalMoves(IMutableBoard board, MoveList moves, MoveGenerationMode mode = MoveGenerationMode.All)
        {
            moves.Clear();
            GeneratePseudoLegalMoves(board, mode, moves);
            _legalityFilter.FilterInPlace(board, moves);
        }

        public void GenerateLegalMovesFrom(IMutableBoard board, Square from, MoveList moves)
        {
            moves.Clear();

            Piece piece = board[from];
            if (piece.IsNone || piece.Color != board.SideToMove)
            {
                return;
            }

            _generatorsByPieceType[(int)piece.Type]
                .GeneratePseudoLegalMoves(board, from, MoveGenerationMode.All, moves);

            _legalityFilter.FilterInPlace(board, moves);
        }

        public bool HasAnyLegalMove(IMutableBoard board)
        {
            _scratch.Clear();
            GeneratePseudoLegalMoves(board, MoveGenerationMode.All, _scratch);

            // Stop at the first legal move rather than filtering the whole list: in a checkmate
            // search this is called constantly and usually only needs a handful of probes.
            for (int i = 0; i < _scratch.Count; i++)
            {
                if (_legalityFilter.IsLegal(board, _scratch[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void GeneratePseudoLegalMoves(IMutableBoard board, MoveGenerationMode mode, MoveList moves)
        {
            IReadOnlyList<Square> origins = board.GetOccupiedSquares(board.SideToMove);

            // Indexed rather than foreach-ed: enumerating through the interface would box the
            // enumerator once per node, which is pure garbage in the search's hottest loop.
            for (int i = 0; i < origins.Count; i++)
            {
                Square from = origins[i];
                Piece piece = board[from];
                _generatorsByPieceType[(int)piece.Type].GeneratePseudoLegalMoves(board, from, mode, moves);
            }
        }
    }
}
