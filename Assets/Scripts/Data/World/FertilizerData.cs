using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFertilizer", menuName = "Garden/Fertilizer Data")]
public class FertilizerData : ScriptableObject
{
    [Header("Identity")]
    public string fertilizerName;

    [Header("Visual")]
    [Tooltip("Texture shown on the plot overlay when this fertilizer is applied. " +
             "Should be a tileable or square texture. Alpha channel controls opacity.")]
    public Texture2D overlayTexture;

    [Header("Growth Effects")]
    [Range(0f, 3f)]
    [Tooltip("Added to the combined growth speed multiplier during growth.")]
    public float growthBonus         = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("Added to the harvest mutation chance roll.")]
    public float mutationChanceBonus = 0.05f;

    [Header("Mutation")]
    [Tooltip("If set, this mutation is always added to the harvested crop " +
             "(if crop tags match, or affectedTags is empty).")]
    public MutationData guaranteedMutation;

    [Header("Tag Filter")]
    [Tooltip("If non-empty, the guaranteed mutation only applies to crops with at least one " +
             "of these tags. Growth bonuses always apply regardless of tags.")]
    public List<string> affectedTags = new List<string>();
}