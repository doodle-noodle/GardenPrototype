using UnityEngine;

public enum ToolType { Shovel, WateringCan }

[CreateAssetMenu(fileName = "NewTool", menuName = "Garden/Tool Data")]
public class ToolData : ScriptableObject, IShopable
{
    [Header("Identity")]
    public string   toolName;
    public ToolType toolType;

    [Header("Economy")]
    public int buyCost;

    [Header("Shop")]
    public Rarity rarity = Rarity.Common;

    [Header("Inventory")]
    public Color toolColor = new Color(0.6f, 0.4f, 0.1f);

    [Header("Usage")]
    public bool isConsumable = false;
    public int  buyQuantity  = 1;  // charges added per purchase

    // ── IShopable ─────────────────────────────────────────────
    string   IShopable.DisplayName     => toolName;
    int      IShopable.BasePrice       => buyCost;
    Rarity   IShopable.ItemRarity      => rarity;
    ShopItem IShopable.CreateShopItem() => ShopItem.MakeTool(this, rarity);
}