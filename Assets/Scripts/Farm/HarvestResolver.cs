using UnityEngine;

public static class HarvestResolver
{
    public static HarvestedCrop Resolve(CropData crop, float evolutionBonus = 0f)
    {
        bool evolved = Random.value < Mathf.Clamp01(crop.mutationChance + evolutionBonus);
        CropData finalCrop = (evolved && crop.mutatesInto != null)
            ? crop.mutatesInto : crop;
        return new HarvestedCrop(finalCrop, evolved);
    }
}