using UnityEngine;

public enum Rank { D, C, B, A, S }

[System.Serializable]
public struct RankDefinition
{
    public Rank   Rank;
    public Color  DisplayColor;
    public Color  ButtonColor;
    public string HexCode;
    public float  SellMultiplier;
}

public static class RankUtility
{
    private static readonly RankDefinition[] Definitions =
    {
        new RankDefinition { Rank = Rank.D, DisplayColor = UIColors.RankD_Display, ButtonColor = UIColors.RankD_Button, HexCode = UIColors.RankD_Hex, SellMultiplier = 1f   },
        new RankDefinition { Rank = Rank.C, DisplayColor = UIColors.RankC_Display, ButtonColor = UIColors.RankC_Button, HexCode = UIColors.RankC_Hex, SellMultiplier = 1.5f },
        new RankDefinition { Rank = Rank.B, DisplayColor = UIColors.RankB_Display, ButtonColor = UIColors.RankB_Button, HexCode = UIColors.RankB_Hex, SellMultiplier = 2.5f },
        new RankDefinition { Rank = Rank.A, DisplayColor = UIColors.RankA_Display, ButtonColor = UIColors.RankA_Button, HexCode = UIColors.RankA_Hex, SellMultiplier = 4f   },
        new RankDefinition { Rank = Rank.S, DisplayColor = UIColors.RankS_Display, ButtonColor = UIColors.RankS_Button, HexCode = UIColors.RankS_Hex, SellMultiplier = 10f  },
    };

    private static readonly float[] Weights = { 50f, 28f, 15f, 6f, 1f };

    public static Rank RollRank()
    {
        float total = 0f;
        foreach (var w in Weights) total += w;

        float roll       = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < Weights.Length; i++)
        {
            cumulative += Weights[i];
            if (roll <= cumulative) return (Rank)i;
        }

        return Rank.D;
    }

    public static RankDefinition GetDefinition(Rank rank) => Definitions[(int)rank];
    public static Color  RankColor(Rank rank)       => GetDefinition(rank).DisplayColor;
    public static Color  RankButtonColor(Rank rank)  => GetDefinition(rank).ButtonColor;
    public static float  SellMultiplier(Rank rank)   => GetDefinition(rank).SellMultiplier;

    public static string RankLabel(Rank rank)
    {
        var def = GetDefinition(rank);
        return $"<color={def.HexCode}>{rank}</color>";
    }
}