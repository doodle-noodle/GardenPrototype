using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MutationDatabase", menuName = "Garden/Mutation Database")]
public class MutationDatabase : ScriptableObject
{
    public static MutationDatabase Instance;

    public List<MutationData> AllMutations;

    public void Init() => Instance = this;
}