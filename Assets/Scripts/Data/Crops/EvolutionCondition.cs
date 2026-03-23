using System.Collections.Generic;
using UnityEngine;

public enum EvolutionConditionType
{
    HasMutation,
    IsFertilized,
    WateredAtLeast,
    WorldEvent,
    SoilType
}

[System.Serializable]
public class EvolutionCondition
{
    public EvolutionConditionType type;

    [Header("HasMutation")]
    public MutationData requiredMutation;

    [Header("WateredAtLeast")]
    public int minWaterCount = 1;

    [Header("WorldEvent")]
    public List<WorldEventData> requiredWorldEvents  = new List<WorldEventData>();
    public List<WorldEventData> forbiddenWorldEvents = new List<WorldEventData>();

    [Header("SoilType")]
    public SoilData requiredSoil;
}

[System.Serializable]
public class EvolutionPath
{
    [Tooltip("The character this crop evolves into.")]
    public CharacterData characterData;

    [Tooltip("Every condition in this list must be satisfied.")]
    public List<EvolutionCondition> conditions = new List<EvolutionCondition>();
}