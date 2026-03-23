using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public const int MaxSlots         = 10;
    public const int StorageSlotCount = 50;

    public InventorySlot[] Slots        { get; private set; }
    public InventorySlot[] StorageSlots { get; private set; }
    public CropData        SelectedSeed { get; private set; }
    public InventorySlot   SelectedSlot { get; private set; }

    void Awake()
    {
        Instance     = this;
        Slots        = new InventorySlot[MaxSlots];
        StorageSlots = new InventorySlot[StorageSlotCount];
        for (int i = 0; i < MaxSlots;         i++) Slots[i]        = new InventorySlot();
        for (int i = 0; i < StorageSlotCount; i++) StorageSlots[i]  = new InventorySlot();
    }

    // ── Seeds ─────────────────────────────────────────────────

    public bool AddSeed(CropData crop, int amount = 1)
    {
        var existing = FindSeedSlot(crop);
        if (existing != null)
        {
            existing.SeedCount += amount;
            if (SelectedSeed == null) SelectSeedInternal(crop);
            EventBus.Raise_SeedAdded(crop);
            return true;
        }
        var empty = FindEmptyHotbar();
        if (empty == null)
        {
            TutorialConsole.Error("Hotbar full!");
            AudioManager.Play(SoundEvent.InventoryFull);
            return false;
        }
        empty.Type      = InventoryItemType.Seed;
        empty.Crop      = crop;
        empty.SeedCount = amount;
        if (SelectedSeed == null) SelectSeedInternal(crop);
        EventBus.Raise_SeedAdded(crop);
        return true;
    }

    public bool UseSeed(CropData crop)
    {
        var slot = FindSeedSlot(crop);
        if (slot == null || slot.SeedCount <= 0) return false;
        slot.SeedCount--;
        if (slot.SeedCount <= 0)
        {
            if (SelectedSlot == slot) Deselect();
            slot.Clear();
            SelectedSeed = Slots.FirstOrDefault(
                s => s.Type == InventoryItemType.Seed)?.Crop;
        }
        EventBus.Raise_SeedUsed(crop);
        return true;
    }

    // Always equips — clicking an already-selected seed keeps it selected (no toggle-off).
    public void SelectSeed(CropData crop)
    {
        var slot = FindSeedSlot(crop);
        if (slot == null || slot.SeedCount <= 0)
        { TutorialConsole.Warn($"No {crop.cropName} seeds."); return; }
        SelectSeedInternal(crop);
    }

    void SelectSeedInternal(CropData crop)
    {
        SelectedSeed = crop;
        SelectedSlot = FindSeedSlot(crop);
    }

    public int GetSeedCount(CropData crop) => FindSeedSlot(crop)?.SeedCount ?? 0;

    // ── Tools ─────────────────────────────────────────────────

    public bool AddTool(ToolData tool, int amount = 1)
    {
        var existing = FindToolSlot(tool);
        if (existing != null)
        {
            existing.ToolCount += amount;
            EventBus.Raise_ToolAdded(tool);
            return true;
        }
        var empty = FindEmptyHotbar();
        if (empty == null)
        {
            TutorialConsole.Error("Hotbar full!");
            AudioManager.Play(SoundEvent.InventoryFull);
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
        if (tool == null) return false;
        var slot = FindToolSlot(tool);
        if (slot == null || slot.ToolCount <= 0) return false;
        slot.ToolCount--;
        if (slot.ToolCount <= 0)
        {
            if (SelectedSlot == slot) Deselect();
            slot.Clear();
        }
        EventBus.Raise_ToolUsed(tool);
        return true;
    }

    // Always equips — no toggle-off on re-click.
    public void SelectTool(ToolData tool)
    {
        var slot = FindToolSlot(tool);
        if (slot == null || slot.ToolCount <= 0)
        { TutorialConsole.Warn($"No {tool.toolName}."); return; }
        SelectedSeed = null;
        SelectedSlot = slot;
    }

    // Deselects to empty hands. Called when an empty hotbar slot is clicked/pressed.
    public void Deselect()
    {
        SelectedSeed = null;
        SelectedSlot = null;
    }

    // ── Harvest ───────────────────────────────────────────────

    public bool AddHarvest(HarvestedCrop crop)
    {
        var existing = FindHarvestSlot(crop.Source);
        if (existing != null)
        { existing.Harvested.Add(crop); EventBus.Raise_CropHarvested(crop); return true; }
        var empty = FindEmptyHotbar();
        if (empty == null)
        {
            TutorialConsole.Error("Hotbar full!");
            AudioManager.Play(SoundEvent.InventoryFull);
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
        var slot = FindHarvestSlot(crop.Source);
        if (slot == null) return false;
        bool removed = slot.Harvested.Remove(crop);
        if (slot.Harvested.Count == 0) slot.Clear();
        return removed;
    }

    public List<HarvestedCrop> GetAllHarvested() =>
        Slots.Where(s => s.Type == InventoryItemType.Harvest)
             .SelectMany(s => s.Harvested).ToList();

    public Dictionary<string, List<HarvestedCrop>> GetGroupedHarvest()
    {
        var groups = new Dictionary<string, List<HarvestedCrop>>();
        foreach (var slot in Slots.Where(s => s.Type == InventoryItemType.Harvest))
            foreach (var h in slot.Harvested)
            {
                string key = GroupKey(h);
                if (!groups.ContainsKey(key)) groups[key] = new List<HarvestedCrop>();
                groups[key].Add(h);
            }
        return groups;
    }

    static string GroupKey(HarvestedCrop h)
    {
        string muts = h.Mutations != null && h.Mutations.Count > 0
            ? string.Join(",", h.Mutations.Select(m => m.mutationName).OrderBy(n => n)) : "";
        return $"{h.Source?.cropName ?? ""}|{muts}";
    }

    // ── Slot selection ─────────────────────────────────────────
    // Called by number keys and hotbar button clicks.
    // Occupied slot → equip. Empty slot → deselect.
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= MaxSlots) return;
        var slot = Slots[index];
        if (slot.IsEmpty) { Deselect(); return; }
        switch (slot.Type)
        {
            case InventoryItemType.Seed: SelectSeed(slot.Crop); break;
            case InventoryItemType.Tool: SelectTool(slot.Tool); break;
        }
    }

    public void ValidateSelection()
    {
        if (SelectedSlot == null) return;
        bool valid = Slots.Contains(SelectedSlot) || StorageSlots.Contains(SelectedSlot);
        if (!valid || SelectedSlot.IsEmpty) Deselect();
        else if (SelectedSlot.Type == InventoryItemType.Seed)
            SelectedSeed = SelectedSlot.Crop;

        if (SelectedSeed != null)
        {
            var slot = FindSeedSlot(SelectedSeed);
            if (slot == null || slot.SeedCount <= 0) Deselect();
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    InventorySlot FindSeedSlot(CropData c)    => Slots.FirstOrDefault(s => s.Type == InventoryItemType.Seed    && s.Crop == c);
    InventorySlot FindToolSlot(ToolData t)    => Slots.FirstOrDefault(s => s.Type == InventoryItemType.Tool    && s.Tool == t);
    InventorySlot FindHarvestSlot(CropData c) => Slots.FirstOrDefault(s => s.Type == InventoryItemType.Harvest && s.Crop == c);
    InventorySlot FindEmptyHotbar()           => Slots.FirstOrDefault(s => s.IsEmpty);
}
