using UnityEngine;

// Assigned to a CropData to make it evolvable into a character.
// One asset per evolvable crop — e.g. CharacterData_Carrot.
// Stored in Assets/Data/Characters/
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Garden/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName;

    [Header("Portrait")]
    public Sprite portraitSprite;         // idle portrait shown in dialogue panel
    public Sprite portraitSpriteTalking;  // optional alternate frame while text is revealing

    [Header("Dialogue")]
    [Tooltip("The Yarn node title to run when this character is clicked. " +
             "Must match a node title in the shared YarnProject.")]
    public string yarnStartNode;

    [Header("Relationship")]
    public int   maxRelationshipLevel = 10;
    [Tooltip("Points required to reach each level. Length must equal maxRelationshipLevel.")]
    public int[] pointThresholds;         // e.g. [10, 25, 50, 80, 120 ...]

    [Header("Visual")]
    [Tooltip("Optional model prefab shown when plot is Evolved. " +
             "If null, the crop's mature stageVisual prefab is kept.")]
    public GameObject evolvedModelPrefab;
}
