using UnityEngine;

public static class HarvestResolver
{
    // Central place for all harvest outcome logic.
    // Add watering, fertiliser, season, weather bonuses here in the future.
    public static HarvestedCrop Resolve(CropData crop,
        float mutationBonus = 0f, float rankBonus = 0f)
    {
        bool mutated = Random.value < Mathf.Clamp01(crop.mutationChance + mutationBonus);

        Rank rank = RankUtility.RollRank();

        CropData finalCrop = (mutated && crop.mutatesInto != null)
            ? crop.mutatesInto
            : crop;

        return new HarvestedCrop(finalCrop, rank, mutated);
    }
}