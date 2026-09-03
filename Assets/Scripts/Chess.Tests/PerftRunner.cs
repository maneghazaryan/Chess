using Chess.Core.Board;
using Chess.Core.Notation;
using Chess.Core.Primitives;
using Chess.Core.Rules;

namespace Chess.Tests
{
    /// <summary>
    /// Counts the leaf nodes of the legal move tree to a given depth.
    /// </summary>
    /// <remarks>
    /// Perft is the standard correctness test for a chess engine. Published node counts exist for
    /// well-known positions, and because a single missing or spurious move at any depth changes
    /// the total, matching them is strong evidence that move generation, make and unmake are all
    /// exactly right. It catches en passant, castling, promotion and pin bugs that hand-written
    /// unit tests routinely miss.
    /// </remarks>
    public static class PerftRunner
    {
        public static long Run(string fen, int depth)
        {
            ChessBoard board = FenSerializer.Parse(fen);
            IAttackService attackService = new AttackService();
            MoveGenerator generator = MoveGenerator.CreateStandard(attackService);

            var pool = new MoveList[depth + 1];
            for (int i = 0; i <= depth; i++)
            {
                pool[i] = new MoveList(256);
            }

            return Count(generator, board, depth, pool);
        }

        /// <summary>
        /// Per-move node counts from the root, which is how a failing total is localised: compare
        /// against a reference engine's divide output and the disagreeing move names the bug.
        /// </summary>
        public static string Divide(string fen, int depth)
        {
            ChessBoard board = FenSerializer.Parse(fen);
            IAttackService attackService = new AttackService();
            MoveGenerator generator = MoveGenerator.CreateStandard(attackService);

            var rootMoves = new MoveList(256);
            generator.GenerateLegalMoves(board, rootMoves);
            Move[] snapshot = rootMoves.ToArray();

            var pool = new MoveList[depth + 1];
            for (int i = 0; i <= depth; i++)
            {
                pool[i] = new MoveList(256);
            }

            var report = new System.Text.StringBuilder();
            long total = 0;

            foreach (Move move in snapshot)
            {
                board.MakeMove(move);
                long nodes = Count(generator, board, depth - 1, pool);
                board.UnmakeMove();

                total += nodes;
                report.AppendLine($"{move.ToUci()}: {nodes}");
            }

            report.AppendLine($"total: {total}");
            return report.ToString();
        }

        private static long Count(MoveGenerator generator, IMutableBoard board, int depth, MoveList[] pool)
        {
            if (depth == 0)
            {
                return 1;
            }

            MoveList moves = pool[depth];
            generator.GenerateLegalMoves(board, moves);

            // At depth one the move count is the answer, so the last ply of make/unmake is skipped.
            if (depth == 1)
            {
                return moves.Count;
            }

            // Safe to iterate the pooled list directly: the recursive call rents the buffer for
            // depth - 1, so nothing below this node can disturb it.
            long nodes = 0;

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                board.MakeMove(move);
                nodes += Count(generator, board, depth - 1, pool);
                board.UnmakeMove();
            }

            return nodes;
        }
    }
}
