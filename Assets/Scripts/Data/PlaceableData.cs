using UnityEngine;

[CreateAssetMenu(fileName = "NewPlaceable", menuName = "Garden/Placeable Data")]
public class PlaceableData : ScriptableObject
{
    public string placeableName;
    public GameObject prefab;
    public int unlockCost;
    public int gridWidth  = 1;   // how many grid cells wide
    public int gridHeight = 1;   // how many grid cells deep
}