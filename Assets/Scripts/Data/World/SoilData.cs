using System.Collections.Generic;
using UnityEngine;

// Assigned to a FarmPlot to change how crops grow there.
// Default soil comes from WorldData.defaultSoil via WorldEventManager.currentWorld.
// Stored in Assets/Data/World/
[CreateAssetMenu(fileName = "NewSoil", menuName = "Garden/Soil Data")]
public class SoilData : ScriptableObject
{
    [Header("Identity")]
    public string soilName;
    public Color  soilColor = new Color(0.35f, 0.22f, 0.08f);

    [Header("Growth Modifiers")]
    [Range(0.1f, 5f)]
    public float growthMultiplier    = 1f;   // multiplied into growth speed
    [Range(0f, 1f)]
    public float mutationChanceBonus = 0f;   // added to CropData.mutationChance on harvest

    [Header("Tag Compatibility")]
    [Tooltip("If non-empty, crops lacking all of these tags grow at half speed on this soil.")]
    public List<string> compatibleCropTags   = new List<string>();
    [Tooltip("Crops with any of these tags grow at half speed on this soil regardless of compatible tags.")]
    public List<string> incompatibleCropTags = new List<string>();
}
