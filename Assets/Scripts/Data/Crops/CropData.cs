using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Garden/Crop Data")]
public class CropData : ScriptableObject, IShopable
{
    [Header("Identity")]
    public string cropName;

    [Header("Tags")]
    public List<string> tags = new List<string>();

    [Header("Economy")]
    public int seedCost;
    public int sellValue;

    [Header("Shop")]
    public Rarity rarity = Rarity.Common;
    [Range(0f, 1f)]
    [Tooltip("Chance this crop seed appears in the shop pool on each refresh.")]
    public float stockChance = 1f;

    [Header("Growth — logic")]
    public GrowthStage[] growthStages;

    [Header("Growth — visuals (match by index with growthStages)")]
    public GrowthStageVisual[] stageVisuals;

    [Header("Multi-Harvest")]
    [Tooltip("If true, harvesting does not remove the plant — it regrows after regrowDuration.")]
    public bool       isMultiHarvest = false;
    public float      regrowDuration = 60f;
    [Tooltip("Model shown while regrowing (produce removed). Null = no model shown.")]
    public GameObject strippedPrefab;

    [Header("Harvest Mutations")]
    [Range(0f, 1f)]
    [Tooltip("Chance to apply one mutation from possibleHarvestMutations on harvest.")]
    public float             mutationChance            = 0.05f;
    [Tooltip("If mutationChance roll succeeds, one of these is chosen at random.")]
    public List<MutationData> possibleHarvestMutations = new List<MutationData>();

    [Header("Character Evolution")]
    [Tooltip("Ordered list of possible evolution outcomes. First path with all conditions " +
             "met and no duplicate character on the farm wins.")]
    public List<EvolutionPath> evolutionPaths = new List<EvolutionPath>();

    // ── IShopable ─────────────────────────────────────────────
    string   IShopable.DisplayName    => cropName;
    int      IShopable.BasePrice      => seedCost;
    Rarity   IShopable.ItemRarity     => rarity;
    float    IShopable.StockChance    => stockChance;
    ShopItem IShopable.CreateShopItem() => ShopItem.MakeSeed(this, rarity);
}