using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;

    [Header("Yarn Spinner")]
    [Tooltip("Auto-found at Start if blank.")]
    public DialogueRunner dialogueRunner;

    // Characters currently present on the farm — prevents duplicate evolutions.
    public HashSet<CharacterData> ExistingCharacters { get; } = new HashSet<CharacterData>();

    private readonly Dictionary<string, int> _points = new Dictionary<string, int>();

    public CharacterData CurrentCharacter { get; set; }

    void Awake() { Instance = this; }

    void Start()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner == null)
        {
            Debug.LogError("RelationshipManager: no DialogueRunner found in scene.");
            return;
        }

        dialogueRunner.AddCommandHandler<int>("addRelationship", HandleAddRelationship);
        dialogueRunner.AddFunction("get_relationship", GetCurrentLevel);

        EventBus.OnPlotEvolved += OnPlotEvolved;
    }

    void OnDestroy()
    {
        EventBus.OnPlotEvolved -= OnPlotEvolved;
    }

    void OnPlotEvolved(FarmPlot plot, CharacterData data)
    {
        if (data != null) ExistingCharacters.Add(data);
    }

    // ── Yarn command ──────────────────────────────────────────

    void HandleAddRelationship(int amount)
    {
        if (CurrentCharacter == null)
        {
            Debug.LogWarning("RelationshipManager: <<addRelationship>> called but " +
                "CurrentCharacter is null.");
            return;
        }
        AddPoints(CurrentCharacter, amount);
    }

    // ── Yarn function — must be static (YS1006) ───────────────

    public static int GetCurrentLevel()
    {
        if (Instance == null || Instance.CurrentCharacter == null) return 0;
        return Instance.GetLevel(Instance.CurrentCharacter);
    }

    // ── Public API ────────────────────────────────────────────

    public void AddPoints(CharacterData character, int amount)
    {
        if (character == null) return;
        string key = character.characterName;
        if (!_points.ContainsKey(key)) _points[key] = 0;
        _points[key] = Mathf.Max(0, _points[key] + amount);
        int newLevel = GetLevel(character);
        EventBus.Raise_RelationshipChanged(character, newLevel);
        TutorialConsole.Log($"{character.characterName}: {_points[key]} pts " +
            $"(Level {newLevel}/{character.maxRelationshipLevel}).");
    }

    public int GetPoints(CharacterData character)
    {
        if (character == null) return 0;
        return _points.TryGetValue(character.characterName, out int p) ? p : 0;
    }

    public int GetLevel(CharacterData character)
    {
        if (character == null ||
            character.pointThresholds == null ||
            character.pointThresholds.Length == 0) return 0;
        int points = GetPoints(character);
        for (int i = character.pointThresholds.Length - 1; i >= 0; i--)
            if (points >= character.pointThresholds[i]) return i + 1;
        return 0;
    }
}
