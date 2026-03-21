using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMutation", menuName = "Garden/Mutation")]
public class MutationData : ScriptableObject
{
    [Header("Identity")]
    public string mutationName;
    public string displaySymbol;    // optional short symbol e.g. "❄" for frozen

    [Header("Tags")]
    public List<string> tags = new List<string>();

    [Header("Economy")]
    public float  sellMultiplier = 1.5f;

    [Header("Visual")]
    public Color  tintColor      = Color.white;
    public bool   applyTint      = false;
    public float  sizeMultiplier = 1f;

    [Header("Combinations — when this mutation meets another")]
    public List<MutationCombination> combinations;
}

[System.Serializable]
public class MutationCombination
{
    public MutationData combinesWith;
    public MutationData resultsIn;
}
