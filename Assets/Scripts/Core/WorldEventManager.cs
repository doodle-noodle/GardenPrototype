using System.Collections.Generic;
using UnityEngine;

public class WorldEventManager : MonoBehaviour
{
    public static WorldEventManager Instance;

    [Header("Database")]
    public WorldEventDatabase database;
    public WorldEventData     defaultEvent;

    [Header("World")]
    [Tooltip("The active world/biome. Provides default soil and content lists. " +
             "World-switching UI is planned for a future version.")]
    public WorldData currentWorld;

    [Header("Auto Scheduling")]
    public float eventCheckInterval = 120f;
    [Range(0f, 1f)]
    public float eventTriggerChance = 0.4f;

    private readonly List<ActiveWorldEvent> _activeEvents = new List<ActiveWorldEvent>();
    private float    _schedulerTimer;
    private Material _defaultSkybox;
    private Color    _defaultAmbientLight;

    private RainSystem _rainSystem;
    // CelestialSystem removed — a safe replacement will be built in a future session

    // ── Public API ────────────────────────────────────────────

    public float GrowthSpeedMultiplier
    {
        get
        {
            float m = 1f;
            foreach (var e in _activeEvents)
                if (e?.Data != null) m *= e.Data.growthSpeedMultiplier;
            return m;
        }
    }

    // Read-only access for WorldEventHUD and any future systems — preserves encapsulation
    public IReadOnlyList<ActiveWorldEvent> ActiveEvents => _activeEvents;

