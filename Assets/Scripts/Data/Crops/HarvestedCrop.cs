using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HarvestedCrop
{
    public CropData           Source;
    public Rank               Rank;
    public bool               IsEvolved;
    public List<MutationData> Mutations         = new List<MutationData>();
    public int                RelationshipLevel;

    public int SellValue
    {
        get
        {
            float multiplier = RankUtility.SellMultiplier(Rank);
            multiplier *= IsEvolved ? 1.5f : 1f;
            foreach (var m in Mutations)
                multiplier *= m.sellMultiplier;
            return (int)(Source.sellValue * multiplier);
        }
    }

    public string DisplayName =>
        IsEvolved ? $"Evolved {Source.cropName}" : Source.cropName;

    public string MutationDisplay
    {
        get
        {
            if (Mutations == null || Mutations.Count == 0) return "";
            var names = new List<string>();
            foreach (var m in Mutations) names.Add(m.mutationName);
            return string.Join(" + ", names);
        }
    }

    public HarvestedCrop(CropData source, Rank rank, bool isEvolved)
    {
        Source            = source;
        Rank              = rank;
        IsEvolved         = isEvolved;
        RelationshipLevel = 0;
    }
}