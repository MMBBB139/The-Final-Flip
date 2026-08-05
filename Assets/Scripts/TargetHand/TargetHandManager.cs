using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TargetHandManager : MonoBehaviour
{
    private List<TargetHand> allTargetHands;
    private TargetHand currentTarget;
    private List<string> completedTargetNames;

    void Awake()
    {
        completedTargetNames = new List<string>();
        InitializeAllTargetHands();
    }

    private void InitializeAllTargetHands()
    {
        allTargetHands = new List<TargetHand>();

        // ============ 第一层（预期5-10张）============
        allTargetHands.Add(new TargetHand("三奇数", TargetHand.Tier.Tier1,
            "累计翻出3张奇数牌", 5,
            cards => HandEvaluator.HasOddCards(cards, 3)));

        allTargetHands.Add(new TargetHand("小三连", TargetHand.Tier.Tier1,
            "累计翻出3张点数≤4的牌", 9,
            cards => HandEvaluator.HasCardsRankAtMost(cards, 3, 4)));

        allTargetHands.Add(new TargetHand("同花三张", TargetHand.Tier.Tier1,
            "累计翻出3张相同花色的牌", 8,
            cards => HandEvaluator.HasSameSuit(cards, 3)));

        allTargetHands.Add(new TargetHand("红黑3-3", TargetHand.Tier.Tier1,
            "累计翻出3张红牌和3张黑牌", 7,
            cards => HandEvaluator.HasRedBlackBalance(cards, 3, 3)));

        allTargetHands.Add(new TargetHand("四色齐", TargetHand.Tier.Tier1,
            "累计翻出四种花色各至少一张", 8,
            cards => HandEvaluator.HasAllFourSuits(cards)));

        allTargetHands.Add(new TargetHand("三连数", TargetHand.Tier.Tier1,
            "累计翻出3张点数连续的牌", 10,
            cards => HandEvaluator.HasConsecutiveRanks(cards, 3)));

        // ============ 第二层（预期7-18张）============
        allTargetHands.Add(new TargetHand("四奇数", TargetHand.Tier.Tier2,
            "累计翻出4张奇数牌", 7,
            cards => HandEvaluator.HasOddCards(cards, 4)));

        allTargetHands.Add(new TargetHand("小四连", TargetHand.Tier.Tier2,
            "累计翻出4张点数≤6的牌", 8,
            cards => HandEvaluator.HasCardsRankAtMost(cards, 4, 6)));

        allTargetHands.Add(new TargetHand("人头三张", TargetHand.Tier.Tier2,
            "累计翻出3张人头牌（J/Q/K）", 13,
            cards => HandEvaluator.HasFaceCards(cards, 3)));

        allTargetHands.Add(new TargetHand("两对", TargetHand.Tier.Tier2,
            "累计翻出两组各2张相同点数（两组点数不同）", 15,
            cards => HandEvaluator.HasTwoPairs(cards)));

        allTargetHands.Add(new TargetHand("三条", TargetHand.Tier.Tier2,
            "累计翻出3张相同点数的牌", 16,
            cards => HandEvaluator.HasSameRank(cards, 3)));

        allTargetHands.Add(new TargetHand("五同花", TargetHand.Tier.Tier2,
            "累计翻出5张相同花色的牌", 18,
            cards => HandEvaluator.HasSameSuit(cards, 5)));

        // ============ 第三层（预期18-30张）============
        allTargetHands.Add(new TargetHand("四连数", TargetHand.Tier.Tier3,
            "累计翻出4张点数连续的牌", 18,
            cards => HandEvaluator.HasConsecutiveRanks(cards, 4)));

        allTargetHands.Add(new TargetHand("六同花", TargetHand.Tier.Tier3,
            "累计翻出6张相同花色的牌", 24,
            cards => HandEvaluator.HasSameSuit(cards, 6)));

        allTargetHands.Add(new TargetHand("偶数四连", TargetHand.Tier.Tier3,
            "累计翻出4张点数连续的偶数牌", 26,
            cards => HandEvaluator.HasConsecutiveEvenRanks(cards, 4)));

        allTargetHands.Add(new TargetHand("五连数", TargetHand.Tier.Tier3,
            "累计翻出5张点数连续的牌", 26,
            cards => HandEvaluator.HasConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("人头同花三张", TargetHand.Tier.Tier3,
            "累计翻出3张人头牌且同花色", 28,
            cards => HandEvaluator.HasFaceCardSameSuit(cards, 3)));

        allTargetHands.Add(new TargetHand("四条", TargetHand.Tier.Tier3,
            "累计翻出4张相同点数的牌", 30,
            cards => HandEvaluator.HasSameRank(cards, 4)));

        // ============ 第四层 & 第五层（预期32-46张）============
        allTargetHands.Add(new TargetHand("葫芦", TargetHand.Tier.Tier4,
            "累计翻出3张相同点数+另2张相同点数（两组点数不同）", 32,
            cards => HandEvaluator.HasFullHouse(cards)));

        allTargetHands.Add(new TargetHand("四色各三", TargetHand.Tier.Tier4,
            "累计翻出四种花色各至少3张", 35,
            cards => HandEvaluator.HasAllSuitsCount(cards, 3)));

        allTargetHands.Add(new TargetHand("六连数", TargetHand.Tier.Tier4,
            "累计翻出6张点数连续的牌", 36,
            cards => HandEvaluator.HasConsecutiveRanks(cards, 6)));

        allTargetHands.Add(new TargetHand("全套点数", TargetHand.Tier.Tier4,
            "累计翻出全部13种点数各至少一张", 40,
            cards => HandEvaluator.HasAllRanks(cards)));

        allTargetHands.Add(new TargetHand("同花顺", TargetHand.Tier.Tier4,
            "累计翻出5张同花色且点数连续的牌", 42,
            cards => HandEvaluator.HasSameSuitConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("皇家同花顺", TargetHand.Tier.Tier4,
            "累计翻出10/J/Q/K/A同花色的5张牌", 46,
            cards => HandEvaluator.HasRoyalFlush(cards)));

        Debug.Log($"初始化完成，共{allTargetHands.Count}种目标牌型");
    }

    public TargetHand SelectRandomTarget(int tier)
    {
        TargetHand.Tier targetTier = (TargetHand.Tier)(tier - 1);
        // 第5层使用第4层的牌型池
        if (tier == 5) targetTier = TargetHand.Tier.Tier4;

        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == targetTier && !completedTargetNames.Contains(t.handName))
            .ToList();

        if (availableTargets.Count == 0)
        {
            Debug.LogWarning($"第{tier}层所有目标已完成，无可用目标");
            return null;
        }

        int randomIndex = Random.Range(0, availableTargets.Count);
        currentTarget = availableTargets[randomIndex];

        Debug.Log($"选取目标：{currentTarget.handName}（第{tier}层）");
        AnnounceTarget();
        return currentTarget;
    }

    public TargetHand ChangeTarget()
    {
        if (currentTarget == null) return null;

        TargetHand.Tier targetTier = currentTarget.tier;

        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == targetTier
                        && t.handName != currentTarget.handName
                        && !completedTargetNames.Contains(t.handName))
            .ToList();

        if (availableTargets.Count == 0) return null;

        int randomIndex = Random.Range(0, availableTargets.Count);
        TargetHand oldTarget = currentTarget;
        currentTarget = availableTargets[randomIndex];

        Debug.Log($"更换目标：{oldTarget.handName} → {currentTarget.handName}");
        AnnounceTarget();
        return currentTarget;
    }

    public List<TargetHand> GetAlternativeTargets(int count)
    {
        if (currentTarget == null) return new List<TargetHand>();

        return allTargetHands
            .Where(t => t.tier == currentTarget.tier
                        && t.handName != currentTarget.handName
                        && !completedTargetNames.Contains(t.handName))
            .OrderBy(_ => Random.value)
            .Take(count)
            .ToList();
    }

    public void SetCurrentTarget(TargetHand target)
    {
        currentTarget = target;
        AnnounceTarget();
    }

    public void MarkCurrentTargetAsCompleted()
    {
        if (currentTarget != null && !completedTargetNames.Contains(currentTarget.handName))
        {
            completedTargetNames.Add(currentTarget.handName);
            Debug.Log($"目标 [{currentTarget.handName}] 已完成结算，永久排除");
        }
    }

    public void MarkTargetAsCompleted(string handName)
    {
        if (!completedTargetNames.Contains(handName))
        {
            completedTargetNames.Add(handName);
            Debug.Log($"目标 [{handName}] 已完成结算，永久排除");
        }
    }

    public void AnnounceTarget(TargetHand secondTarget = null)
    {
        if (currentTarget != null)
        {
            Debug.Log($"════════════════════════════════");
            Debug.Log($"本局目标牌型：{currentTarget.handName}");
            Debug.Log($"达成条件：{currentTarget.description}");
            Debug.Log($"预期翻牌数：{currentTarget.expectedDraws}张");
            Debug.Log($"层级：第{(int)currentTarget.tier + 1}层");
            Debug.Log($"════════════════════════════════");
        }
    }

    public bool CheckTarget(List<Card> drawnCards)
    {
        if (currentTarget == null) return false;
        return currentTarget.checkCondition(drawnCards);
    }

    public TargetHand GetCurrentTarget() => currentTarget;

    /// <summary>
    /// 获取指定层级的所有可用目标
    /// </summary>
    public List<TargetHand> GetTargetsByTier(int tier)
    {
        TargetHand.Tier targetTier = (TargetHand.Tier)(tier - 1);
        if (tier == 5) targetTier = TargetHand.Tier.Tier4;
        return allTargetHands
            .Where(t => t.tier == targetTier && !completedTargetNames.Contains(t.handName))
            .ToList();
    }

    public void ClearCompletedTargets()
    {
        completedTargetNames.Clear();
        currentTarget = null;
    }
}