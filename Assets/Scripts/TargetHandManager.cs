// TargetHandManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TargetHandManager : MonoBehaviour
{
    private List<TargetHand> allTargetHands;         // 所有36种牌型
    private TargetHand currentTarget;                // 当前局目标
    private List<string> completedTargetNames;       // 已完成（结算）的目标名称，永久排除

    void Awake()
    {
        completedTargetNames = new List<string>();
        InitializeAllTargetHands();
    }

    /// <summary>
    /// 初始化全部36种目标牌型（检测逻辑委托给HandEvaluator）
    /// </summary>
    private void InitializeAllTargetHands()
    {
        allTargetHands = new List<TargetHand>();

        // ============ 第一层（预期5-10张）============
        allTargetHands.Add(new TargetHand("对子", TargetHand.Tier.Tier1,
            "2张相同点数",
            cards => HandEvaluator.HasSameRank(cards, 2)));

        allTargetHands.Add(new TargetHand("三连同色", TargetHand.Tier.Tier1,
            "连续3张相同颜色",
            cards => HandEvaluator.HasConsecutiveSameColor(cards, 3)));

        allTargetHands.Add(new TargetHand("双高牌", TargetHand.Tier.Tier1,
            "2张点数≥10（10/J/Q/K）",
            cards => HandEvaluator.HasHighCards(cards, 2)));

        allTargetHands.Add(new TargetHand("三奇数", TargetHand.Tier.Tier1,
            "3张奇数牌（A/3/5/7/9）",
            cards => HandEvaluator.HasOddCards(cards, 3)));

        allTargetHands.Add(new TargetHand("颜色交替", TargetHand.Tier.Tier1,
            "连续3张颜色交替",
            cards => HandEvaluator.HasColorAlternating(cards, 3)));

        allTargetHands.Add(new TargetHand("三连偶数", TargetHand.Tier.Tier1,
            "连续3张偶数牌（2/4/6/8/10/Q）",
            cards => HandEvaluator.HasConsecutiveEven(cards, 3)));

        allTargetHands.Add(new TargetHand("两连高", TargetHand.Tier.Tier1,
            "连续2张点数≥9",
            cards => HandEvaluator.HasConsecutiveHighCards(cards, 2, 9)));

        allTargetHands.Add(new TargetHand("点数差≥5", TargetHand.Tier.Tier1,
            "任意2张牌点数差≥5",
            cards => HandEvaluator.HasPointDifference(cards, 5)));

        allTargetHands.Add(new TargetHand("四色齐", TargetHand.Tier.Tier1,
            "四种花色各至少一张",
            cards => HandEvaluator.HasAllFourSuits(cards)));

        // ============ 第二层（预期8-18张）============
        allTargetHands.Add(new TargetHand("四同花", TargetHand.Tier.Tier2,
            "4张相同花色",
            cards => HandEvaluator.HasSameSuit(cards, 4)));

        allTargetHands.Add(new TargetHand("人头对", TargetHand.Tier.Tier2,
            "2张人头牌（J/Q/K）",
            cards => HandEvaluator.HasFaceCards(cards, 2)));

        allTargetHands.Add(new TargetHand("王牌现身", TargetHand.Tier.Tier2,
            "翻到任意一张A",
            cards => HandEvaluator.HasAce(cards)));

        allTargetHands.Add(new TargetHand("国王降临", TargetHand.Tier.Tier2,
            "翻到任意一张K",
            cards => HandEvaluator.HasKing(cards)));

        allTargetHands.Add(new TargetHand("两对", TargetHand.Tier.Tier2,
            "两组不同点数的对子",
            cards => HandEvaluator.HasTwoPairs(cards)));

        allTargetHands.Add(new TargetHand("花色交替", TargetHand.Tier.Tier2,
            "连续4张牌，每张花色不同",
            cards => HandEvaluator.HasSuitAlternating(cards, 4)));

        allTargetHands.Add(new TargetHand("三连升", TargetHand.Tier.Tier2,
            "3张点数严格递增",
            cards => HandEvaluator.HasStrictlyIncreasing(cards, 3)));

        allTargetHands.Add(new TargetHand("同点异色", TargetHand.Tier.Tier2,
            "同一数字出现红黑两种颜色",
            cards => HandEvaluator.HasSameRankDifferentColor(cards)));

        allTargetHands.Add(new TargetHand("四十五点", TargetHand.Tier.Tier2,
            "已翻牌点数之和≥45（A=1, J/Q/K=10）",
            cards => HandEvaluator.HasTotalPoints(cards, 45)));

        // ============ 第三层（预期18-30张）============
        allTargetHands.Add(new TargetHand("三条", TargetHand.Tier.Tier3,
            "3张相同点数",
            cards => HandEvaluator.HasSameRank(cards, 3)));

        allTargetHands.Add(new TargetHand("小顺子", TargetHand.Tier.Tier3,
            "3张点数连续",
            cards => HandEvaluator.HasConsecutiveRanks(cards, 3)));

        allTargetHands.Add(new TargetHand("花色+奇偶", TargetHand.Tier.Tier3,
            "3张同花色且同为奇数",
            cards => HandEvaluator.HasSameSuitAndOdd(cards, 3)));

        allTargetHands.Add(new TargetHand("人头+花色", TargetHand.Tier.Tier3,
            "2张人头牌且同花色",
            cards => HandEvaluator.HasFaceCardSameSuit(cards, 2)));

        allTargetHands.Add(new TargetHand("五同花", TargetHand.Tier.Tier3,
            "5张相同花色",
            cards => HandEvaluator.HasSameSuit(cards, 5)));

        allTargetHands.Add(new TargetHand("四连奇", TargetHand.Tier.Tier3,
            "连续4张奇数牌",
            cards => HandEvaluator.HasConsecutiveOdd(cards, 4)));

        allTargetHands.Add(new TargetHand("单花4连", TargetHand.Tier.Tier3,
            "4张同花色且点数连续",
            cards => HandEvaluator.HasSameSuitConsecutiveRanks(cards, 4)));

        allTargetHands.Add(new TargetHand("A带小", TargetHand.Tier.Tier3,
            "任意A + 一张点数≤4的牌",
            cards => HandEvaluator.HasAceAndSmall(cards)));

        allTargetHands.Add(new TargetHand("双面人", TargetHand.Tier.Tier3,
            "J和Q各一张，且同花色",
            cards => HandEvaluator.HasJackQueenSameSuit(cards)));

        // ============ 第四层（预期30-48张）============
        allTargetHands.Add(new TargetHand("红心女王", TargetHand.Tier.Tier4,
            "翻到唯一的红心Q",
            cards => HandEvaluator.HasQueenOfHearts(cards)));

        allTargetHands.Add(new TargetHand("葫芦", TargetHand.Tier.Tier4,
            "3条+1对（累计，且三条和对子点数不同）",
            cards => HandEvaluator.HasFullHouse(cards)));

        allTargetHands.Add(new TargetHand("全套点数", TargetHand.Tier.Tier4,
            "全部13种点数各至少一张",
            cards => HandEvaluator.HasAllRanks(cards)));

        allTargetHands.Add(new TargetHand("顺子", TargetHand.Tier.Tier4,
            "5张点数连续",
            cards => HandEvaluator.HasConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("四条", TargetHand.Tier.Tier4,
            "4张相同点数",
            cards => HandEvaluator.HasSameRank(cards, 4)));

        allTargetHands.Add(new TargetHand("同花顺", TargetHand.Tier.Tier4,
            "5张同花色且点数连续",
            cards => HandEvaluator.HasSameSuitConsecutiveRanks(cards, 5)));

        allTargetHands.Add(new TargetHand("红黑配", TargetHand.Tier.Tier4,
            "5张红牌+5张黑牌均已出现",
            cards => HandEvaluator.HasRedBlackBalance(cards)));

        allTargetHands.Add(new TargetHand("花牌全餐", TargetHand.Tier.Tier4,
            "J/Q/K各至少一张，来自≥3种不同花色",
            cards => HandEvaluator.HasFaceCardVariety(cards)));

        allTargetHands.Add(new TargetHand("A的四重奏", TargetHand.Tier.Tier4,
            "四种花色的A各一张全部出现",
            cards => HandEvaluator.HasAllFourAces(cards)));

        Debug.Log($"初始化完成，共{allTargetHands.Count}种目标牌型");
    }

    /// <summary>
    /// 按层级随机选取一个目标（排除已结算的）
    /// </summary>
    public TargetHand SelectRandomTarget(int tier)
    {
        TargetHand.Tier targetTier = (TargetHand.Tier)(tier - 1);
        List<TargetHand> availableTargets = allTargetHands
            .Where(t => t.tier == targetTier && !completedTargetNames.Contains(t.handName))
            .ToList();

        if (availableTargets.Count == 0)
        {
            Debug.LogWarning($"第{tier}层所有目标已完成结算，无可用目标");
            return null;
        }

        int randomIndex = Random.Range(0, availableTargets.Count);
        currentTarget = availableTargets[randomIndex];

        Debug.Log($"选取目标：{currentTarget.handName}（第{tier}层）");
        AnnounceTarget();
        return currentTarget;
    }

    /// <summary>
    /// 更换为同层级另一个随机目标（与当前目标不同，排除已结算的，但之前换掉的目标可能重新出现）
    /// </summary>
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
            Debug.LogWarning($"第{(int)currentTarget.tier + 1}层没有可替换的目标（其他目标均已结算）");
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
    /// 获取双重目标（第二个目标与第一个不同，且都不能是已结算的）
    /// </summary>
    public (TargetHand, TargetHand) GetDoubleTargets()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("无当前目标，无法获取双重目标");
            return (null, null);
        }

        List<TargetHand> availableForSecond = allTargetHands
            .Where(t => t.tier == currentTarget.tier
                        && t.handName != currentTarget.handName
                        && !completedTargetNames.Contains(t.handName))
            .ToList();

        if (availableForSecond.Count == 0)
        {
            Debug.LogWarning($"第{(int)currentTarget.tier + 1}层没有其他可用目标作为第二目标");
            return (currentTarget, null);
        }

        int randomIndex = Random.Range(0, availableForSecond.Count);
        TargetHand secondTarget = availableForSecond[randomIndex];

        Debug.Log($"双重目标：{currentTarget.handName} + {secondTarget.handName}");
        return (currentTarget, secondTarget);
    }

    /// <summary>
    /// 标记当前目标为已完成（结算后调用，永久排除）
    /// </summary>
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

    /// <summary>
    /// 公布当前目标（支持双重目标）
    /// </summary>
    public void AnnounceTarget(TargetHand secondTarget = null)
    {
        if (currentTarget != null)
        {
            Debug.Log($"════════════════════════════════");

            if (secondTarget != null)
            {
                Debug.Log($"本局为双重目标！");
                Debug.Log($"目标一：{currentTarget.handName} - {currentTarget.description}");
                Debug.Log($"目标二：{secondTarget.handName} - {secondTarget.description}");
            }
            else
            {
                Debug.Log($"本局目标牌型：{currentTarget.handName}");
                Debug.Log($"达成条件：{currentTarget.description}");
            }

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
    /// 清除所有已完成记录（新游戏用）
    /// </summary>
    public void ClearCompletedTargets()
    {
        completedTargetNames.Clear();
        currentTarget = null;
        Debug.Log("已清除所有完成记录");
    }
}