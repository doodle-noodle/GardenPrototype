using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    // ── Capacity ──────────────────────────────────────────────
    public const int MaxSlots = 10;

    // ── State ─────────────────────────────────────────────────
    public InventorySlot[] Slots    { get; private set; }
    public CropData        SelectedSeed { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        Slots    = new InventorySlot[MaxSlots];
        for (int i = 0; i < MaxSlots; i++)
            Slots[i] = new InventorySlot();
    }

    // ── Seeds ─────────────────────────────────────────────────

    public bool AddSeed(CropData crop, int amount = 1)
    {
        // Stack into existing seed slot for this crop
        var existing = FindSlot(InventoryItemType.Seed, crop);
        if (existing != null)
        {
            existing.SeedCount += amount;
            if (SelectedSeed == null) SelectedSeed = crop;
            EventBus.Raise_SeedAdded(crop);
            return true;
        }

        // Claim a new empty slot
        var empty = FindEmptySlot();
        if (empty == null)
        {
            TutorialConsole.Error("Inventory full! Sell some items first.");
            return false;
        }

        empty.Type      = InventoryItemType.Seed;
        empty.Crop      = crop;
        empty.SeedCount = amount;
        if (SelectedSeed == null) SelectedSeed = crop;
        EventBus.Raise_SeedAdded(crop);
        return true;
    }

    public bool UseSeed(CropData crop)
    {
        var slot = FindSlot(InventoryItemType.Seed, crop);
        if (slot == null || slot.SeedCount <= 0)
        {
            TutorialConsole.Warn($"No {crop.cropName} seeds left.");
            return false;
        }

        slot.SeedCount--;
        if (slot.SeedCount <= 0)
        {
            slot.Clear();
            SelectedSeed = Slots
                .FirstOrDefault(s => s.Type == InventoryItemType.Seed)?.Crop;
        }

        EventBus.Raise_SeedUsed(crop);
        return true;
    }

    public void SelectSeed(CropData crop)
    {
        var slot = FindSlot(InventoryItemType.Seed, crop);
        if (slot == null || slot.SeedCount <= 0)
        {
            TutorialConsole.Warn($"No {crop.cropName} seeds in inventory.");
            return;
        }
        SelectedSeed = crop;
        TutorialConsole.Log($"Selected: {crop.cropName} seed.");
    }

    public int GetSeedCount(CropData crop) =>
        FindSlot(InventoryItemType.Seed, crop)?.SeedCount ?? 0;

    public Dictionary<CropData, int> GetAllSeeds() =>
        Slots
            .Where(s => s.Type == InventoryItemType.Seed && s.Crop != null)
            .ToDictionary(s => s.Crop, s => s.SeedCount);

    // ── Harvest ───────────────────────────────────────────────

    public bool AddHarvest(HarvestedCrop crop)
    {
        // Stack into existing harvest slot for this crop type
        var existing = FindSlot(InventoryItemType.Harvest, crop.Source);
        if (existing != null)
        {
            existing.Harvested.Add(crop);
            EventBus.Raise_CropHarvested(crop);
            return true;
        }

        // Claim a new empty slot
        var empty = FindEmptySlot();
        if (empty == null)
        {
            TutorialConsole.Error("Inventory full! Sell some items first.");
            return false;
        }

        empty.Type = InventoryItemType.Harvest;
        empty.Crop = crop.Source;
        empty.Harvested.Add(crop);
        EventBus.Raise_CropHarvested(crop);
        return true;
    }

    public bool RemoveHarvest(HarvestedCrop crop)
    {
        var slot = FindSlot(InventoryItemType.Harvest, crop.Source);
        if (slot == null) return false;

        bool removed = slot.Harvested.Remove(crop);
        if (slot.Harvested.Count == 0) slot.Clear();
        return removed;
    }

    public List<HarvestedCrop> GetAllHarvested() =>
        Slots
            .Where(s => s.Type == InventoryItemType.Harvest)
            .SelectMany(s => s.Harvested)
            .ToList();

    // Groups by crop+rank+mutation for the shop sell UI
    public Dictionary<(CropData, Rank, bool), List<HarvestedCrop>> GetGroupedHarvest()
    {
        var groups = new Dictionary<(CropData, Rank, bool), List<HarvestedCrop>>();

        foreach (var slot in Slots.Where(s => s.Type == InventoryItemType.Harvest))
        {
            foreach (var h in slot.Harvested)
            {
                var key = (h.Source, h.Rank, h.IsMutated);
                if (!groups.ContainsKey(key))
                    groups[key] = new List<HarvestedCrop>();
                groups[key].Add(h);
            }
        }

        return groups;
    }

    // ── Helpers ───────────────────────────────────────────────

    InventorySlot FindSlot(InventoryItemType type, CropData crop) =>
        Slots.FirstOrDefault(s => s.Type == type && s.Crop == crop);

    InventorySlot FindEmptySlot() =>
        Slots.FirstOrDefault(s => s.IsEmpty);
}