using System.Collections.Generic;
using System.Linq;

// 牌型枚举（按优先级从低到高）
public enum PokerHandType
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush
}

public static class PokerHandEvaluator
{
    // 获取最终牌型和对应的伤害加成
    public static int GetHandBonus(List<RuntimeCard> handCards, out PokerHandType bestHand)
    {
        bestHand = Evaluate(handCards);
        switch (bestHand)
        {
            case PokerHandType.StraightFlush: return 85;
            case PokerHandType.FourOfAKind: return 70;
            case PokerHandType.FullHouse: return 55;
            case PokerHandType.Flush: return 40;
            case PokerHandType.Straight: return 35;
            case PokerHandType.ThreeOfAKind: return 25;
            case PokerHandType.TwoPair: return 15;
            case PokerHandType.Pair: return 8;
            default: return 0; // HighCard 没有加成
        }
    }

    public static string GetHandName(PokerHandType type)
    {
        switch (type)
        {
            case PokerHandType.StraightFlush: return "StraightFlush";
            case PokerHandType.FourOfAKind: return "FourOfAKind";
            case PokerHandType.FullHouse: return "FullHouse";
            case PokerHandType.Flush: return "Flush";
            case PokerHandType.Straight: return "Straight";
            case PokerHandType.ThreeOfAKind: return "ThreeOfAKind";
            case PokerHandType.TwoPair: return "TwoPair";
            case PokerHandType.Pair: return "Pair";
            default: return "";
        }
    }

    private static PokerHandType Evaluate(List<RuntimeCard> cards)
    {
        if (cards == null || cards.Count < 2) return PokerHandType.HighCard;

        List<int> ranks = new List<int>();
        Dictionary<CardSuit, List<int>> suitMap = new Dictionary<CardSuit, List<int>>();
        Dictionary<int, int> rankCounts = new Dictionary<int, int>();

        foreach (var card in cards)
        {
            int r = card.baseData.rank;
            CardSuit s = card.baseData.suit;

            ranks.Add(r);

            if (!suitMap.ContainsKey(s)) suitMap[s] = new List<int>();
            suitMap[s].Add(r);

            if (!rankCounts.ContainsKey(r)) rankCounts[r] = 0;
            rankCounts[r]++;
        }

        // 1. 同花顺 & 同花 判定
        bool hasFlush = false;
        bool hasStraightFlush = false;

        foreach (var kvp in suitMap)
        {
            if (kvp.Value.Count >= 5)
            {
                hasFlush = true;
                // 如果这5张同花牌里还能凑出顺子，那就是同花顺
                if (HasStraight(kvp.Value))
                {
                    hasStraightFlush = true;
                    break;
                }
            }
        }

        if (hasStraightFlush) return PokerHandType.StraightFlush;

        // 2. 统计相同点数
        bool hasFour = false;
        bool hasThree = false;
        int pairCount = 0;

        foreach (var count in rankCounts.Values)
        {
            if (count >= 4) hasFour = true;
            else if (count == 3) hasThree = true;
            else if (count == 2) pairCount++;
        }

        // 四条判定
        if (hasFour) return PokerHandType.FourOfAKind;

        // 3. 葫芦判定 (三条+对子，或者有两个三条)
        if ((hasThree && pairCount >= 1) || rankCounts.Values.Count(c => c >= 3) >= 2)
            return PokerHandType.FullHouse;

        // 4. 同花判定
        if (hasFlush) return PokerHandType.Flush;

        // 5. 顺子判定
        if (HasStraight(ranks)) return PokerHandType.Straight;

        // 6. 三条判定
        if (hasThree) return PokerHandType.ThreeOfAKind;

        // 7. 两对判定
        if (pairCount >= 2) return PokerHandType.TwoPair;

        // 8. 一对判定
        if (pairCount == 1) return PokerHandType.Pair;

        return PokerHandType.HighCard;
    }

    // 判断顺子逻辑
    private static bool HasStraight(List<int> ranks)
    {
        if (ranks.Count < 5) return false;

        // 去重并排序
        var distinctRanks = ranks.Distinct().OrderBy(r => r).ToList();

        // 特殊处理 A-2-3-4-5 的情况 (假定 A 是 14)
        if (distinctRanks.Contains(14))
        {
            distinctRanks.Insert(0, 1); // 虚拟插入一个1用于验证顺子
        }

        int consecutiveCount = 1;
        for (int i = 0; i < distinctRanks.Count - 1; i++)
        {
            if (distinctRanks[i + 1] == distinctRanks[i] + 1)
            {
                consecutiveCount++;
                if (consecutiveCount >= 5) return true;
            }
            else
            {
                consecutiveCount = 1; // 断开，重新计数
            }
        }

        return false;
    }
}