using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlaceable", menuName = "Garden/Placeable Data")]
public class PlaceableData : ScriptableObject, IShopable
{
    [Header("Identity")]
    public string placeableName;

    [Header("Tags")]
    public List<string> tags = new List<string>();

    [Header("Placement")]
    public GameObject prefab;
    public int        unlockCost;
    public int        gridWidth  = 1;
    public int        gridHeight = 1;

    [Header("Shop")]
    public Rarity rarity = Rarity.Common;

    // ── IShopable ─────────────────────────────────────────────
    string   IShopable.DisplayName    => placeableName;
    int      IShopable.BasePrice      => unlockCost;
    Rarity   IShopable.ItemRarity     => rarity;
    ShopItem IShopable.CreateShopItem() => ShopItem.MakePlaceable(this, rarity);
}
