using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class HarvestedCrop
{
    public CropData           Source;
    public bool               IsEvolved;    // hidden — reserved for future evolution feature
    public List<MutationData> Mutations     = new List<MutationData>();

    // Final Value = Base × IsEvolved bonus × M1.sellMultiplier × M2.sellMultiplier × ...
    public int SellValue
    {
        get
        {
            float value = Source.sellValue;
            if (IsEvolved) value *= 1.5f;
            foreach (var m in Mutations)
                value *= m.sellMultiplier;
            return Mathf.Max(1, (int)value);
        }
    }

    // Never shows "Evolved" — IsEvolved is fully hidden from players
    public string DisplayName => Source.cropName;

    public string MutationDisplay
    {
        get
        {
            if (Mutations == null || Mutations.Count == 0) return "";
            return string.Join(" + ", Mutations.Select(m => m.mutationName));
        }
    }

    public HarvestedCrop(CropData source, bool isEvolved)
    {
        Source    = source;
        IsEvolved = isEvolved;
    }
}
