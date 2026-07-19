using System.Collections.Generic;
using UnityEngine;

public static class BattleRules
{
    private static readonly Dictionary<int, float> ExplosionRisk = new()
    {
        {5, 0.05f}, {6, 0.15f}, {7, 0.30f}
    };

    public static bool CheckExplosion(int handCount, int safeZoneSize)
    {
        int riskStartIndex = safeZoneSize + 1;
        int index = handCount - riskStartIndex + 5;
        if (index < 5) return false;
        if (index > 8) return Random.value < 0.50f;

        return ExplosionRisk.TryGetValue(index, out float risk) && Random.value < risk;
    }

    public static void ApplyCardColorEffect(RuntimeCard card, BattleData data)
    {
        switch (card.color)
        {
            case CardColor.Blue:
                // 回复1血，满血时自动转护盾
                data.Heal(1);
                break;
            case CardColor.Yellow:
                // 连击倍率+0.1（连击系统待实现）
                break;
            case CardColor.Red:
                // 扣1血，优先消耗护盾
                data.TakeDamage(1);
                data.curseCount++;
                break;
        }
    }

    public static bool ShouldTriggerCurse(BattleData data)
        => data.isCurseReady;

    public static void TriggerCursePenalty(BattleData data)
    {
        data.curseCount = 0;
        var blueCards = data.levelDeck.FindAll(c => c.color == CardColor.Blue);
        if (blueCards.Count > 0)
        {
            int randomIndex = Random.Range(0, blueCards.Count);
            blueCards[randomIndex].color = CardColor.Yellow;
        }
    }

    public static List<RuntimeCard> GenerateWeightedDrawPile(List<RuntimeCard> deck, CardColor? mainColor)
    {
        List<RuntimeCard> pile = new List<RuntimeCard>(deck);

        if (mainColor.HasValue)
        {
            List<RuntimeCard> mainColorCards = pile.FindAll(c => c.color == mainColor.Value);
            pile.AddRange(mainColorCards);
        }

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
        else if (mainColorIndex == -1)
        {
            drawPile[0].color = mainColor.Value;
        }
    }

    private static CardColor GetFallbackColor(List<RuntimeCard> deck)
    {
        return CardColor.Blue;
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