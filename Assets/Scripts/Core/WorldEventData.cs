using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWorldEvent", menuName = "Garden/World Event")]
public class WorldEventData : ScriptableObject
{
    [Header("Identity")]
    public string eventName;
    public int    priority = 0;     // higher priority wins skybox

    [Header("Duration")]
    public float duration = 300f;   // seconds, 5 min default

    [Header("Skybox & Lighting")]
    public Material skyboxMaterial;
    public bool     useAmbientLight   = false;
    public Color    ambientLightColor = Color.white;

    [Header("Growth")]
    [Range(0.5f, 5f)]
    public float growthSpeedMultiplier = 1f;

    [Header("Mutation Application")]
    public MutationData mutationToApply;
    [Range(0f, 1f)]
    public float        mutationChancePerCrop    = 0f;
    public float        mutationIntervalSeconds  = 0f; // 0 = once at start
    public int          maxCropsPerCycle         = 0;  // 0 = all eligible

    [Header("Rain Visual")]
    public bool  hasRainVisual = false;
    public Color rainColor     = new Color(0.7f, 0.85f, 1f);

    [Header("Celestial")]
    public bool  hasMoon   = false;
    public Color moonColor = Color.white;
    public bool  hasStars  = false;

    [Header("Event Replacement")]
    public List<WorldEventData> canReplaceEvents = new List<WorldEventData>();
    [Range(0f, 1f)]
    public float replaceChance = 0f;

    [Header("Audio")]
    public AudioClip startSound;

    [Header("Stacking Rules")]
    public List<WorldEventData> incompatibleWith = new List<WorldEventData>();
}