using UnityEngine;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Garden/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    public string cropName;

    [Header("Economy")]
    public int seedCost;
    public int sellValue;

    [Header("Shop")]
    public Rarity rarity = Rarity.Common;  // set per crop in Inspector

    [Header("Growth")]
    public GrowthStage[] growthStages;

    [Header("Mutation")]
    [Range(0f, 1f)]
    public float mutationChance = 0.05f;
    public CropData mutatesInto;

    [Header("Dating")]
    public DialogueData dialogue;
}