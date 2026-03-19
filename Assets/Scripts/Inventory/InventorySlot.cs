using System.Collections.Generic;

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
        InventoryItemType.Seed    => $"{Crop.cropName}\nx{SeedCount}",
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

    // Swaps all contents between two slots — used by drag-drop
    public static void Swap(InventorySlot a, InventorySlot b)
    {
        var tType = a.Type;      a.Type      = b.Type;      b.Type      = tType;
        var tCrop = a.Crop;      a.Crop      = b.Crop;      b.Crop      = tCrop;
        var tTool = a.Tool;      a.Tool      = b.Tool;      b.Tool      = tTool;
        var tSeeds = a.SeedCount; a.SeedCount = b.SeedCount; b.SeedCount = tSeeds;
        var tTools = a.ToolCount; a.ToolCount = b.ToolCount; b.ToolCount = tTools;
        var tHarv = a.Harvested; a.Harvested = b.Harvested; b.Harvested = tHarv;
    }
}