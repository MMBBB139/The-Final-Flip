using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HandEvaluator
{
    public static int GetRankValue(Card.Rank rank) => (int)rank;

    public static int GetPointValue(Card.Rank rank) => rank >= Card.Rank.Jack ? 10 : (int)rank;

    public static bool IsOdd(Card.Rank rank) => GetRankValue(rank) % 2 == 1;

    public static bool IsEven(Card.Rank rank) => GetRankValue(rank) % 2 == 0;

    public static bool IsFaceCard(Card.Rank rank) => rank >= Card.Rank.Jack;


    // ============ 通用累计检测 ============

    public static bool HasSameRank(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.rank).Any(g => g.Count() >= count);
    }

    public static bool HasSameSuit(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.suit).Any(g => g.Count() >= count);
    }

    public static bool HasOddCards(List<Card> cards, int count)
    {
        return cards.Count(c => IsOdd(c.rank)) >= count;
    }

    public static bool HasFaceCards(List<Card> cards, int count)
    {
        return cards.Count(c => IsFaceCard(c.rank)) >= count;
    }

    public static bool HasAce(List<Card> cards)
    {
        return cards.Any(c => c.rank == Card.Rank.Ace);
    }

    public static bool HasAllFourSuits(List<Card> cards)
    {
        return cards.Select(c => c.suit).Distinct().Count() >= 4;
    }

    public static bool HasTwoPairs(List<Card> cards)
    {
        return cards.GroupBy(c => c.rank).Count(g => g.Count() >= 2) >= 2;
    }

    public static bool HasAllRanks(List<Card> cards)
    {
        return cards.Select(c => c.rank).Distinct().Count() >= 13;
    }

    public static bool HasConsecutiveRanks(List<Card> cards, int count)
    {
        var ranks = cards.Select(c => GetRankValue(c.rank)).Distinct().OrderBy(r => r).ToList();
        for (int i = 0; i <= ranks.Count - count; i++)
        {
            bool consecutive = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (ranks[i + j] + 1 != ranks[i + j + 1])
                {
                    consecutive = false;
                    break;
                }
            }
            if (consecutive) return true;
        }
        return false;
    }

    public static bool HasFaceCardSameSuit(List<Card> cards, int count)
    {
        return cards.Where(c => IsFaceCard(c.rank))
            .GroupBy(c => c.suit)
            .Any(g => g.Count() >= count);
    }

    public static bool HasSameSuitConsecutiveRanks(List<Card> cards, int count)
    {
        foreach (var suitGroup in cards.GroupBy(c => c.suit))
        {
            var ranks = suitGroup.Select(c => GetRankValue(c.rank)).Distinct().OrderBy(r => r).ToList();
            for (int i = 0; i <= ranks.Count - count; i++)
            {
                bool consecutive = true;
                for (int j = 0; j < count - 1; j++)
                {
                    if (ranks[i + j] + 1 != ranks[i + j + 1])
                    {
                        consecutive = false;
                        break;
                    }
                }
                if (consecutive) return true;
            }
        }
        return false;
    }

    public static bool HasFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.rank).ToList();
        var threeGroup = groups.FirstOrDefault(g => g.Count() >= 3);
        if (threeGroup == null) return false;
        return groups.Any(g => g.Key != threeGroup.Key && g.Count() >= 2);
    }


    // ============ 新增检测方法 ============

    /// <summary>累计翻出指定数量点数≤maxRank的牌</summary>
    public static bool HasCardsRankAtMost(List<Card> cards, int count, int maxRank)
    {
        return cards.Count(c => GetRankValue(c.rank) <= maxRank) >= count;
    }

    /// <summary>红牌≥redCount且黑牌≥blackCount</summary>
    public static bool HasRedBlackBalance(List<Card> cards, int redCount, int blackCount)
    {
        int red = cards.Count(c => c.color == Card.CardColor.Red);
        int black = cards.Count(c => c.color == Card.CardColor.Black);
        return red >= redCount && black >= blackCount;
    }

    /// <summary>累计翻出count张点数连续的偶数牌</summary>
    public static bool HasConsecutiveEvenRanks(List<Card> cards, int count)
    {
        var evenRanks = cards.Where(c => IsEven(c.rank))
            .Select(c => GetRankValue(c.rank))
            .Distinct()
            .OrderBy(r => r)
            .ToList();

        for (int i = 0; i <= evenRanks.Count - count; i++)
        {
            bool consecutive = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (evenRanks[i + j] + 2 != evenRanks[i + j + 1])
                {
                    consecutive = false;
                    break;
                }
            }
            if (consecutive) return true;
        }
        return false;
    }

    /// <summary>四种花色每种至少minCount张</summary>
    public static bool HasAllSuitsCount(List<Card> cards, int minCount)
    {
        var suitCounts = cards.GroupBy(c => c.suit).ToDictionary(g => g.Key, g => g.Count());
        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            if (!suitCounts.ContainsKey(suit) || suitCounts[suit] < minCount)
                return false;
        }
        return true;
    }

    /// <summary>皇家同花顺：同花色的10/J/Q/K/A各一张</summary>
    public static bool HasRoyalFlush(List<Card> cards)
    {
        var royalRanks = new HashSet<int> { 10, 11, 12, 13, 1 }; // 10, J, Q, K, A
        foreach (var suitGroup in cards.GroupBy(c => c.suit))
        {
            var ranks = suitGroup.Select(c => GetRankValue(c.rank)).ToHashSet();
            if (royalRanks.All(r => ranks.Contains(r)))
                return true;
        }
        return false;
    }

    /// <summary>计数检测通用方法：某种条件的牌达到count张</summary>
    public static bool HasCountByCondition(List<Card> cards, int count, System.Func<Card, bool> condition)
    {
        return cards.Count(condition) >= count;
    }
}