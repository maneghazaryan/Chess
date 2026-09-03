using System;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// How much chess understanding an engine is given.
    /// </summary>
    public enum EvaluationProfile
    {
        /// <summary>Material only. Will not hang pieces, has no idea where they belong.</summary>
        MaterialOnly,

        /// <summary>Material plus midgame piece-square tables, with no phase blending.</summary>
        MaterialAndPosition,

        /// <summary>The full evaluation, interpolated between midgame and endgame weights.</summary>
        Tapered
    }

    /// <summary>
    /// Turns a profile into a configured evaluator, so that difficulty can be described as data
    /// (in code defaults or in a designer-authored asset) rather than as a chain of constructor
    /// calls repeated in each place.
    /// </summary>
    public static class EvaluatorFactory
    {
        public static IPositionEvaluator Create(EvaluationProfile profile)
        {
            switch (profile)
            {
                case EvaluationProfile.MaterialOnly:
                    return new MaterialEvaluator();

                case EvaluationProfile.MaterialAndPosition:
                    return new CompositeEvaluator(new MaterialEvaluator(), new PieceSquareTableEvaluator());

                case EvaluationProfile.Tapered:
                    return new TaperedEvaluator();

                default:
                    throw new ArgumentOutOfRangeException(nameof(profile), profile, null);
            }
        }
    }
}
