namespace Chess.AI
{
    /// <summary>
    /// How strong the computer opponent should be.
    /// </summary>
    /// <remarks>
    /// Lives in the AI assembly rather than the Unity one because difficulty is a property of the
    /// engine, not of the presentation. Strength is varied by changing how deep the search looks
    /// and how much the evaluation understands, never by making the engine play deliberate
    /// blunders, which reads as erratic rather than weak.
    /// </remarks>
    public enum AiDifficulty
    {
        Easy,
        Medium,
        Hard
    }
}
