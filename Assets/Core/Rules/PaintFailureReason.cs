namespace ColorFlow.Core
{
    /// <summary>Raison pour laquelle une peinture a été refusée.</summary>
    public enum PaintFailureReason
    {
        None = 0,
        NoCreature = 1,
        UnknownCreature = 2,
        NoCompatiblePixel = 3,
        LevelComplete = 4,
        GameNotPlaying = 5
    }
}
