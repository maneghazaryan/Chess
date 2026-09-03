using System;
using Chess.Core.Primitives;

namespace Chess.AI.Search
{
    /// <summary>
    /// One reusable <see cref="MoveList"/> per ply.
    /// </summary>
    /// <remarks>
    /// A search visits hundreds of thousands of nodes, each needing a move list. Because the
    /// search is depth-first and single-threaded, only one list per ply is ever live at a time,
    /// so indexing by ply reuses the same handful of buffers for the entire search and reduces
    /// move-list allocation to zero after the first pass.
    /// </remarks>
    public sealed class MoveListPool
    {
        private MoveList[] _lists;

        public MoveListPool(int initialPlies = 32)
        {
            _lists = new MoveList[Math.Max(1, initialPlies)];
        }

        public MoveList Rent(int ply)
        {
            if (ply >= _lists.Length)
            {
                Array.Resize(ref _lists, Math.Max(ply + 1, _lists.Length * 2));
            }

            return _lists[ply] ??= new MoveList(64);
        }
    }
}
