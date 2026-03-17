using UnityEngine;

[System.Serializable]
public class ShopItem
{
    public enum ItemType { Seed, Placeable, Tool }

    public ItemType      Type;
    public Rarity        Rarity;
    public CropData      Crop;
    public PlaceableData Placeable;
    public ToolData      Tool;

    public string DisplayName => Type switch
    {
        ItemType.Seed      => $"{RarityUtility.RarityLabel(Rarity)} {Crop.cropName} seed",
        ItemType.Placeable => $"{RarityUtility.RarityLabel(Rarity)} {Placeable.placeableName}",
        ItemType.Tool      => $"{RarityUtility.RarityLabel(Rarity)} {Tool.toolName}",
        _                  => "Unknown item"
    };

    public int Price => Type switch
    {
        ItemType.Seed      => (int)(Crop.seedCost       * RarityUtility.PriceMultiplier(Rarity)),
        ItemType.Placeable => (int)(Placeable.unlockCost * RarityUtility.PriceMultiplier(Rarity)),
        ItemType.Tool      => (int)(Tool.buyCost         * RarityUtility.PriceMultiplier(Rarity)),
        _                  => 0
    };

    public static ShopItem MakeSeed(CropData crop, Rarity rarity) =>
        new ShopItem { Type = ItemType.Seed, Crop = crop, Rarity = rarity };

    public static ShopItem MakePlaceable(PlaceableData placeable, Rarity rarity) =>
        new ShopItem { Type = ItemType.Placeable, Placeable = placeable, Rarity = rarity };

    public static ShopItem MakeTool(ToolData tool, Rarity rarity) =>
        new ShopItem { Type = ItemType.Tool, Tool = tool, Rarity = rarity };
}