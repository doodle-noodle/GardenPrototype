// Represents one harvested crop instance with its own rank and mutation state.
// This is a plain data class — no MonoBehaviour.
[System.Serializable]
public class HarvestedCrop
{
    public CropData Source;
    public Rank     Rank;
    public bool     IsMutated;
    public int      RelationshipLevel;  // used by dating mechanics later

    public int SellValue =>
        (int)(Source.sellValue * RankUtility.SellMultiplier(Rank) * (IsMutated ? 1.5f : 1f));

    public string DisplayName =>
        IsMutated ? $"Mutant {Source.cropName}" : Source.cropName;

    public HarvestedCrop(CropData source, Rank rank, bool isMutated)
    {
        Source            = source;
        Rank              = rank;
        IsMutated         = isMutated;
        RelationshipLevel = 0;
    }
}