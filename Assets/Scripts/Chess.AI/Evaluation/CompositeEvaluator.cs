using System;
using System.Collections.Generic;
using System.Linq;
using Chess.Core.Board;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// Sums a set of weighted evaluation terms.
    /// </summary>
    /// <remarks>
    /// Lets an evaluation be assembled from parts rather than written as one function, so a new
    /// term (king safety, pawn structure, mobility) can be added and weighted without touching
    /// anything that already works.
    /// </remarks>
    public sealed class CompositeEvaluator : IPositionEvaluator
    {
        private readonly IPositionEvaluator[] _terms;
        private readonly int[] _weights;

        public CompositeEvaluator(params IPositionEvaluator[] terms)
            : this(terms, terms?.Select(_ => 1).ToArray())
        {
        }

        public CompositeEvaluator(IReadOnlyList<IPositionEvaluator> terms, IReadOnlyList<int> weights)
        {
            if (terms == null)
            {
                throw new ArgumentNullException(nameof(terms));
            }

            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            if (terms.Count != weights.Count)
            {
                throw new ArgumentException("Each evaluation term needs exactly one weight.", nameof(weights));
            }

            _terms = terms.ToArray();
            _weights = weights.ToArray();
        }

        public int Evaluate(IBoard board)
        {
            int total = 0;

            for (int i = 0; i < _terms.Length; i++)
            {
                total += _terms[i].Evaluate(board) * _weights[i];
            }

            return total;
        }
    }
}
