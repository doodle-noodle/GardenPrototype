using System.Collections.Generic;
using UnityEngine;

// Defines what a fertilizer does. Linked to a ToolData (toolType = Fertilizer).
// Stored in Assets/Data/World/
[CreateAssetMenu(fileName = "NewFertilizer", menuName = "Garden/Fertilizer Data")]
public class FertilizerData : ScriptableObject
{
    [Header("Identity")]
    public string fertilizerName;
    public Color  tintColor = new Color(0.55f, 0.75f, 0.30f);   // plot color hint when applied

    [Header("Growth Effects")]
    [Range(0f, 3f)]
    [Tooltip("Added to the growth speed multiplier while the crop grows.")]
    public float growthBonus         = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("Added to CropData.mutationChance on harvest.")]
    public float mutationChanceBonus = 0.05f;

    [Header("Mutation")]
    [Tooltip("If set, this mutation is always applied to the crop on harvest.")]
    public MutationData guaranteedMutation;

    [Header("Tag Filter")]
    [Tooltip("If non-empty, only crops with at least one of these tags are affected. " +
             "Leave empty to affect all crops.")]
    public List<string> affectedTags = new List<string>();
}
