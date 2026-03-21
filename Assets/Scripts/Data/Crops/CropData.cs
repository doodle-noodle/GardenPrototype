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

    [Header("Growth — logic")]
    public GrowthStage[] growthStages;

    [Header("Growth — visuals (match by index with growthStages)")]
    public GrowthStageVisual[] stageVisuals;

    [Header("Multi-Harvest")]
    [Tooltip("If true, harvesting does not remove the plant — it regrows after regrowDuration.")]
    public bool       isMultiHarvest = false;
    [Tooltip("Seconds until the plant becomes Ready again after a multi-harvest.")]
    public float      regrowDuration = 60f;
    [Tooltip("Model shown while regrowing (fruits/produce removed). If null, no model is shown.")]
    public GameObject strippedPrefab;

    [Header("Mutation / Evolution")]
    [Range(0f, 1f)]
    public float    mutationChance = 0.05f;
    public CropData mutatesInto;

    [Header("Character Evolution")]
    [Tooltip("If assigned and this crop rolls IsEvolved on harvest, it becomes this character " +
             "instead of going to inventory. If null the crop is harvested normally even if IsEvolved.")]
    public CharacterData characterData;

    // ── IShopable ─────────────────────────────────────────────
    string   IShopable.DisplayName    => cropName;
    int      IShopable.BasePrice      => seedCost;
    Rarity   IShopable.ItemRarity     => rarity;
    ShopItem IShopable.CreateShopItem() => ShopItem.MakeSeed(this, rarity);
}
