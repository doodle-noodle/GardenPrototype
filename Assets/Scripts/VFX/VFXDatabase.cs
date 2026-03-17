using UnityEngine;

[CreateAssetMenu(fileName = "VFXDatabase", menuName = "Garden/VFX Database")]
public class VFXDatabase : ScriptableObject
{
    [Header("── Burial animation (seed planting) ──────────────")]
    public float BurialHoverHeight   = 0.5f;
    public float BurialHoverDuration = 0.1f;
    public float BurialRiseHeight    = 0.12f;
    public float BurialRiseDuration  = 0.08f;
    public float BurialPlantDuration = 0.15f;

    [Header("── Drop animation (stage transitions) ───────────")]
    public float DropHeight   = 1f;
    public float DropDuration = 0.3f;

    [Header("── Glow effect (ready to harvest) ───────────────")]
    public Color GlowColor     = Color.yellow;
    public float GlowIntensity = 1.2f;
    public float GlowPulseSpeed = 2f;
}