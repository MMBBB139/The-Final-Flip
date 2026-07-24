// TargetHandManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TargetHandManager : MonoBehaviour
{
    private List<TargetHand> allTargetHands;     // 所有36种牌型
    private TargetHand currentTarget;            // 当前局目标
    private List<string> usedTargets;            // 已用过的目标（避免重复）

    void Awake()
    {
        usedTargets = new List<string>();
        InitializeAllTargetHands();
    }

    /// <summary>
    /// 初始化全部36种目标牌型
    /// </summary>
    private void InitializeAllTargetHands()
    {
        allTargetHands = new List<TargetHand>();

        // ============ 第一层（预期5-10张）============
        allTargetHands.Add(new TargetHand("对子", TargetHand.Tier.Tier1,
            "2张相同点数",
            cards => HasSameRank(cards, 2)));

        allTargetHands.Add(new TargetHand("三连同色", TargetHand.Tier.Tier1,
            "连续3张相同颜色",
            cards => HasConsecutiveSameColor(cards, 3)));

        allTargetHands.Add(new TargetHand("双高牌", TargetHand.Tier.Tier1,
            "2张点数≥10（10/J/Q/K）",
            cards => cards.Count(c => GetRankValue(c.rank) >= 10) >= 2));

        allTargetHands.Add(new TargetHand("三奇数", TargetHand.Tier.Tier1,
            "3张奇数牌（A/3/5/7/9）",
            cards => cards.Count(c => IsOdd(c.rank)) >= 3));

        allTargetHands.Add(new TargetHand("颜色交替", TargetHand.Tier.Tier1,
            "连续3张颜色交替",
            cards => HasColorAlternating(cards, 3)));

        allTargetHands.Add(new TargetHand("三连偶数", TargetHand.Tier.Tier1,
            "连续3张偶数牌（2/4/6/8/10/Q）",
            cards => HasConsecutiveEven(cards, 3)));

        allTargetHands.Add(new TargetHand("两连高", TargetHand.Tier.Tier1,
            "连续2张点数≥9",
            cards => HasConsecutiveHighCards(cards, 2, 9)));

        allTargetHands.Add(new TargetHand("点数差≥5", TargetHand.Tier.Tier1,
            "任意2张牌点数差≥5",
            cards => HasPointDifference(cards, 5)));

        allTargetHands.Add(new TargetHand("四色齐", TargetHand.Tier.Tier1,
            "四种花色各至少一张",
            cards => HasAllFourSuits(cards)));

        // ============ 第二层（预期8-18张）============
        allTargetHands.Add(new TargetHand("四同花", TargetHand.Tier.Tier2,
            "4张相同花色",
            cards => HasSameSuit(cards, 4)));

        allTargetHands.Add(new TargetHand("人头对", TargetHand.Tier.Tier2,
            "2张人头牌（J/Q/K）",
            cards => cards.Count(c => IsFaceCard(c.rank)) >= 2));

        allTargetHands.Add(new TargetHand("王牌现身", TargetHand.Tier.Tier2,
            "翻到任意一张A",
            cards => cards.Any(c => c.rank == Card.Rank.Ace)));

        allTargetHands.Add(new TargetHand("国王降临", TargetHand.Tier.Tier2,
            "翻到任意一张K",
            cards => cards.Any(c => c.rank == Card.Rank.King)));

        allTargetHands.Add(new TargetHand("两对", TargetHand.Tier.Tier2,
            "两组不同点数的对子",
            cards => HasTwoPairs(cards)));

        allTargetHands.Add(new TargetHand("花色交替", TargetHand.Tier.Tier2,
            "连续4张牌，每张花色不同",
            cards => HasSuitAlternating(cards, 4)));

        allTargetHands.Add(new TargetHand("三连升", TargetHand.Tier.Tier2,
            "3张点数严格递增",
            cards => HasStrictlyIncreasing(cards, 3)));

        allTargetHands.Add(new TargetHand("同点异色", TargetHand.Tier.Tier2,
            "同一数字出现红黑两种颜色",
            cards => HasSameRankDifferentColor(cards)));

        allTargetHands.Add(new TargetHand("四十五点", TargetHand.Tier.Tier2,
            "已翻牌点数之和≥45（A=1, J/Q/K=10）",
            cards => cards.Sum(c => GetPointValue(c.rank)) >= 45));

        // ============ 第三层（预期18-30张）============
        allTargetHands.Add(new TargetHand("三条", TargetHand.Tier.Tier3,
            "3张相同点数",
            cards => HasSameRank(cards, 3)));

        allTargetHands.Add(new TargetHand("小顺子", TargetHand.Tier.Tier3,
            "3张点数连续",
            cards => HasConsecutiveRanks(cards, 3)));

        allTargetHands.Add(new TargetHand("花色+奇偶", TargetHand.Tier.Tier3,
            "3张同花色且同为奇数",
            cards => HasSameSuitAndOdd(cards, 3)));

        allTargetHands.Add(new TargetHand("人头+花色", TargetHand.Tier.Tier3,
            "2张人头牌且同花色",
            cards => HasFaceCardSameSuit(cards, 2)));

        allTargetHands.Add(new TargetHand("五同花", TargetHand.Tier.Tier3,
            "5张相同花色",
            cards => HasSameSuit(cards, 5)));

        allTargetHands.Add(new TargetHand("四连奇", TargetHand.Tier.Tier3,
            "连续4张奇数牌",
            cards => HasConsecutiveOdd(cards, 4)));

        allTargetHands.Add(new TargetHand("单花4连", TargetHand.Tier.Tier3,
            "4张同花色且点数连续",
            cards => HasSameSuitConsecutiveRanks(cards, 4)));

        allTargetHands.Add(new TargetHand("A带小", TargetHand.Tier.Tier3,
            "任意A + 一张点数≤4的牌",
            cards => HasAceAndSmall(cards)));

        allTargetHands.Add(new TargetHand("双面人", TargetHand.Tier.Tier3,
            "J和Q各一张，且同花色",
            cards => HasJackQueenSameSuit(cards)));

        // ============ 第四层（预期30-48张）============
        allTargetHands.Add(new TargetHand("红心女王", TargetHand.Tier.Tier4,
            "翻到唯一的红心Q",
            cards => cards.Any(c => c.suit == Card.Suit.Hearts && c.rank == Card.Rank.Queen)));

        allTargetHands.Add(new TargetHand("葫芦", TargetHand.Tier.Tier4,
            "3条+1对（累计）",
            cards => HasFullHouse(cards)));

        allTargetHands.Add(new TargetHand("全套点数", TargetHand.Tier.Tier4,
            "全部13种点数各至少一张",
            cards => HasAllRanks(cards)));

        allTargetHands.Add(new TargetHand("顺子", TargetHand.Tier.Tier4,
            "5张点数连续",
            cards => HasConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("四条", TargetHand.Tier.Tier4,
            "4张相同点数",
            cards => HasSameRank(cards, 4)));

        allTargetHands.Add(new TargetHand("同花顺", TargetHand.Tier.Tier4,
            "5张同花色且点数连续",
            cards => HasSameSuitConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("红黑配", TargetHand.Tier.Tier4,
            "5张红牌+5张黑牌均已出现",
            cards => HasRedBlackBalance(cards)));

        allTargetHands.Add(new TargetHand("花牌全餐", TargetHand.Tier.Tier4,
            "J/Q/K各至少一张，来自≥3种不同花色",
            cards => HasFaceCardVariety(cards)));

        allTargetHands.Add(new TargetHand("A的四重奏", TargetHand.Tier.Tier4,
            "四种花色的A各一张全部出现",
            cards => HasAllFourAces(cards)));

        Debug.Log($"初始化完成，共{allTargetHands.Count}种目标牌型");
    }

    /// <summary>
    /// 按层级随机选取一个目标（排除本层已用过的）
    /// </summary>
    public TargetHand SelectRandomTarget(int tier)
    {
        TargetHand.Tier targetTier = (TargetHand.Tier)(tier - 1);
        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == targetTier && !usedTargets.Contains(t.handName))
            .ToList();

        if (availableTargets.Count == 0)
        {
            Debug.LogWarning($"第{tier}层所有目标已用完，重置该层已用记录");
            // 重置该层已用记录
            usedTargets.RemoveAll(name =>
                allTargetHands.Any(t => t.handName == name && t.tier == targetTier));
            availableTargets = allTargetHands
                .Where(t => t.tier == targetTier)
                .ToList();
        }

        int randomIndex = Random.Range(0, availableTargets.Count);
        currentTarget = availableTargets[randomIndex];
        usedTargets.Add(currentTarget.handName);

        Debug.Log($"选取目标：{currentTarget.handName}（第{tier}层）");
        AnnounceTarget();
        return currentTarget;
    }

    /// <summary>
    /// 更换为同层级另一个随机目标（改目标底牌用）
    /// </summary>
    public TargetHand ChangeTarget()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("无当前目标，无法更换");
            return null;
        }

        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == currentTarget.tier && t.handName != currentTarget.handName && !usedTargets.Contains(t.handName))
            .ToList();

        if (availableTargets.Count == 0)
        {
            availableTargets = allTargetHands
                .Where(t => t.tier == currentTarget.tier && t.handName != currentTarget.handName)
                .ToList();
        }

        int randomIndex = Random.Range(0, availableTargets.Count);
        usedTargets.Add(availableTargets[randomIndex].handName);
        currentTarget = availableTargets[randomIndex];

        Debug.Log($"更换目标为：{currentTarget.handName}");
        AnnounceTarget();
        return currentTarget;
    }

    /// <summary>
    /// 获取随机双重目标（用于双重目标底牌或第4层特殊规则）
    /// </summary>
    public (TargetHand, TargetHand) GetDoubleTargets()
    {
        if (currentTarget == null) return (null, null);

        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == currentTarget.tier && t.handName != currentTarget.handName)
            .ToList();

        int randomIndex = Random.Range(0, availableTargets.Count);
        TargetHand secondTarget = availableTargets[randomIndex];

        Debug.Log($"双重目标：{currentTarget.handName} + {secondTarget.handName}");
        return (currentTarget, secondTarget);
    }

    /// <summary>
    /// 公布当前目标
    /// </summary>
    public void AnnounceTarget()
    {
        if (currentTarget != null)
        {
            Debug.Log($"════════════════════════════════");
            Debug.Log($"本局目标牌型：{currentTarget.handName}");
            Debug.Log($"达成条件：{currentTarget.description}");
            Debug.Log($"层级：第{(int)currentTarget.tier + 1}层");
            Debug.Log($"════════════════════════════════");
        }
    }

    /// <summary>
    /// 检查已翻牌区是否满足当前目标
    /// </summary>
    public bool CheckTarget(List<Card> drawnCards)
    {
        if (currentTarget == null) return false;
        return currentTarget.checkCondition(drawnCards);
    }

    /// <summary>
    /// 获取当前目标信息
    /// </summary>
    public TargetHand GetCurrentTarget()
    {
        return currentTarget;
    }

    /// <summary>
    /// 清除所有已用记录（新游戏用）
    /// </summary>
    public void ClearUsedTargets()
    {
        usedTargets.Clear();
        currentTarget = null;
    }

    // ============ 辅助检测函数 ============

    private int GetRankValue(Card.Rank rank) => (int)rank;
    private int GetPointValue(Card.Rank rank) => rank >= Card.Rank.Jack ? 10 : (int)rank;

    private bool IsOdd(Card.Rank rank) => GetRankValue(rank) % 2 == 1;
    private bool IsEven(Card.Rank rank) => GetRankValue(rank) % 2 == 0;
    private bool IsFaceCard(Card.Rank rank) => rank >= Card.Rank.Jack;

    private bool HasSameRank(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.rank).Any(g => g.Count() >= count);
    }

    private bool HasSameSuit(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.suit).Any(g => g.Count() >= count);
    }

    private bool HasConsecutiveSameColor(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            bool consecutive = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (cards[i + j].color != cards[i + j + 1].color)
                {
                    consecutive = false;
                    break;
                }
            }
            if (consecutive) return true;
        }
        return false;
    }

    private bool HasColorAlternating(List<Card> cards, int count)
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

    private bool HasConsecutiveEven(List<Card> cards, int count)
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

    private bool HasConsecutiveHighCards(List<Card> cards, int count, int minValue)
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

    private bool HasPointDifference(List<Card> cards, int diff)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            for (int j = i + 1; j < cards.Count; j++)
            {
                if (Mathf.Abs(GetRankValue(cards[i].rank) - GetRankValue(cards[j].rank)) >= diff)
                    return true;
            }
        }
        return false;
    }

    private bool HasAllFourSuits(List<Card> cards)
    {
        return cards.Select(c => c.suit).Distinct().Count() >= 4;
    }

    private bool HasTwoPairs(List<Card> cards)
    {
        var pairs = cards.GroupBy(c => c.rank).Where(g => g.Count() >= 2).Select(g => g.Key);
        return pairs.Count() >= 2;
    }

    private bool HasSuitAlternating(List<Card> cards, int count)
    {
        for (int i = 0; i <= cards.Count - count; i++)
        {
            bool alternating = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (cards[i + j].suit == cards[i + j + 1].suit)
                {
                    alternating = false;
                    break;
                }
            }
            if (alternating) return true;
        }
        return false;
    }

    private bool HasStrictlyIncreasing(List<Card> cards, int count)
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

    private bool HasSameRankDifferentColor(List<Card> cards)
    {
        return cards.GroupBy(c => c.rank)
            .Any(g => g.Select(c => c.color).Distinct().Count() >= 2);
    }

    private bool HasConsecutiveRanks(List<Card> cards, int count)
    {
        var sortedRanks = cards.Select(c => GetRankValue(c.rank)).Distinct().OrderBy(r => r).ToList();
        for (int i = 0; i <= sortedRanks.Count - count; i++)
        {
            bool consecutive = true;
            for (int j = 0; j < count - 1; j++)
            {
                if (sortedRanks[i + j] + 1 != sortedRanks[i + j + 1])
                {
                    consecutive = false;
                    break;
                }
            }
            if (consecutive) return true;
        }
        return false;
    }

    private bool HasSameSuitAndOdd(List<Card> cards, int count)
    {
        return cards.GroupBy(c => c.suit)
            .Any(g => g.Count(c => IsOdd(c.rank)) >= count);
    }

    private bool HasFaceCardSameSuit(List<Card> cards, int count)
    {
        return cards.Where(c => IsFaceCard(c.rank))
            .GroupBy(c => c.suit)
            .Any(g => g.Count() >= count);
    }

    private bool HasConsecutiveOdd(List<Card> cards, int count)
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

    private bool HasSameSuitConsecutiveRanks(List<Card> cards, int count)
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

    private bool HasAceAndSmall(List<Card> cards)
    {
        bool hasAce = cards.Any(c => c.rank == Card.Rank.Ace);
        bool hasSmall = cards.Any(c => GetRankValue(c.rank) <= 4 && c.rank != Card.Rank.Ace);
        return hasAce && hasSmall;
    }

    private bool HasJackQueenSameSuit(List<Card> cards)
    {
        var jacks = cards.Where(c => c.rank == Card.Rank.Jack);
        var queens = cards.Where(c => c.rank == Card.Rank.Queen);
        return jacks.Any(j => queens.Any(q => q.suit == j.suit));
    }

    private bool HasFullHouse(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.rank);
        bool hasThree = groups.Any(g => g.Count() >= 3);
        bool hasPair = groups.Any(g => g.Count() >= 2 && g.Key != groups.First(x => x.Count() >= 3).Key);
        return hasThree && hasPair;
    }

    private bool HasAllRanks(List<Card> cards)
    {
        return cards.Select(c => c.rank).Distinct().Count() >= 13;
    }

    private bool HasRedBlackBalance(List<Card> cards)
    {
        int redCount = cards.Count(c => c.color == Card.CardColor.Red);
        int blackCount = cards.Count(c => c.color == Card.CardColor.Black);
        return redCount >= 5 && blackCount >= 5;
    }

    private bool HasFaceCardVariety(List<Card> cards)
    {
        var faceCards = cards.Where(c => IsFaceCard(c.rank));
        bool hasJ = faceCards.Any(c => c.rank == Card.Rank.Jack);
        bool hasQ = faceCards.Any(c => c.rank == Card.Rank.Queen);
        bool hasK = faceCards.Any(c => c.rank == Card.Rank.King);
        int suitVariety = faceCards.Select(c => c.suit).Distinct().Count();
        return hasJ && hasQ && hasK && suitVariety >= 3;
    }

    private bool HasAllFourAces(List<Card> cards)
    {
        return cards.Where(c => c.rank == Card.Rank.Ace)
            .Select(c => c.suit).Distinct().Count() >= 4;
    }
}