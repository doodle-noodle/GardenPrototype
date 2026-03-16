using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Legendary, Mythical }

[System.Serializable]
public struct RarityDefinition
{
    public Rarity Rarity;
    public Color  DisplayColor;
    public string HexCode;
    public float  PriceMultiplier;
}

public static class RarityUtility
{
    private static readonly RarityDefinition[] Definitions =
    {
        new RarityDefinition { Rarity = Rarity.Common,    DisplayColor = UIColors.RarityCommon,    HexCode = UIColors.RarityCommon_Hex,    PriceMultiplier = 1f   },
        new RarityDefinition { Rarity = Rarity.Uncommon,  DisplayColor = UIColors.RarityUncommon,  HexCode = UIColors.RarityUncommon_Hex,  PriceMultiplier = 1.5f },
        new RarityDefinition { Rarity = Rarity.Rare,      DisplayColor = UIColors.RarityRare,      HexCode = UIColors.RarityRare_Hex,      PriceMultiplier = 2.5f },
        new RarityDefinition { Rarity = Rarity.Legendary, DisplayColor = UIColors.RarityLegendary, HexCode = UIColors.RarityLegendary_Hex, PriceMultiplier = 5f   },
        new RarityDefinition { Rarity = Rarity.Mythical,  DisplayColor = UIColors.RarityMythical,  HexCode = UIColors.RarityMythical_Hex,  PriceMultiplier = 12f  },
    };

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

    public static RarityDefinition GetDefinition(Rarity rarity) => Definitions[(int)rarity];
    public static Color  RarityColor(Rarity rarity)      => GetDefinition(rarity).DisplayColor;
    public static float  PriceMultiplier(Rarity rarity)   => GetDefinition(rarity).PriceMultiplier;

    public static string RarityLabel(Rarity rarity)
    {
        var def = GetDefinition(rarity);
        return $"<color={def.HexCode}>{rarity}</color>";
    }
}