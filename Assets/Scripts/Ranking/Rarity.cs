using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Legendary, Mythical }

public static class RarityUtility
{
    private static readonly float[] Weights = { 55f, 28f, 12f, 4f, 1f };

    public static Rarity RollRarity()
    {
        float total = 0f;
        foreach (var w in Weights) total += w;

        float roll       = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < Weights.Length; i++)
        {
            cumulative += Weights[i];
            if (roll <= cumulative) return (Rarity)i;
        }

        return Rarity.Common;
    }

    // Price multiplier per rarity — rarer items cost more in the shop
    public static float PriceMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common    => 1f,
        Rarity.Uncommon  => 1.5f,
        Rarity.Rare      => 2.5f,
        Rarity.Legendary => 5f,
        Rarity.Mythical  => 12f,
        _                => 1f
    };

    public static Color RarityColor(Rarity rarity) => rarity switch
    {
        Rarity.Common    => Color.white,
        Rarity.Uncommon  => new Color(0.18f, 0.8f, 0.18f),   // green
        Rarity.Rare      => new Color(1f,    0.85f, 0f),      // yellow
        Rarity.Legendary => new Color(1f,    0.4f,  0.8f),    // pink
        Rarity.Mythical  => new Color(0.6f,  0.2f,  1f),      // purple
        _                => Color.white
    };

    // Rich text label with rarity color
    public static string RarityLabel(Rarity rarity)
    {
        Color  c     = RarityColor(rarity);
        string hex   = ColorUtility.ToHtmlStringRGB(c);
        string name  = rarity.ToString();
        return $"<color=#{hex}>{name}</color>";
    }
}