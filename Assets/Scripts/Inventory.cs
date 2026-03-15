using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    private Dictionary<CropData, int> seeds     = new();
    private List<HarvestedCrop>       harvested  = new();

    public CropData SelectedSeed { get; private set; }

    void Awake() => Instance = this;

    // ── Seeds ─────────────────────────────────────────────────

    public void AddSeed(CropData crop, int amount = 1)
    {
        if (!seeds.ContainsKey(crop)) seeds[crop] = 0;
        seeds[crop] += amount;

        if (SelectedSeed == null) SelectedSeed = crop;

        EventBus.Raise_SeedAdded(crop);
    }

    public bool UseSeed(CropData crop)
    {
        if (!seeds.ContainsKey(crop) || seeds[crop] <= 0)
        {
            Debug.Log($"No {crop.cropName} seeds left!");
            return false;
        }

        seeds[crop]--;

        if (seeds[crop] <= 0 && SelectedSeed == crop)
            SelectedSeed = seeds.FirstOrDefault(k => k.Value > 0).Key;

        EventBus.Raise_SeedUsed(crop);
        return true;
    }

    public void SelectSeed(CropData crop)
    {
        if (GetSeedCount(crop) <= 0) return;
        SelectedSeed = crop;
    }

    public int GetSeedCount(CropData crop) =>
        seeds.ContainsKey(crop) ? seeds[crop] : 0;

    public Dictionary<CropData, int> GetAllSeeds() => seeds;

    // ── Harvest ───────────────────────────────────────────────

    public void AddHarvest(HarvestedCrop crop)
    {
        harvested.Add(crop);
        EventBus.Raise_CropHarvested(crop);
    }

    public List<HarvestedCrop> GetAllHarvested() => harvested;

    public bool RemoveHarvest(HarvestedCrop crop)
    {
        return harvested.Remove(crop);
    }

    // Group harvested crops by type and rank for the sell UI
    public Dictionary<(CropData, Rank, bool), List<HarvestedCrop>> GetGroupedHarvest()
    {
        var groups = new Dictionary<(CropData, Rank, bool), List<HarvestedCrop>>();

        foreach (var crop in harvested)
        {
            var key = (crop.Source, crop.Rank, crop.IsMutated);
            if (!groups.ContainsKey(key)) groups[key] = new List<HarvestedCrop>();
            groups[key].Add(crop);
        }

        return groups;
    }
}