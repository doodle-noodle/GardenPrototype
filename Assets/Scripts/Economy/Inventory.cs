using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    // ── Capacity ──────────────────────────────────────────────
    public const int MaxSlots = 10;

    // ── State ─────────────────────────────────────────────────
    public InventorySlot[] Slots        { get; private set; }
    public CropData        SelectedSeed { get; private set; }

    // The currently selected slot — used by ToolUser to know what's active
    public InventorySlot SelectedSlot   { get; private set; }

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
        var existing = FindSlotByType(InventoryItemType.Seed, crop);
        if (existing != null)
        {
            existing.SeedCount += amount;
            if (SelectedSeed == null) SelectSeedSlot(crop);
            EventBus.Raise_SeedAdded(crop);
            return true;
        }

        var empty = FindEmptySlot();
        if (empty == null)
        {
            TutorialConsole.Error("Inventory full! Sell some items first.");
            return false;
        }

        empty.Type      = InventoryItemType.Seed;
        empty.Crop      = crop;
        empty.SeedCount = amount;
        if (SelectedSeed == null) SelectSeedSlot(crop);
        EventBus.Raise_SeedAdded(crop);
        return true;
    }

    public bool UseSeed(CropData crop)
    {
        var slot = FindSlotByType(InventoryItemType.Seed, crop);
        if (slot == null || slot.SeedCount <= 0)
        {
            TutorialConsole.Warn($"No {crop.cropName} seeds left.");
            return false;
        }

        slot.SeedCount--;

        if (slot.SeedCount <= 0)
        {
            if (SelectedSlot == slot) SelectedSlot = null;
            slot.Clear();
            SelectedSeed = Slots
                .FirstOrDefault(s => s.Type == InventoryItemType.Seed)?.Crop;
        }

        EventBus.Raise_SeedUsed(crop);
        return true;
    }

    public void SelectSeed(CropData crop)
    {
        var slot = FindSlotByType(InventoryItemType.Seed, crop);
        if (slot == null || slot.SeedCount <= 0)
        {
            TutorialConsole.Warn($"No {crop.cropName} seeds in inventory.");
            return;
        }
        SelectSeedSlot(crop);
        TutorialConsole.Log($"Selected: {crop.cropName} seed.");
    }

    void SelectSeedSlot(CropData crop)
    {
        SelectedSeed = crop;
        SelectedSlot = FindSlotByType(InventoryItemType.Seed, crop);
    }

    public int GetSeedCount(CropData crop) =>
        FindSlotByType(InventoryItemType.Seed, crop)?.SeedCount ?? 0;

    public Dictionary<CropData, int> GetAllSeeds() =>
        Slots
            .Where(s => s.Type == InventoryItemType.Seed && s.Crop != null)
            .ToDictionary(s => s.Crop, s => s.SeedCount);

    // ── Tools ─────────────────────────────────────────────────

    public bool AddTool(ToolData tool, int amount = 1)
    {
        // Stack into existing tool slot of same type
        var existing = FindToolSlot(tool);
        if (existing != null)
        {
            existing.ToolCount += amount;
            EventBus.Raise_ToolAdded(tool);
            return true;
        }

        var empty = FindEmptySlot();
        if (empty == null)
        {
            TutorialConsole.Error("Inventory full! Sell some items first.");
            return false;
        }

        empty.Type      = InventoryItemType.Tool;
        empty.Tool      = tool;
        empty.ToolCount = amount;
        EventBus.Raise_ToolAdded(tool);
        return true;
    }

    public bool UseTool(ToolData tool)
    {
        var slot = FindToolSlot(tool);
        if (slot == null || slot.ToolCount <= 0)
        {
            TutorialConsole.Warn($"No {tool.toolName} left.");
            return false;
        }

        slot.ToolCount--;

        if (slot.ToolCount <= 0)
        {
            if (SelectedSlot == slot) SelectedSlot = null;
            slot.Clear();
        }

        EventBus.Raise_ToolUsed(tool);
        return true;
    }

    public void SelectTool(ToolData tool)
    {
        var slot = FindToolSlot(tool);
        if (slot == null || slot.ToolCount <= 0)
        {
            TutorialConsole.Warn($"No {tool.toolName} in inventory.");
            return;
        }

        // Deselect seed when switching to a tool
        SelectedSeed = null;
        SelectedSlot = slot;
        TutorialConsole.Log($"Selected: {tool.toolName}.");
    }

    public int GetToolCount(ToolData tool) =>
        FindToolSlot(tool)?.ToolCount ?? 0;

    // ── Harvest ───────────────────────────────────────────────

    public bool AddHarvest(HarvestedCrop crop)
    {
        var existing = FindSlotByType(InventoryItemType.Harvest, crop.Source);
        if (existing != null)
        {
            existing.Harvested.Add(crop);
            EventBus.Raise_CropHarvested(crop);
            return true;
        }

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
        var slot = FindSlotByType(InventoryItemType.Harvest, crop.Source);
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

    // ── Slot selection ────────────────────────────────────────

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= MaxSlots) return;

        InventorySlot slot = Slots[index];
        if (slot.IsEmpty) return;

        switch (slot.Type)
        {
            case InventoryItemType.Seed:
                SelectSeed(slot.Crop);
                break;
            case InventoryItemType.Tool:
                SelectTool(slot.Tool);
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    InventorySlot FindSlotByType(InventoryItemType type, CropData crop) =>
        Slots.FirstOrDefault(s => s.Type == type && s.Crop == crop);

    InventorySlot FindToolSlot(ToolData tool) =>
        Slots.FirstOrDefault(s => s.Type == InventoryItemType.Tool && s.Tool == tool);

    InventorySlot FindEmptySlot() =>
        Slots.FirstOrDefault(s => s.IsEmpty);
}