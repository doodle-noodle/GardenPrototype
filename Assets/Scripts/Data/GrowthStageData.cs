using UnityEngine;

[System.Serializable]
public class GrowthStage
{
    public string stageName;        // "Seedling", "Sprout", "Mature"
    public float duration;          // seconds in this stage
    public Color stageColor;        // color of the visual sphere
    [Range(0.1f, 1f)]
    public float scale;             // how big the visual is at this stage

    public GameObject visualPrefab;
}