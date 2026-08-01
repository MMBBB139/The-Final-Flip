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
        allTargetHands.Add(new TargetHand("对子", TargetHand.Tier.Tier1,
            "2张相同点数（累计）",
            cards => HandEvaluator.HasSameRank(cards, 2)));

        allTargetHands.Add(new TargetHand("双高牌", TargetHand.Tier.Tier1,
            "2张点数≥10（累计）",
            cards => HandEvaluator.HasHighCards(cards, 2)));

        allTargetHands.Add(new TargetHand("颜色交替3连", TargetHand.Tier.Tier1,
            "连续3张颜色交替（非累计，检查最后3张）",
            cards => HandEvaluator.HasColorAlternatingLast(cards, 3)));

        allTargetHands.Add(new TargetHand("三奇数", TargetHand.Tier.Tier1,
            "3张奇数牌（累计）",
            cards => HandEvaluator.HasOddCards(cards, 3)));

        allTargetHands.Add(new TargetHand("点数差≥5", TargetHand.Tier.Tier1,
            "任意2张点数差≥5（累计）",
            cards => HandEvaluator.HasPointDifference(cards, 5)));

        allTargetHands.Add(new TargetHand("四色齐", TargetHand.Tier.Tier1,
            "四种花色各至少一张（累计）",
            cards => HandEvaluator.HasAllFourSuits(cards)));

        // ============ 第二层（预期8-18张）============
        allTargetHands.Add(new TargetHand("四同花", TargetHand.Tier.Tier2,
            "4张相同花色（累计）",
            cards => HandEvaluator.HasSameSuit(cards, 4)));

        allTargetHands.Add(new TargetHand("两对", TargetHand.Tier.Tier2,
            "两组不同点数的对子（累计）",
            cards => HandEvaluator.HasTwoPairs(cards)));

        allTargetHands.Add(new TargetHand("人头对", TargetHand.Tier.Tier2,
            "2张人头牌（J/Q/K，累计）",
            cards => HandEvaluator.HasFaceCards(cards, 2)));

        allTargetHands.Add(new TargetHand("三连升", TargetHand.Tier.Tier2,
            "3张点数严格递增（非累计，检查最后3张）",
            cards => HandEvaluator.HasStrictlyIncreasingLast(cards, 3)));

        allTargetHands.Add(new TargetHand("王牌现身", TargetHand.Tier.Tier2,
            "翻到任意一张A（累计）",
            cards => HandEvaluator.HasAce(cards)));

        allTargetHands.Add(new TargetHand("花色交替4连", TargetHand.Tier.Tier2,
            "连续4张花色各不相同（非累计，检查最后4张）",
            cards => HandEvaluator.HasSuitAlternatingLast(cards, 4)));

        // ============ 第三层（预期18-30张）============
        allTargetHands.Add(new TargetHand("三条", TargetHand.Tier.Tier3,
            "3张相同点数（累计）",
            cards => HandEvaluator.HasSameRank(cards, 3)));

        allTargetHands.Add(new TargetHand("五同花", TargetHand.Tier.Tier3,
            "5张相同花色（累计）",
            cards => HandEvaluator.HasSameSuit(cards, 5)));

        allTargetHands.Add(new TargetHand("小顺子", TargetHand.Tier.Tier3,
            "3张点数连续（累计）",
            cards => HandEvaluator.HasConsecutiveRanks(cards, 3)));

        allTargetHands.Add(new TargetHand("人头同花", TargetHand.Tier.Tier3,
            "2张人头牌且同花色（累计）",
            cards => HandEvaluator.HasFaceCardSameSuit(cards, 2)));

        allTargetHands.Add(new TargetHand("三连奇", TargetHand.Tier.Tier3,
            "连续3张奇数牌（非累计，检查最后3张）",
            cards => HandEvaluator.HasConsecutiveOddLast(cards, 3)));

        allTargetHands.Add(new TargetHand("A带小", TargetHand.Tier.Tier3,
            "任意A + 一张点数≤4（累计）",
            cards => HandEvaluator.HasAceAndSmall(cards)));

        // ============ 第四层（预期30-48张）============
        allTargetHands.Add(new TargetHand("葫芦", TargetHand.Tier.Tier4,
            "3条+1对，不同点数（累计）",
            cards => HandEvaluator.HasFullHouse(cards)));

        allTargetHands.Add(new TargetHand("顺子", TargetHand.Tier.Tier4,
            "5张点数连续（累计）",
            cards => HandEvaluator.HasConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("四条", TargetHand.Tier.Tier4,
            "4张相同点数（累计）",
            cards => HandEvaluator.HasSameRank(cards, 4)));

        allTargetHands.Add(new TargetHand("同花顺", TargetHand.Tier.Tier4,
            "5张同花色且点数连续（累计）",
            cards => HandEvaluator.HasSameSuitConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("红黑配", TargetHand.Tier.Tier4,
            "5张红牌+5张黑牌均已出现（累计）",
            cards => HandEvaluator.HasRedBlackBalance(cards)));

        allTargetHands.Add(new TargetHand("全套点数", TargetHand.Tier.Tier4,
            "全部13种点数各至少一张（累计）",
            cards => HandEvaluator.HasAllRanks(cards)));

        Debug.Log($"初始化完成，共{allTargetHands.Count}种目标牌型");
    }

    public TargetHand SelectRandomTarget(int tier)
    {
        TargetHand.Tier targetTier = (TargetHand.Tier)(tier - 1);
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
        if (currentTarget == null)
        {
            Debug.LogWarning("无当前目标，无法更换");
            return null;
        }

        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == currentTarget.tier
                        && t.handName != currentTarget.handName
                        && !completedTargetNames.Contains(t.handName))
            .ToList();

        if (availableTargets.Count == 0)
        {
            Debug.LogWarning($"第{(int)currentTarget.tier + 1}层没有可替换的目标");
            return null;
        }

        int randomIndex = Random.Range(0, availableTargets.Count);
        TargetHand oldTarget = currentTarget;
        currentTarget = availableTargets[randomIndex];

        Debug.Log($"更换目标：{oldTarget.handName} → {currentTarget.handName}");
        AnnounceTarget();
        return currentTarget;
    }

    /// <summary>
    /// 换目标策略牌：获取同层级随机N个可选目标
    /// </summary>
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
        if (currentTarget == null)
        {
            Debug.LogWarning("无当前目标，无法标记完成");
            return;
        }

        if (!completedTargetNames.Contains(currentTarget.handName))
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
            Debug.Log($"层级：第{(int)currentTarget.tier + 1}层");
            Debug.Log($"════════════════════════════════");
        }
    }

    public bool CheckTarget(List<Card> drawnCards)
    {
        if (currentTarget == null) return false;
        return currentTarget.checkCondition(drawnCards);
    }

    public TargetHand GetCurrentTarget()
    {
        return currentTarget;
    }

    public void ClearCompletedTargets()
    {
        completedTargetNames.Clear();
        currentTarget = null;
        Debug.Log("已清除所有完成记录");
    }
}