using System.Collections.Generic;
using UnityEngine;

public enum InventoryItemType { Empty, Seed, Harvest, Tool }

public class InventorySlot
{
    public InventoryItemType   Type      = InventoryItemType.Empty;
    public CropData            Crop;
    public ToolData            Tool;
    public int                 SeedCount;
    public int                 ToolCount;
    public List<HarvestedCrop> Harvested = new List<HarvestedCrop>();

    public bool IsEmpty => Type == InventoryItemType.Empty;

    public int Count => Type switch
    {
        InventoryItemType.Seed    => SeedCount,
        InventoryItemType.Harvest => Harvested.Count,
        InventoryItemType.Tool    => ToolCount,
        _                         => 0
    };

    public string DisplayName => Type switch
    {
        InventoryItemType.Seed    => $"{Crop.cropName}\nSeed x{SeedCount}",
        InventoryItemType.Harvest => $"{Crop.cropName}\nx{Harvested.Count}",
        InventoryItemType.Tool    => $"{Tool.toolName}\nx{ToolCount}",
        _                         => ""
    };

    public string TypeLabel => Type switch
    {
        InventoryItemType.Seed    => "SEED",
        InventoryItemType.Harvest => "CROP",
        InventoryItemType.Tool    => "TOOL",
        _                         => ""
    };

    public void Clear()
    {
        Type      = InventoryItemType.Empty;
        Crop      = null;
        Tool      = null;
        SeedCount = 0;
        ToolCount = 0;
        Harvested.Clear();
    }
}