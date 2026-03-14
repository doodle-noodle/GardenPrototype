using UnityEngine;

[CreateAssetMenu(fileName = "NewCrop", menuName = "Garden/Crop Data")]
public class CropData : ScriptableObject
{
    public string cropName;
    public int seedCost;
    public int sellValue;
    public GrowthStage[] growthStages;  // define as many stages as you want
}