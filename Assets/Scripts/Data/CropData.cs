using UnityEngine;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Garden/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    public string cropName;

    [Header("Economy")]
    public int seedCost;
    public int sellValue;

    [Header("Growth")]
    public GrowthStage[] growthStages;

    [Header("Mutation")]
    [Range(0f, 1f)]
    public float mutationChance = 0.05f;  // 5% chance by default
    public CropData mutatesInto;           // leave null = same crop, just mutated flag

    [Header("Dating")]
    public DialogueData dialogue;          // assign when the crop has dating content
}