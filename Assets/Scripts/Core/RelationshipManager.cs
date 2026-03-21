using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

// Tracks relationship points per CharacterData.
// Registers the <<addRelationship N>> Yarn command and get_relationship() Yarn function.
//
// Scene setup:
//   1. Add RelationshipManager component to the Managers GameObject.
//   2. Assign the Dialogue System's DialogueRunner in the Inspector, OR leave blank and it
//      will be found automatically at Start().
public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;

    [Header("Yarn Spinner")]
    [Tooltip("Drag the Dialogue System's DialogueRunner here. " +
             "If left blank, it will be found automatically at Start.")]
    public DialogueRunner dialogueRunner;

    // Points keyed by character name string — makes Yarn function access trivial
    // and keeps serialization (Easy Save 3) straightforward in a future session
    private readonly Dictionary<string, int> _points = new Dictionary<string, int>();

    // Set by DialoguePanel.Open() before dialogue starts so commands know the active character
    public CharacterData CurrentCharacter { get; set; }

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner == null)
        {
            Debug.LogError("RelationshipManager: no DialogueRunner found. " +
                "Add a Yarn Spinner Dialogue System to the scene.");
            return;
        }

        // <<addRelationship 10>> / <<addRelationship -5>>
        // Uses AddCommandHandler — global state operation, not GameObject-targeted
        dialogueRunner.AddCommandHandler<int>("addRelationship", HandleAddRelationship);

        // get_relationship() — returns the current character's relationship level as int.
        // Must point to a static method (YS1006 requirement for AddFunction).
        // Usage in .yarn: <<if get_relationship() >= 3>>
        dialogueRunner.AddFunction("get_relationship", GetCurrentLevel);
    }

    // ── Yarn command ──────────────────────────────────────────

    void HandleAddRelationship(int amount)
    {
        if (CurrentCharacter == null)
        {
            Debug.LogWarning("RelationshipManager: <<addRelationship>> called but " +
                "CurrentCharacter is null. Was DialoguePanel.Open() called first?");
            return;
        }
        AddPoints(CurrentCharacter, amount);
    }

    // ── Yarn function — MUST be static (YS1006) ──────────────

    // Called from Yarn: <<if get_relationship() >= 3>>
    // Static so Yarn Spinner can register it via AddFunction without a MonoBehaviour reference.
    // Accesses runtime state through the singleton Instance.
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
