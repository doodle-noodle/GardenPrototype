using System.Collections.Generic;
using UnityEngine;

// Manages active world events and exposes the API FarmPlot uses for evolution conditions.
public class WorldEventManager : MonoBehaviour
{
    public static WorldEventManager Instance;

    [Header("Data")]
    public WorldEventDatabase database;
    public WorldEventData     defaultEvent;   // WE_Day — MUST be assigned or per-frame NullRef
    public WorldData          currentWorld;   // WorldData_Default asset

    [Header("Runtime — read-only in Inspector")]
    [SerializeField] private List<WorldEventData> _activeEvents = new List<WorldEventData>();

    public IReadOnlyList<WorldEventData> ActiveEvents => _activeEvents;

    // Growth speed multiplier contributed by currently active events
    public float GrowthSpeedMultiplier
    {
        get
        {
            float mult = 1f;
            foreach (var e in _activeEvents)
                if (e != null) mult *= e.growthSpeedMultiplier;
            return mult;
        }
    }

    void Awake() { Instance = this; }

    void Start()
    {
        if (defaultEvent != null && !_activeEvents.Contains(defaultEvent))
            StartEvent(defaultEvent);
    }

    // ── Event API ─────────────────────────────────────────────

    public bool IsEventActive(WorldEventData eventData)
    {
        if (eventData == null) return false;
        return _activeEvents.Contains(eventData);
    }

    public void StartEvent(WorldEventData eventData)
    {
        if (eventData == null || _activeEvents.Contains(eventData)) return;
        _activeEvents.Add(eventData);
        ApplyVisuals();
        EventBus.Raise_WorldEventStarted(eventData);
    }

    public void EndEvent(WorldEventData eventData)
    {
        if (eventData == null || !_activeEvents.Contains(eventData)) return;
        _activeEvents.Remove(eventData);
        ApplyVisuals();
        EventBus.Raise_WorldEventEnded(eventData);
    }

    public void EndAllEvents()
    {
        for (int i = _activeEvents.Count - 1; i >= 0; i--)
            EndEvent(_activeEvents[i]);
    }

    // ── Visuals ───────────────────────────────────────────────

    void ApplyVisuals()
    {
        bool raining = false;
        foreach (var e in _activeEvents)
            if (e != null && e.hasRainVisual) raining = true;

        // RainSystem is optional — gracefully skip if not present
        var rain = FindFirstObjectByType<RainSystem>();
        if (rain != null)
        {
            // Collect the rain color from the first active event that has a rain visual
            Color rainColor = Color.white;
            foreach (var e in _activeEvents)
                if (e != null && e.hasRainVisual) { rainColor = e.rainColor; break; }

            rain.SetRain(raining, rainColor);
        }

        // Skybox — use first active event that has one, or world default
        Material skybox = null;
        foreach (var e in _activeEvents)
            if (e?.skyboxMaterial != null) { skybox = e.skyboxMaterial; break; }

        if (skybox == null && currentWorld?.skyboxMaterial != null)
            skybox = currentWorld.skyboxMaterial;

        if (skybox != null)
            RenderSettings.skybox = skybox;
    }
}
