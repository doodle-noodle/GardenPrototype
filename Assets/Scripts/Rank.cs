using UnityEngine;

public enum Rank { D, C, B, A, S }

public static class RankUtility
{
    // Probability weights for each rank — higher index = rarer
    private static readonly float[] Weights = { 50f, 28f, 15f, 6f, 1f };

    public static Rank RollRank()
    {
        float total = 0f;
        foreach (var w in Weights) total += w;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < Weights.Length; i++)
        {
            cumulative += Weights[i];
            if (roll <= cumulative) return (Rank)i;
        }

        return Rank.D;
    }

    // Sell value multiplier per rank
    public static float SellMultiplier(Rank rank) => rank switch
    {
        Rank.D => 1f,
        Rank.C => 1.5f,
        Rank.B => 2.5f,
        Rank.A => 4f,
        Rank.S => 10f,
        _      => 1f
    };

    public static Color RankColor(Rank rank) => rank switch
    {
        Rank.D => Color.gray,
        Rank.C => Color.white,
        Rank.B => Color.green,
        Rank.A => Color.blue,
        Rank.S => Color.yellow,
        _      => Color.white
    };

    public static string RankLabel(Rank rank) => rank switch
    {
        Rank.S => "<color=#FFD700>S</color>",
        Rank.A => "<color=#6495ED>A</color>",
        Rank.B => "<color=#32CD32>B</color>",
        Rank.C => "<color=#FFFFFF>C</color>",
        Rank.D => "<color=#808080>D</color>",
        _      => "D"
    };
}