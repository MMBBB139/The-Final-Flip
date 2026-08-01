using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 牌型检测静态工具类，提供所有24种目标牌型的判断逻辑
/// </summary>
public static class HandEvaluator
{
    // ============ 基础辅助方法 ============

    public static int GetRankValue(Card.Rank rank) => (int)rank;

    public static int GetPointValue(Card.Rank rank) => rank >= Card.Rank.Jack ? 10 : (int)rank;

    public static bool IsOdd(Card.Rank rank) => GetRankValue(rank) % 2 == 1;

    public static bool IsEven(Card.Rank rank) => GetRankValue(rank) % 2 == 0;

    public static bool IsFaceCard(Card.Rank rank) => rank >= Card.Rank.Jack;


    // ============ 第1层检测（预期5-10张）============

    public static bool HasSameRank(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.rank).Any(g => g.Count() >= count);
    }

    public static bool HasConsecutiveSameColor(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            bool sameColor = true;
            Card.CardColor firstColor = cards[i].color;
            for (int j = 1; j < count; j++)
            {
                if (cards[i + j].color != firstColor)
                {
                    sameColor = false;
                    break;
                }
            }
            if (sameColor) return true;
        }
        return false;
    }

    public static bool HasHighCards(List<Card> cards, int count)
    {
        return cards.Count(c => GetRankValue(c.rank) >= 10) >= count;
    }

    public static bool HasOddCards(List<Card> cards, int count)
    {
        return cards.Count(c => IsOdd(c.rank)) >= count;
    }

    /// <summary>
    /// 检测序列中是否有连续count张颜色交替（用于整体判断）
    /// </summary>
    public static bool HasColorAlternating(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            bool alternating = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (cards[i + j].color == cards[i + j + 1].color)
                {
                    alternating = false;
                    break;
                }
            }
            if (alternating) return true;
        }
        return false;
    }

    /// <summary>
    /// 检测翻牌序列最后count张是否颜色交替（非累计）
    /// </summary>
    public static bool HasColorAlternatingLast(List<Card> cards, int count)
    {
        if (cards.Count < count) return false;
        return HasColorAlternating(cards.GetRange(cards.Count - count, count), count);
    }

    public static bool HasPointDifference(List<Card> cards, int diff)
    {
        if (cards.Count < 2) return false;
        int maxRank = cards.Max(c => GetRankValue(c.rank));
        int minRank = cards.Min(c => GetRankValue(c.rank));
        return maxRank - minRank >= diff;
    }

    public static bool HasAllFourSuits(List<Card> cards)
    {
        return cards.Select(c => c.suit).Distinct().Count() >= 4;
    }


    // ============ 第2层检测（预期8-18张）============

    public static bool HasSameSuit(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.suit).Any(g => g.Count() >= count);
    }

    public static bool HasFaceCards(List<Card> cards, int count)
    {
        return cards.Count(c => IsFaceCard(c.rank)) >= count;
    }

    public static bool HasAce(List<Card> cards)
    {
        return cards.Any(c => c.rank == Card.Rank.Ace);
    }

    public static bool HasTwoPairs(List<Card> cards)
    {
        return cards.GroupBy(c => c.rank).Count(g => g.Count() >= 2) >= 2;
    }

    /// <summary>
    /// 检测序列中是否有连续count张花色各不相同
    /// </summary>
    public static bool HasSuitAlternating(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            int distinctSuits = cards.Skip(i).Take(count).Select(c => c.suit).Distinct().Count();
            if (distinctSuits == count) return true;
        }
        return false;
    }

    /// <summary>
    /// 检测翻牌序列最后count张是否花色各不相同（非累计）
    /// </summary>
    public static bool HasSuitAlternatingLast(List<Card> cards, int count)
    {
        if (cards.Count < count) return false;
        return HasSuitAlternating(cards.GetRange(cards.Count - count, count), count);
    }

    /// <summary>
    /// 检测序列中是否有count张点数严格递增
    /// </summary>
    public static bool HasStrictlyIncreasing(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            bool increasing = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (GetRankValue(cards[i + j].rank) >= GetRankValue(cards[i + j + 1].rank))
                {
                    increasing = false;
                    break;
                }
            }
            if (increasing) return true;
        }
        return false;
    }

    /// <summary>
    /// 检测翻牌序列最后count张是否严格递增（非累计）
    /// </summary>
    public static bool HasStrictlyIncreasingLast(List<Card> cards, int count)
    {
        if (cards.Count < count) return false;
        return HasStrictlyIncreasing(cards.GetRange(cards.Count - count, count), count);
    }


    // ============ 第3层检测（预期18-30张）============

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

    /// <summary>
    /// 检测翻牌序列最后count张是否连续奇数（非累计）
    /// </summary>
    public static bool HasConsecutiveOddLast(List<Card> cards, int count)
    {
        if (cards.Count < count) return false;
        for (int i = cards.Count - count; i < cards.Count; i++)
        {
            if (!IsOdd(cards[i].rank)) return false;
        }
        return true;
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

    public static bool HasAceAndSmall(List<Card> cards)
    {
        bool hasAce = cards.Any(c => c.rank == Card.Rank.Ace);
        bool hasSmall = cards.Any(c => GetRankValue(c.rank) <= 4 && c.rank != Card.Rank.Ace);
        return hasAce && hasSmall;
    }


    // ============ 第4层检测（预期30-48张）============

    public static bool HasFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.rank).ToList();
        var threeGroup = groups.FirstOrDefault(g => g.Count() >= 3);
        if (threeGroup == null) return false;
        return groups.Any(g => g.Key != threeGroup.Key && g.Count() >= 2);
    }

    public static bool HasAllRanks(List<Card> cards)
    {
        return cards.Select(c => c.rank).Distinct().Count() >= 13;
    }

    public static bool HasRedBlackBalance(List<Card> cards)
    {
        int redCount = cards.Count(c => c.color == Card.CardColor.Red);
        int blackCount = cards.Count(c => c.color == Card.CardColor.Black);
        return redCount >= 5 && blackCount >= 5;
    }
}