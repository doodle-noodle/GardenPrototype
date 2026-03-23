using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Garden/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName;

    [Header("Portrait")]
    public Sprite portraitSprite;
    public Sprite portraitSpriteTalking;

    [Header("Dialogue")]
    [Tooltip("Yarn node title shown when player clicks an Evolved plot.")]
    public string yarnStartNode;

    [Tooltip("Yarn node title for the evolution cutscene shown after player confirms evolution. " +
             "If blank, evolution completes immediately without a cutscene node.")]
    public string evolutionYarnNode;

    [Header("Relationship")]
    public int   maxRelationshipLevel = 10;
    [Tooltip("Points required to reach each level. Length must equal maxRelationshipLevel.")]
    public int[] pointThresholds;

    [Header("Visual")]
    [Tooltip("Optional model override shown in PlotState.Evolved. " +
             "If null, the crop's mature stageVisual prefab is kept.")]
    public GameObject evolvedModelPrefab;
}