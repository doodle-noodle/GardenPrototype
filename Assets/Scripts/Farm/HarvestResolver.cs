using UnityEngine;

public static class HarvestResolver
{
    public static HarvestedCrop Resolve(CropData crop, float extraMutationChance = 0f)
    {
        var harvest = new HarvestedCrop(crop);

        if (crop.possibleHarvestMutations != null && crop.possibleHarvestMutations.Count > 0)
        {
            float chance = Mathf.Clamp01(crop.mutationChance + extraMutationChance);
            if (Random.value < chance)
            {
                var mutation = crop.possibleHarvestMutations[
                    Random.Range(0, crop.possibleHarvestMutations.Count)];
                if (mutation != null && !harvest.Mutations.Contains(mutation))
                    harvest.Mutations.Add(mutation);
            }
        }

        return harvest;
    }
}