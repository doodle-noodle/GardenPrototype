using UnityEngine;

// Wraps any buyable thing — a seed, a placeable, or future item types —
// into a single unit the shop can reason about.
[System.Serializable]
public class ShopItem
{
    public enum ItemType { Seed, Placeable }

    public ItemType  Type;
    public Rank      Rank;
    public CropData  Crop;           // set if Type == Seed
    public PlaceableData Placeable;  // set if Type == Placeable

    public string DisplayName => Type switch
    {
        ItemType.Seed      => $"{RankUtility.RankLabel(Rank)} {Crop.cropName} seed",
        ItemType.Placeable => $"{RankUtility.RankLabel(Rank)} {Placeable.placeableName}",
        _                  => "Unknown item"
    };

    public int Price => Type switch
    {
        ItemType.Seed      => (int)(Crop.seedCost      * RankUtility.SellMultiplier(Rank)),
        ItemType.Placeable => (int)(Placeable.unlockCost * RankUtility.SellMultiplier(Rank)),
        _                  => 0
    };

    public static ShopItem MakeSeed(CropData crop, Rank rank) =>
        new ShopItem { Type = ItemType.Seed, Crop = crop, Rank = rank };

    public static ShopItem MakePlaceable(PlaceableData placeable, Rank rank) =>
        new ShopItem { Type = ItemType.Placeable, Placeable = placeable, Rank = rank };
}