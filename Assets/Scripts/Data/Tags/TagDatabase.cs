using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TagDatabase", menuName = "Garden/Tag Database")]
public class TagDatabase : ScriptableObject
{
    [Header("Master list of all valid tags — used for validation and Inspector display")]
    public List<string> AllTags = new List<string>();
}
