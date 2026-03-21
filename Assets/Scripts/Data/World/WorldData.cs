using System.Collections.Generic;
using UnityEngine;

// Data structure for a game world/biome.
// Assign one WorldData asset to WorldEventManager.currentWorld in the Inspector.
// World-switching UI is out of scope this session — data-layer only.
// Stored in Assets/Data/World/
[CreateAssetMenu(fileName = "NewWorld", menuName = "Garden/World Data")]
public class WorldData : ScriptableObject
{
    [Header("Identity")]
    public string worldName;

    [Header("Environment")]
    public SoilData  defaultSoil;      // all FarmPlots inherit this soil if not overridden
    public Material  skyboxMaterial;   // optional world skybox override

    [Header("Available Content")]
    [Tooltip("World events that can be scheduled in this world. " +
             "Leave empty to allow all events from WorldEventDatabase.")]
    public List<WorldEventData> availableEvents = new List<WorldEventData>();

    [Tooltip("Crops that appear in the shop in this world. " +
             "Leave empty to allow all crops from ShopStock.")]
    public List<CropData>       availableCrops  = new List<CropData>();
}
