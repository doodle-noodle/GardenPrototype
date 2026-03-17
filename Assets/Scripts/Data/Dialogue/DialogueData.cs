using UnityEngine;

// One dialogue exchange: a question the plant asks and the choices the player can make.
[System.Serializable]
public class DialogueLine
{
    [TextArea]
    public string plantSpeaks;

    public DialogueChoice[] choices;
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public int    relationshipChange;  // positive = good answer, negative = bad
    [TextArea]
    public string plantResponse;
}

// Attach a set of dialogue lines to a CropData via this ScriptableObject.
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Garden/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;

    // Returns a line appropriate for the current relationship level
    public DialogueLine GetLineForLevel(int relationshipLevel)
    {
        if (lines == null || lines.Length == 0) return null;
        int index = Mathf.Clamp(relationshipLevel, 0, lines.Length - 1);
        return lines[index];
    }
}