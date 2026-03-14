using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    private Dictionary<CropData, int> seeds = new();
    private Dictionary<CropData, int> harvested = new();

    public CropData SelectedSeed { get; private set; }

    void Awake() => Instance = this;

    // ── Seeds ─────────────────────────────────────────────────

    public void AddSeed(CropData crop, int amount = 1)
    {
        if (!seeds.ContainsKey(crop)) seeds[crop] = 0;
        seeds[crop] += amount;

        if (SelectedSeed == null)
            SelectedSeed = crop;

        Debug.Log($"Inventory: {crop.cropName} seeds x{seeds[crop]}. Selected: {SelectedSeed.cropName}");

        if (SeedHotbar.Instance != null) SeedHotbar.Instance.Refresh();
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
        {
            SelectedSeed = seeds.FirstOrDefault(k => k.Value > 0).Key;
            Debug.Log(SelectedSeed != null
                ? $"Auto-switched selection to {SelectedSeed.cropName}"
                : "No seeds left.");
        }

        if (SeedHotbar.Instance != null) SeedHotbar.Instance.Refresh();

        return true;
    }

    public void SelectSeed(CropData crop)
    {
        if (GetSeedCount(crop) <= 0)
        {
            Debug.Log($"No {crop.cropName} seeds to select.");
            return;
        }
        SelectedSeed = crop;
        Debug.Log($"Selected: {SelectedSeed.cropName}");
    }

    public int GetSeedCount(CropData crop) =>
        seeds.ContainsKey(crop) ? seeds[crop] : 0;

    public Dictionary<CropData, int> GetAllSeeds() => seeds;

    // ── Harvest ───────────────────────────────────────────────

    public void AddHarvest(CropData crop, int amount = 1)
    {
        if (!harvested.ContainsKey(crop)) harvested[crop] = 0;
        harvested[crop] += amount;
    }

    public bool UseHarvest(CropData crop)
    {
        if (!harvested.ContainsKey(crop) || harvested[crop] <= 0) return false;
        harvested[crop]--;
        return true;
    }

    public int GetHarvestCount(CropData crop) =>
        harvested.ContainsKey(crop) ? harvested[crop] : 0;

    public Dictionary<CropData, int> GetAllHarvested() => harvested;
}