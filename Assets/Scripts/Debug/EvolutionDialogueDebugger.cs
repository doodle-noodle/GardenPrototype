#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

// EvolutionDialogueDebugger
// ─────────────────────────────────────────────────────────────────────────────
// Purpose: rapid in-play testing of character evolution, cutscene, dialogue,
//          and relationship point mechanics without needing to grow a real crop.
//
// Setup:
//   1. Add this component to any scene GameObject (e.g. "DebugTools").
//   2. Fill the inspector fields.
//   3. Enter Play mode and use the on-screen UI.
//
// Keyboard shortcuts (only active in Editor builds):
//   F1  — force first farm plot to Ready + first evolution path
//   F2  — add +5 relationship points to debugCharacter
//   F3  — add -3 relationship points to debugCharacter
//   F4  — open dialogue for debugCharacter directly
//   F5  — log all current relationship levels to TutorialConsole
// ─────────────────────────────────────────────────────────────────────────────
public class EvolutionDialogueDebugger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Character to use for F2/F3/F4 shortcuts.")]
    public CharacterData debugCharacter;

    [Tooltip("Fertilizer to inject when forcing evolution conditions.")]
    public FertilizerData debugFertilizer;

    [Tooltip("Mutations to inject when forcing evolution conditions.")]
    public List<MutationData> debugMutations = new List<MutationData>();

    [Header("Evolution path index")]
    [Tooltip("Which evolution path index to force on F1 (0 = first path in CropData).")]
    public int evolutionPathIndex = 0;

    [Header("Water count to inject")]
    public int debugWaterCount = 3;

    // ── GUI ───────────────────────────────────────────────────

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 340, 600));
        GUIStyle header = new GUIStyle(GUI.skin.label)
            { fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle small = new GUIStyle(GUI.skin.label)
            { fontSize = 11 };

        GUILayout.Label("── Evolution & Dialogue Debugger ──", header);
        GUILayout.Label("(Editor only — stripped from builds)", small);
        GUILayout.Space(6);

        // ── Plot list ─────────────────────────────────────────
        var plots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        GUILayout.Label($"Farm plots in scene: {plots.Length}", small);

        int pi = 0;
        foreach (var plot in plots)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"[{pi}] {plot.ActiveCrop?.cropName ?? "empty"} | " +
                $"{plot.State} | W:{plot.WaterCount} | F:{plot.IsFertilized}",
                small, GUILayout.Width(220));

            if (GUILayout.Button("ForceEvolve", GUILayout.Width(90)))
                ForceEvolveOnPlot(plot);

            GUILayout.EndHorizontal();
            pi++;
        }

        GUILayout.Space(8);

        // ── Relationship ──────────────────────────────────────
        GUILayout.Label("── Relationship ──", header);
        if (debugCharacter != null && RelationshipManager.Instance != null)
        {
            int pts   = RelationshipManager.Instance.GetPoints(debugCharacter);
            int level = RelationshipManager.Instance.GetLevel(debugCharacter);
            GUILayout.Label($"{debugCharacter.characterName}: {pts} pts, Level {level}", small);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+5 pts"))
                RelationshipManager.Instance.AddPoints(debugCharacter, 5);
            if (GUILayout.Button("-3 pts"))
                RelationshipManager.Instance.AddPoints(debugCharacter, -3);
            if (GUILayout.Button("+20 pts"))
                RelationshipManager.Instance.AddPoints(debugCharacter, 20);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Open Dialogue"))
                DialoguePanel.Instance?.Open(debugCharacter);
        }
        else
        {
            GUILayout.Label("No debugCharacter assigned.", small);
        }

        GUILayout.Space(8);

        // ── Active world events ───────────────────────────────
        GUILayout.Label("── Active World Events ──", header);
        if (WorldEventManager.Instance != null)
        {
            if (WorldEventManager.Instance.ActiveEvents.Count == 0)
                GUILayout.Label("(none)", small);
            foreach (var e in WorldEventManager.Instance.ActiveEvents)
                GUILayout.Label($"• {e?.name ?? "(null)"}", small);
        }

        GUILayout.Space(8);

        // ── EvolutionConfirmPanel state ───────────────────────
        GUILayout.Label("── Panel State ──", header);
        GUILayout.Label($"DialoguePanel.IsOpen:         {DialoguePanel.IsOpen}", small);
        GUILayout.Label($"EvolutionConfirmPanel.IsOpen: {EvolutionConfirmPanel.IsOpen}", small);
        GUILayout.Label($"ShopUI.IsOpen:                {ShopUI.IsOpen}", small);

        GUILayout.Space(8);
        GUILayout.Label("── Keyboard shortcuts ──", header);
        GUILayout.Label("F1  Force first plot → ReadyToEvolve", small);
        GUILayout.Label("F2  +5 pts to debugCharacter",         small);
        GUILayout.Label("F3  -3 pts to debugCharacter",         small);
        GUILayout.Label("F4  Open dialogue for debugCharacter", small);
        GUILayout.Label("F5  Log all relationship levels",       small);

        GUILayout.EndArea();
    }

    // ── Keyboard shortcuts ────────────────────────────────────

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) ForceFirstPlotEvolve();
        if (Input.GetKeyDown(KeyCode.F2) && debugCharacter != null)
            RelationshipManager.Instance?.AddPoints(debugCharacter, 5);
        if (Input.GetKeyDown(KeyCode.F3) && debugCharacter != null)
            RelationshipManager.Instance?.AddPoints(debugCharacter, -3);
        if (Input.GetKeyDown(KeyCode.F4) && debugCharacter != null)
            DialoguePanel.Instance?.Open(debugCharacter);
        if (Input.GetKeyDown(KeyCode.F5))
            LogAllRelationships();
    }

    // ── Helpers ───────────────────────────────────────────────

    void ForceFirstPlotEvolve()
    {
        var plots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        if (plots.Length == 0)
        {
            TutorialConsole.Warn("Debug: no farm plots in scene.");
            return;
        }
        ForceEvolveOnPlot(plots[0]);
    }

    void ForceEvolveOnPlot(FarmPlot plot)
    {
        if (plot == null) return;

        // Inject water count
        plot.ForceWaterCountDebug(debugWaterCount);

        // Inject fertilizer
        if (debugFertilizer != null)
            plot.ForceFertilizerDebug(debugFertilizer);

        // Inject mutations
        foreach (var m in debugMutations)
            if (m != null) plot.ApplyMutation(m);

        // Force ReadyToEvolve state on path index
        plot.ForceReadyToEvolveDebug(evolutionPathIndex);

        // Trigger EvolutionConfirmPanel
        if (plot.ReadyToEvolveWith != null)
            EvolutionConfirmPanel.Instance?.Open(plot, plot.ReadyToEvolveWith);
        else
            TutorialConsole.Warn("Debug: no valid evolution path found after forcing conditions. " +
                "Check CropData.evolutionPaths and that conditions are now satisfied.");
    }

    void LogAllRelationships()
    {
        if (RelationshipManager.Instance == null)
        {
            TutorialConsole.Warn("Debug: RelationshipManager not found.");
            return;
        }
        var rm = RelationshipManager.Instance;
        TutorialConsole.Log("── Relationship Summary ──");
        foreach (var character in rm.ExistingCharacters)
        {
            int pts   = rm.GetPoints(character);
            int level = rm.GetLevel(character);
            TutorialConsole.Log(
                $"  {character.characterName}: {pts} pts | Level {level}/{character.maxRelationshipLevel}");
        }
    }
}
#endif
