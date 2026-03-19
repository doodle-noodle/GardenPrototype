using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldEventDatabase", menuName = "Garden/World Event Database")]
public class WorldEventDatabase : ScriptableObject
{
    [Header("All available world events — drag assets here")]
    public List<WorldEventData> Events = new List<WorldEventData>();
}