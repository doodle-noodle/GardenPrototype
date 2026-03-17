using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EffectsDatabase", menuName = "Garden/Effects Database")]
public class EffectsDatabase : ScriptableObject
{
    [System.Serializable]
    public struct PlantAnimConfig
    {
        public EffectEvent Event;

        [Header("Planting drop animation")]
        public float DropHeight;
        public float DropDuration;

        [Header("Glow")]
        public bool  GlowEnabled;
        public Color GlowColor;
        public float GlowIntensity;
        public float GlowPulseSpeed;
    }

    public List<PlantAnimConfig> Effects = new List<PlantAnimConfig>();

    private Dictionary<EffectEvent, PlantAnimConfig> _lookup;

    public void Init()
    {
        _lookup = new Dictionary<EffectEvent, PlantAnimConfig>();
        foreach (var entry in Effects)
            _lookup[entry.Event] = entry;
    }

    public bool TryGet(EffectEvent e, out PlantAnimConfig config)
    {
        if (_lookup == null) Init();
        return _lookup.TryGetValue(e, out config);
    }
}