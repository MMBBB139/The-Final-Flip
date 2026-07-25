// HandEvaluator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 牌型检测静态工具类，提供所有36种目标牌型的判断逻辑
/// </summary>
public static class HandEvaluator
{
    // ============ 基础辅助方法 ============

    /// <summary>获取点数数值（A=1, 2=2, ..., K=13）</summary>
    public static int GetRankValue(Card.Rank rank) => (int)rank;

    /// <summary>获取计点数值（A=1, 2-9=面值, 10/J/Q/K=10）</summary>
    public static int GetPointValue(Card.Rank rank) => rank >= Card.Rank.Jack ? 10 : (int)rank;

    /// <summary>判断是否为奇数点数</summary>
    public static bool IsOdd(Card.Rank rank) => GetRankValue(rank) % 2 == 1;

    /// <summary>判断是否为偶数点数</summary>
    public static bool IsEven(Card.Rank rank) => GetRankValue(rank) % 2 == 0;

    /// <summary>判断是否为人头牌（J/Q/K）</summary>
    public static bool IsFaceCard(Card.Rank rank) => rank >= Card.Rank.Jack;


    // ============ 第1层检测（预期5-10张）============

    /// <summary>检测是否有指定数量的相同点数（对子/三条/四条）</summary>
    public static bool HasSameRank(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.rank).Any(g => g.Count() >= count);
    }

    /// <summary>检测是否有连续指定数量的相同颜色牌</summary>
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

    /// <summary>检测是否有指定数量的点数≥10的高牌</summary>
    public static bool HasHighCards(List<Card> cards, int count)
    {
        return cards.Count(c => GetRankValue(c.rank) >= 10) >= count;
    }

    /// <summary>检测是否有指定数量的奇数牌</summary>
    public static bool HasOddCards(List<Card> cards, int count)
    {
        return cards.Count(c => IsOdd(c.rank)) >= count;
    }

    /// <summary>检测是否有连续指定数量的颜色交替牌</summary>
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

    /// <summary>检测是否有连续指定数量的偶数牌</summary>
    public static bool HasConsecutiveEven(List<Card> cards, int count)
    {
        int consecutive = 0;
        foreach (Card card in cards)
        {
            if (IsEven(card.rank))
            {
                consecutive++;
                if (consecutive >= count) return true;
            }
            else
            {
                consecutive = 0;
            }
        }
        return false;
    }

    /// <summary>检测是否有连续指定数量的高点数牌（点数≥minValue）</summary>
    public static bool HasConsecutiveHighCards(List<Card> cards, int count, int minValue)
    {
        int consecutive = 0;
        foreach (Card card in cards)
        {
            if (GetRankValue(card.rank) >= minValue)
            {
                consecutive++;
                if (consecutive >= count) return true;
            }
            else
            {
                consecutive = 0;
            }
        }
        return false;
    }

    /// <summary>检测是否存在两张牌点数差≥指定值</summary>
    public static bool HasPointDifference(List<Card> cards, int diff)
    {
        if (cards.Count < 2) return false;
        int maxRank = cards.Max(c => GetRankValue(c.rank));
        int minRank = cards.Min(c => GetRankValue(c.rank));
        return maxRank - minRank >= diff;
    }

    /// <summary>检测四种花色是否各至少一张</summary>
    public static bool HasAllFourSuits(List<Card> cards)
    {
        return cards.Select(c => c.suit).Distinct().Count() >= 4;
    }


    // ============ 第2层检测（预期8-18张）============

    /// <summary>检测是否有指定数量的相同花色</summary>
    public static bool HasSameSuit(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.suit).Any(g => g.Count() >= count);
    }

    /// <summary>检测是否有指定数量的人头牌（J/Q/K）</summary>
    public static bool HasFaceCards(List<Card> cards, int count)
    {
        return cards.Count(c => IsFaceCard(c.rank)) >= count;
    }

    /// <summary>检测是否有任意A</summary>
    public static bool HasAce(List<Card> cards)
    {
        return cards.Any(c => c.rank == Card.Rank.Ace);
    }

    /// <summary>检测是否有任意K</summary>
    public static bool HasKing(List<Card> cards)
    {
        return cards.Any(c => c.rank == Card.Rank.King);
    }

    /// <summary>检测是否有两组不同点数的对子</summary>
    public static bool HasTwoPairs(List<Card> cards)
    {
        return cards.GroupBy(c => c.rank).Count(g => g.Count() >= 2) >= 2;
    }

    /// <summary>检测是否有连续指定数量花色各不相同的牌</summary>
    public static bool HasSuitAlternating(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            int distinctSuits = cards.Skip(i).Take(count).Select(c => c.suit).Distinct().Count();
            if (distinctSuits == count) return true;
        }
        return false;
    }

    /// <summary>检测是否有指定数量点数严格递增的牌</summary>
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

    /// <summary>检测同一数字是否出现红黑两种颜色</summary>
    public static bool HasSameRankDifferentColor(List<Card> cards)
    {
        return cards.GroupBy(c => c.rank)
            .Any(g => g.Select(c => c.color).Distinct().Count() >= 2);
    }

    /// <summary>检测已翻牌点数之和是否≥指定值（A=1, J/Q/K=10）</summary>
    public static bool HasTotalPoints(List<Card> cards, int target)
    {
        return cards.Sum(c => GetPointValue(c.rank)) >= target;
    }


    // ============ 第3层检测（预期18-30张）============

    /// <summary>检测是否有指定数量点数连续的牌（不要求同花）</summary>
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

    /// <summary>检测是否有指定数量同花色且同为奇数的牌</summary>
    public static bool HasSameSuitAndOdd(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.suit)
            .Any(g => g.Count(c => IsOdd(c.rank)) >= count);
    }

    /// <summary>检测是否有指定数量同花色的人头牌</summary>
    public static bool HasFaceCardSameSuit(List<Card> cards, int count)
    {
        return cards.Where(c => IsFaceCard(c.rank))
            .GroupBy(c => c.suit)
            .Any(g => g.Count() >= count);
    }

    /// <summary>检测是否有连续指定数量的奇数牌</summary>
    public static bool HasConsecutiveOdd(List<Card> cards, int count)
    {
        int consecutive = 0;
        foreach (Card card in cards)
        {
            if (IsOdd(card.rank))
            {
                consecutive++;
                if (consecutive >= count) return true;
            }
            else
            {
                consecutive = 0;
            }
        }
        return false;
    }

    /// <summary>检测是否有同花色且点数连续的牌（同花顺）</summary>
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

    /// <summary>检测是否同时有A和点数≤4的牌（A自身除外）</summary>
    public static bool HasAceAndSmall(List<Card> cards)
    {
        bool hasAce = cards.Any(c => c.rank == Card.Rank.Ace);
        bool hasSmall = cards.Any(c => GetRankValue(c.rank) <= 4 && c.rank != Card.Rank.Ace);
        return hasAce && hasSmall;
    }

    /// <summary>检测是否有同花色的J和Q各一张</summary>
    public static bool HasJackQueenSameSuit(List<Card> cards)
    {
        var jackSuits = cards.Where(c => c.rank == Card.Rank.Jack).Select(c => c.suit);
        var queenSuits = cards.Where(c => c.rank == Card.Rank.Queen).Select(c => c.suit);
        return jackSuits.Intersect(queenSuits).Any();
    }


    // ============ 第4层检测（预期30-48张）============

    /// <summary>检测是否有红心Q</summary>
    public static bool HasQueenOfHearts(List<Card> cards)
    {
        return cards.Any(c => c.suit == Card.Suit.Hearts && c.rank == Card.Rank.Queen);
    }

    /// <summary>检测是否同时有3条和1对（葫芦，三条和一对必须是不同点数）</summary>
    public static bool HasFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.rank).ToList();
        var threeGroup = groups.FirstOrDefault(g => g.Count() >= 3);
        if (threeGroup == null) return false;
        return groups.Any(g => g.Key != threeGroup.Key && g.Count() >= 2);
    }

    /// <summary>检测是否全部13种点数各至少一张</summary>
    public static bool HasAllRanks(List<Card> cards)
    {
        return cards.Select(c => c.rank).Distinct().Count() >= 13;
    }

    /// <summary>检测红牌和黑牌是否各至少5张</summary>
    public static bool HasRedBlackBalance(List<Card> cards)
    {
        int redCount = cards.Count(c => c.color == Card.CardColor.Red);
        int blackCount = cards.Count(c => c.color == Card.CardColor.Black);
        return redCount >= 5 && blackCount >= 5;
    }

    /// <summary>检测J/Q/K各至少一张且来自≥3种不同花色</summary>
    public static bool HasFaceCardVariety(List<Card> cards)
    {
        var faceCards = cards.Where(c => IsFaceCard(c.rank)).ToList();
        bool hasJ = faceCards.Any(c => c.rank == Card.Rank.Jack);
        bool hasQ = faceCards.Any(c => c.rank == Card.Rank.Queen);
        bool hasK = faceCards.Any(c => c.rank == Card.Rank.King);
        int suitCount = faceCards.Select(c => c.suit).Distinct().Count();
        return hasJ && hasQ && hasK && suitCount >= 3;
    }

    /// <summary>检测是否四种花色的A各一张全部出现</summary>
    public static bool HasAllFourAces(List<Card> cards)
    {
        return cards.Where(c => c.rank == Card.Rank.Ace)
            .Select(c => c.suit).Distinct().Count() >= 4;
    }
}