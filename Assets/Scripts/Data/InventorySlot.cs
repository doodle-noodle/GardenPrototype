using System.Collections.Generic;
using UnityEngine;

public enum InventoryItemType { Empty, Seed, Harvest }

public class InventorySlot
{
    public InventoryItemType   Type      = InventoryItemType.Empty;
    public CropData            Crop;
    public int                 SeedCount;
    public List<HarvestedCrop> Harvested = new List<HarvestedCrop>();

    public bool IsEmpty => Type == InventoryItemType.Empty;
    public int  Count   => Type == InventoryItemType.Seed ? SeedCount : Harvested.Count;

    public string DisplayName => Type switch
    {
        InventoryItemType.Seed    => $"{Crop.cropName}\nSeed x{SeedCount}",
        InventoryItemType.Harvest => $"{Crop.cropName}\nx{Harvested.Count}",
        _                         => ""
    };

    public string TypeLabel => Type switch
    {
        InventoryItemType.Seed    => "SEED",
        InventoryItemType.Harvest => "CROP",
        _                         => ""
    };

    public void Clear()
    {
        Type      = InventoryItemType.Empty;
        Crop      = null;
        SeedCount = 0;
        Harvested.Clear();
    }
}