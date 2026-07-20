using System.Collections.Generic;
using UnityEngine;

public static class BattleRules
{
    public static float GetNextBustRate(BattleData data)
    {
        int index = data.handCards.Count + 1;
        int safeZone = (data.selectedMainColor == CardColor.Blue) ? 2 : 1;

        if (index <= safeZone) return 0f;

        int riskIndex = index - safeZone;
        float rate = riskIndex switch
        {
            1 => 0.05f,
            2 => 0.10f,
            3 => 0.20f,
            4 => 0.35f,
            _ => 0.50f
        };

        return Mathf.Clamp01(rate);
    }

    public static float GetBaseComboMultiplier(int comboCount)
    {
        if (comboCount <= 1) return 1.0f;
        if (comboCount == 2) return 1.5f;
        return 2.0f; // 3连+
    }

    public static List<RuntimeCard> GenerateWeightedDrawPile(List<RuntimeCard> deck, CardColor? mainColor)
    {
        List<RuntimeCard> pile = new List<RuntimeCard>(deck);
        ShuffleList(pile);
        return pile;
    }

    public static void EnsureFirstCardIsMainColor(List<RuntimeCard> drawPile, CardColor? mainColor)
    {
        if (!mainColor.HasValue || drawPile.Count == 0) return;
        int mainColorIndex = drawPile.FindIndex(c => c.color == mainColor.Value);
        if (mainColorIndex > 0)
        {
            var mainCard = drawPile[mainColorIndex];
            drawPile.RemoveAt(mainColorIndex);
            drawPile.Insert(0, mainCard);
        }
    }

    public static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}