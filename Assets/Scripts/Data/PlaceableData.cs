using UnityEngine;

[CreateAssetMenu(fileName = "NewPlaceable", menuName = "Garden/Placeable Data")]
public class PlaceableData : ScriptableObject
{
    public string placeableName;
    public GameObject prefab;
    public int unlockCost;
    public Rarity rarity = Rarity.Common;  // set per placeable in Inspector
    public int gridWidth  = 1;
    public int gridHeight = 1;
}