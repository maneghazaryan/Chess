using System;
using System.Collections;
using System.Collections.Generic;

namespace Chess.Core.Primitives
{
    /// <summary>
    /// A growable, reusable buffer of moves.
    /// </summary>
    /// <remarks>
    /// The search generates a move list at every node, so allocating a fresh <see cref="List{T}"/>
    /// each time would dominate the profile. This type is designed to be cleared and refilled, and
    /// its <see cref="GetEnumerator"/> returns a struct enumerator so <c>foreach</c> does not box.
    /// </remarks>
    public sealed class MoveList : IReadOnlyList<Move>
    {
        private const int DefaultCapacity = 64;

        private Move[] _moves;

        public MoveList(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _moves = new Move[capacity];
        }

        public int Count { get; private set; }

        public Move this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _moves[index];
            }
            set
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                _moves[index] = value;
            }
        }

        public void Add(in Move move)
        {
            if (Count == _moves.Length)
            {
                Array.Resize(ref _moves, _moves.Length * 2);
            }

            _moves[Count++] = move;
        }

        public void Clear() => Count = 0;

        /// <summary>Drops everything past the first <paramref name="count"/> moves.</summary>
        public void Truncate(int count)
        {
            if ((uint)count > (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
        }

        public bool Contains(in Move move)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_moves[i] == move)
                {
                    return true;
                }
            }

            return false;
        }

        public void Swap(int a, int b)
        {
            (_moves[a], _moves[b]) = (_moves[b], _moves[a]);
        }

        public void CopyTo(MoveList destination)
        {
            destination.Clear();
            for (int i = 0; i < Count; i++)
            {
                destination.Add(_moves[i]);
            }
        }

        public Move[] ToArray()
        {
            var result = new Move[Count];
            Array.Copy(_moves, result, Count);
            return result;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Move> IEnumerable<Move>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<Move>
        {
            private readonly MoveList _list;
            private int _index;

            internal Enumerator(MoveList list)
            {
                _list = list;
                _index = -1;
            }

            public Move Current => _list._moves[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _list.Count;

            public void Reset() => _index = -1;

            public void Dispose()
            {
            }
        }
    }
}
