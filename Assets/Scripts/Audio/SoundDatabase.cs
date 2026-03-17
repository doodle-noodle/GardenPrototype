using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Garden/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    [System.Serializable]
    public struct SoundEntry
    {
        public SoundEvent Event;
        public AudioClip  Clip;
        [Range(0f, 1f)]
        public float      Volume;
        [Range(0.8f, 1.2f)]
        public float      PitchVariance;  // randomises pitch slightly for variety
    }

    public List<SoundEntry> Sounds = new List<SoundEntry>();

    private Dictionary<SoundEvent, SoundEntry> _lookup;

    public void Init()
    {
        _lookup = new Dictionary<SoundEvent, SoundEntry>();
        foreach (var entry in Sounds)
            _lookup[entry.Event] = entry;
    }

    public bool TryGet(SoundEvent e, out SoundEntry entry)
    {
        if (_lookup == null) Init();
        return _lookup.TryGetValue(e, out entry);
    }
}