    public bool IsEventActive(WorldEventData data)
    {
        if (data == null) return false;
        return _activeEvents.Exists(e => e?.Data == data);
    }

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance             = this;
        _defaultSkybox       = RenderSettings.skybox;
        _defaultAmbientLight = RenderSettings.ambientLight;
        _rainSystem          = gameObject.AddComponent<RainSystem>();
    }

    void Start()
    {
        _schedulerTimer = eventCheckInterval;
        if (defaultEvent != null)
            ActivateEvent(defaultEvent);
        else
            Debug.LogWarning("WorldEventManager: defaultEvent not assigned in Inspector. " +
                "GrowthSpeedMultiplier will throw NullReferenceException every frame.");
    }

    void Update()
    {
        TickActiveEvents();
        TickScheduler();
    }

    // ── Scheduling ────────────────────────────────────────────

    void TickScheduler()
    {
        _schedulerTimer -= Time.deltaTime;
        if (_schedulerTimer > 0f) return;
        _schedulerTimer = eventCheckInterval;
        if (Random.value <= eventTriggerChance) TryTriggerRandomEvent();
    }

    void TryTriggerRandomEvent()
    {
        if (database == null) return;
        var candidates = database.Events.FindAll(
            e => e != null && e != defaultEvent &&
                 !IsEventActive(e) && CanActivate(e));
        if (candidates.Count == 0) return;
        StartEvent(candidates[Random.Range(0, candidates.Count)]);
    }

    public void StartEvent(WorldEventData data)
    {
        if (data == null || IsEventActive(data) || !CanActivate(data)) return;
        foreach (var target in data.canReplaceEvents)
            if (target != null && IsEventActive(target) &&
                Random.value < data.replaceChance)
                EndEvent(target);
        ActivateEvent(data);
    }

    public void EndEvent(WorldEventData data)
    {
        if (data == null) return;
        var active = _activeEvents.Find(e => e?.Data == data);
        if (active == null) return;
        _activeEvents.Remove(active);
        ApplySkybox();
        ApplyVisuals();
        TutorialConsole.Log($"World event ended: {data.eventName}.");
        EventBus.Raise_WorldEventEnded(data);
    }

    void ActivateEvent(WorldEventData data)
    {
        if (data == null) return;
        _activeEvents.Add(new ActiveWorldEvent
        {
            Data          = data,
            RemainingTime = data.duration,
            MutationTimer = data.mutationIntervalSeconds > 0f
                            ? data.mutationIntervalSeconds : 0f
        });
        ApplySkybox();
        ApplyVisuals();

        if (data.mutationToApply != null &&
            data.mutationIntervalSeconds <= 0f &&
            data.mutationChancePerCrop > 0f)
            ApplyMutationToCrops(data);

        if (data.startSound != null) AudioManager.PlayClip(data.startSound);
        if (data != defaultEvent)
        {
            TutorialConsole.Log($"World event: {data.eventName} has begun!");
            EventBus.Raise_WorldEventStarted(data);
        }
    }

    bool CanActivate(WorldEventData data)
    {
        if (data == null) return false;
        foreach (var active in _activeEvents)
        {
            if (active?.Data == null) continue;
            if (data.incompatibleWith != null &&
                data.incompatibleWith.Contains(active.Data)) return false;
            if (active.Data.incompatibleWith != null &&
                active.Data.incompatibleWith.Contains(data)) return false;
        }
        return true;
    }

    // ── Tick ──────────────────────────────────────────────────

    void TickActiveEvents()
    {
        for (int i = _activeEvents.Count - 1; i >= 0; i--)
        {
            var active = _activeEvents[i];
            if (active?.Data == null || active.Data == defaultEvent) continue;
            active.RemainingTime -= Time.deltaTime;

            if (active.Data.mutationToApply != null &&
                active.Data.mutationIntervalSeconds > 0f)
            {
                active.MutationTimer -= Time.deltaTime;
                if (active.MutationTimer <= 0f)
                {
                    ApplyMutationToCrops(active.Data);
                    active.MutationTimer = active.Data.mutationIntervalSeconds;
                }
            }

            if (active.RemainingTime <= 0f) EndEvent(active.Data);
        }
    }

    // ── Mutations ─────────────────────────────────────────────

    void ApplyMutationToCrops(WorldEventData data)
    {
        if (data?.mutationToApply == null) return;
        var allPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        var growing  = new List<FarmPlot>();
        foreach (var p in allPlots)
            if (p != null && p.State == FarmPlot.PlotState.Growing)
                growing.Add(p);
        if (growing.Count == 0) return;

        for (int i = growing.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (growing[i], growing[j]) = (growing[j], growing[i]);
        }

        int max     = data.maxCropsPerCycle > 0
                      ? Mathf.Min(data.maxCropsPerCycle, growing.Count)
                      : growing.Count;
        int applied = 0;
        foreach (var plot in growing)
        {
            if (applied >= max) break;
            if (data.mutationChancePerCrop >= 1f ||
                Random.value <= data.mutationChancePerCrop)
            {
                plot.ApplyMutation(data.mutationToApply);
                applied++;
            }
        }
    }

    // ── Skybox & Visuals ──────────────────────────────────────

    void ApplySkybox()
    {
        WorldEventData skyboxWinner  = null;
        WorldEventData ambientWinner = null;
        int highest = -1;

        foreach (var active in _activeEvents)
        {
            if (active?.Data?.skyboxMaterial == null) continue;
            if (active.Data.priority > highest)
            { skyboxWinner = active.Data; highest = active.Data.priority; }
        }

        highest = -1;
        foreach (var active in _activeEvents)
        {
            if (active?.Data == null || !active.Data.useAmbientLight) continue;
            if (active.Data.priority > highest)
            { ambientWinner = active.Data; highest = active.Data.priority; }
        }

        RenderSettings.skybox       = skyboxWinner?.skyboxMaterial ?? _defaultSkybox;
        RenderSettings.ambientLight = ambientWinner?.ambientLightColor ?? _defaultAmbientLight;
        DynamicGI.UpdateEnvironment();
    }

    void ApplyVisuals()
    {
        bool  hasRain   = _activeEvents.Exists(e => e?.Data?.hasRainVisual == true);
        var   rainEvent = _activeEvents.Find(e => e?.Data?.hasRainVisual == true);
        Color rainColor = rainEvent?.Data?.rainColor ?? new Color(0.7f, 0.85f, 1f);
        _rainSystem?.SetRain(hasRain, rainColor);
    }
}

public class ActiveWorldEvent
{
    public WorldEventData Data;
    public float          RemainingTime;
    public float          MutationTimer;
}
