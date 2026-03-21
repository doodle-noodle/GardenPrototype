using UnityEngine;

public static class UIColors
{
    // ── Rank colors ───────────────────────────────────────────
    public static readonly Color RankD_Display = new Color(1.00f, 1.00f, 1.00f);
    public static readonly Color RankC_Display = new Color(0.25f, 1.00f, 0.25f);
    public static readonly Color RankB_Display = new Color(0.35f, 0.65f, 1.00f);
    public static readonly Color RankA_Display = new Color(1.00f, 0.95f, 0.10f);
    public static readonly Color RankS_Display = new Color(0.80f, 0.35f, 1.00f);

    public static readonly Color RankD_Button  = new Color(0.35f, 0.35f, 0.35f);
    public static readonly Color RankC_Button  = new Color(0.10f, 0.50f, 0.10f);
    public static readonly Color RankB_Button  = new Color(0.10f, 0.25f, 0.65f);
    public static readonly Color RankA_Button  = new Color(0.55f, 0.50f, 0.00f);
    public static readonly Color RankS_Button  = new Color(0.45f, 0.10f, 0.65f);

    public const string RankD_Hex = "#FFFFFF";
    public const string RankC_Hex = "#40FF40";
    public const string RankB_Hex = "#59A6FF";
    public const string RankA_Hex = "#FFF21A";
    public const string RankS_Hex = "#CC59FF";

    // ── Rarity colors ─────────────────────────────────────────
    public static readonly Color RarityCommon    = new Color(1.00f, 1.00f, 1.00f);
    public static readonly Color RarityUncommon  = new Color(0.18f, 0.80f, 0.18f);
    public static readonly Color RarityRare      = new Color(1.00f, 0.85f, 0.00f);
    public static readonly Color RarityLegendary = new Color(1.00f, 0.40f, 0.80f);
    public static readonly Color RarityMythical  = new Color(0.60f, 0.20f, 1.00f);

    public const string RarityCommon_Hex    = "#FFFFFF";
    public const string RarityUncommon_Hex  = "#2ECC2E";
    public const string RarityRare_Hex      = "#FFD900";
    public const string RarityLegendary_Hex = "#FF66CC";
    public const string RarityMythical_Hex  = "#9933FF";

    // ── Floating text ─────────────────────────────────────────
    public static readonly Color FloatingGold     = new Color(1.00f, 0.80f, 0.10f);
    public static readonly Color FloatingPositive = new Color(0.40f, 1.00f, 0.40f);
    public static readonly Color FloatingNegative = new Color(1.00f, 0.35f, 0.35f);
    public static readonly Color FloatingNeutral  = new Color(1.00f, 1.00f, 1.00f);

    // ── Tutorial console ──────────────────────────────────────
    public static readonly Color ConsoleNormal  = new Color(1.00f, 1.00f, 1.00f);
    public static readonly Color ConsoleWarning = new Color(1.00f, 0.70f, 0.28f);
    public static readonly Color ConsoleError   = new Color(1.00f, 0.42f, 0.42f);

    public const string ConsoleWarning_Hex = "#FFB347";
    public const string ConsoleError_Hex   = "#FF6B6B";

    // ── Inventory hotbar ──────────────────────────────────────
    public static readonly Color SlotEmpty    = new Color(0.12f, 0.12f, 0.12f, 1.00f);
    public static readonly Color SlotSeed     = new Color(0.22f, 0.22f, 0.22f, 1.00f);
    public static readonly Color SlotHarvest  = new Color(0.22f, 0.18f, 0.10f, 1.00f);
    public static readonly Color SlotSelected = new Color(0.20f, 0.55f, 0.20f, 1.00f);
    public static readonly Color SlotPanel    = new Color(0.08f, 0.08f, 0.08f, 0.90f);

    public static readonly Color TagSeed    = new Color(0.30f, 0.80f, 0.30f, 1.00f);
    public static readonly Color TagHarvest = new Color(0.90f, 0.70f, 0.10f, 1.00f);

    // ── Shop UI ───────────────────────────────────────────────
    public static readonly Color ShopPanel      = new Color(0.08f, 0.08f, 0.08f, 0.96f);
    public static readonly Color ShopScroll     = new Color(0.12f, 0.12f, 0.12f, 1.00f);
    public static readonly Color ShopBtnSellAll = new Color(0.50f, 0.35f, 0.05f, 1.00f);
    public static readonly Color ShopBtnClose   = new Color(0.70f, 0.25f, 0.25f, 1.00f);

    // ── General UI ────────────────────────────────────────────
    public static readonly Color TextPrimary   = new Color(1.00f, 1.00f, 1.00f);
    public static readonly Color TextSecondary = new Color(1.00f, 1.00f, 1.00f, 0.50f);
    public static readonly Color TextDim       = new Color(1.00f, 1.00f, 1.00f, 0.25f);
    public static readonly Color PanelDark     = new Color(0.08f, 0.08f, 0.08f, 0.90f);
    public static readonly Color Transparent   = new Color(0.00f, 0.00f, 0.00f, 0.00f);

    // ── World Event HUD ───────────────────────────────────────
    public static readonly Color EventHudPanel = new Color(0.06f, 0.06f, 0.10f, 0.88f);
    public static readonly Color EventHudBar   = new Color(0.20f, 0.55f, 0.45f, 1.00f);
    public static readonly Color EventHudBarBg = new Color(0.10f, 0.10f, 0.14f, 1.00f);

    // ── Dialogue Panel ────────────────────────────────────────
    public static readonly Color DialoguePanel       = new Color(0.05f, 0.05f, 0.08f, 0.96f);
    public static readonly Color DialoguePortrait    = new Color(0.18f, 0.18f, 0.25f, 1.00f);
    public static readonly Color DialogueName        = new Color(0.95f, 0.80f, 0.45f, 1.00f);
    public static readonly Color DialogueOption      = new Color(0.15f, 0.35f, 0.30f, 1.00f);
    public static readonly Color DialogueOptionHover = new Color(0.22f, 0.50f, 0.42f, 1.00f);
    public static readonly Color DialogueSeparator   = new Color(0.25f, 0.25f, 0.30f, 1.00f);

    // ── Evolved character label (world-space, permanent) ──────
    public static readonly Color EvolvedLabel = new Color(0.95f, 0.80f, 0.45f, 1.00f);
}
