using System.Collections.Generic;
using UnityEngine;

public static class BattleRules
{
    // 爆牌概率表
    private static readonly Dictionary<int, float> ExplosionRisk = new()
    {
        {5, 0.05f}, {6, 0.15f}, {7, 0.30f}
    };

    public static bool CheckExplosion(int handCount)
        => ExplosionRisk.TryGetValue(handCount, out float risk) && Random.value < risk;

    public static void ApplyCardColorEffect(RuntimeCard card, BattleData data)
    {
        switch (card.color)
        {
            case CardColor.Blue:
                data.playerHp += 2;
                break;
            case CardColor.Yellow:
                break;
            case CardColor.Red:
                data.playerHp -= 2;
                data.curseCount++;
                break;
        }
    }

    public static bool ShouldTriggerCurse(BattleData data)
        => data.isCurseReady;

    public static void TriggerCursePenalty(BattleData data)
    {
        data.curseCount = 0;
        foreach (var c in data.drawPile)
            if (c.color == CardColor.Blue) c.color = CardColor.Yellow;
        foreach (var c in data.levelDeck)
            if (c.color == CardColor.Blue) c.color = CardColor.Yellow;
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