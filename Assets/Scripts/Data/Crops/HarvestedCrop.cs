using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class HarvestedCrop
{
    public CropData           Source;
    public List<MutationData> Mutations = new List<MutationData>();

    public int SellValue
    {
        get
        {
            float value = Source.sellValue;
            foreach (var m in Mutations)
                value *= m.sellMultiplier;
            return Mathf.Max(1, (int)value);
        }
    }

    public string DisplayName => Source.cropName;

    public string MutationDisplay
    {
        get
        {
            if (Mutations == null || Mutations.Count == 0) return "";
            return string.Join(" + ", Mutations.Select(m => m.mutationName));
        }
    }

    public HarvestedCrop(CropData source)
    {
        Source = source;
    }
}