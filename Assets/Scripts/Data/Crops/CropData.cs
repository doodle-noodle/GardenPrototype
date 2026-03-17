using UnityEngine;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Garden/Crop Data")]
public class CropData : ScriptableObject, IShopable
{
    [Header("Identity")]
    public string cropName;

    [Header("Economy")]
    public int seedCost;
    public int sellValue;

    [Header("Shop")]
    public Rarity rarity = Rarity.Common;

    [Header("Growth — logic")]
    public GrowthStage[] growthStages;

    [Header("Growth — visuals (match by index with growthStages)")]
    public GrowthStageVisual[] stageVisuals;

    [Header("Mutation")]
    [Range(0f, 1f)]
    public float    mutationChance = 0.05f;
    public CropData mutatesInto;

    [Header("Dating")]
    public DialogueData dialogue;

    // ── IShopable ─────────────────────────────────────────────
    string   IShopable.DisplayName    => cropName;
    int      IShopable.BasePrice      => seedCost;
    Rarity   IShopable.ItemRarity     => rarity;
    ShopItem IShopable.CreateShopItem() => ShopItem.MakeSeed(this, rarity);
}