using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    // 三色牌的数值范围定义
    private static readonly Dictionary<CardColor, (int min, int max)> ColorRanges = new()
    {
        { CardColor.Blue, (1, 2) },
        { CardColor.Yellow, (3, 4) },
        { CardColor.Red, (5, 6) }
    };

    // 初始牌组颜色比例
    private static readonly Dictionary<CardColor, float> ColorRatios = new()
    {
        { CardColor.Blue, 0.34f },
        { CardColor.Yellow, 0.33f },
        { CardColor.Red, 0.33f }
    };

    /// <summary>
    /// 生成初始牌组（纯代码，不需要任何SO）
    /// </summary>
    public List<RuntimeCard> GenerateInitialDeck(int totalCards = 12)
    {
        List<RuntimeCard> deck = new List<RuntimeCard>();

        // 1. 计算各颜色数量
        int blueCount = Mathf.RoundToInt(totalCards * ColorRatios[CardColor.Blue]);
        int yellowCount = Mathf.RoundToInt(totalCards * ColorRatios[CardColor.Yellow]);
        int redCount = totalCards - blueCount - yellowCount;

        // 2. 按颜色生成牌
        GenerateCardsOfColor(deck, CardColor.Blue, blueCount);
        GenerateCardsOfColor(deck, CardColor.Yellow, yellowCount);
        GenerateCardsOfColor(deck, CardColor.Red, redCount);

        // 3. 洗牌
        ShuffleList(deck);

        return deck;
    }

    /// <summary>
    /// 生成指定颜色和数量的牌
    /// </summary>
    private void GenerateCardsOfColor(List<RuntimeCard> deck, CardColor color, int count)
    {
        var (min, max) = ColorRanges[color];
        for (int i = 0; i < count; i++)
        {
            int rank = Random.Range(min, max + 1);
            deck.Add(new RuntimeCard(color, rank));
        }
    }

    /// <summary>
    /// 生成一张指定颜色的随机牌（用于层间奖励加入新牌）
    /// </summary>
    public RuntimeCard GenerateRandomCard(CardColor color)
    {
        var (min, max) = ColorRanges[color];
        int rank = Random.Range(min, max + 1);
        return new RuntimeCard(color, rank);
    }

    // Fisher-Yates洗牌
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}