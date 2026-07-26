using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 规则管理器 - 仅管理4个特殊规则关（每层第3关）
/// </summary>
public class RuleManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private StrategyCardManager strategyCardManager;
    [SerializeField] private CorrectionManager correctionManager;
    [SerializeField] private LevelManager levelManager;

    [Header("规则状态")]
    private int currentLayer;
    private bool isSpecialStage;

    // 第3层特殊规则：褪色牌相关
    private bool nextCardIsFaded;
    private bool observeCardsDisabled;

    // 第4层特殊规则：双重目标相关
    private TargetHand secondTarget;
    private Card revealedCardForTarget1;
    private Card revealedCardForTarget2;
    private int revealedCardPosition1;
    private int revealedCardPosition2;

    // 观察类策略牌名称列表
    private static readonly HashSet<string> ObserveCardNames = new HashSet<string>
    {
        "偷看顶牌", "偷看中间", "偷看底牌", "定点找牌", "提前验货"
    };

    void Awake()
    {
        if (correctionManager != null)
        {
            correctionManager.OnCorrectionUsed.AddListener(OnCorrectionUsed);
        }
    }

    void OnDestroy()
    {
        if (correctionManager != null)
        {
            correctionManager.OnCorrectionUsed.RemoveListener(OnCorrectionUsed);
        }
    }

    /// <summary>
    /// 关卡开始时初始化规则
    /// </summary>
    public void InitializeForStage(int layer, int stage)
    {
        currentLayer = layer;
        isSpecialStage = (stage == 3);

        // 重置所有状态
        nextCardIsFaded = false;
        observeCardsDisabled = false;
        secondTarget = null;
        revealedCardForTarget1 = null;
        revealedCardForTarget2 = null;

        if (isSpecialStage)
        {
            Debug.Log($"[规则] 第{layer}层第{stage}关 - 特殊规则: {GetSpecialRuleDesc(layer)}");

            // 第4层特殊规则：开局即设置双重目标
            if (layer == 4)
            {
                ApplyDoubleTargetRule();
            }
        }
        else
        {
            Debug.Log($"[规则] 第{layer}层第{stage}关 - 普通关，无特殊规则");
        }
    }

    private string GetSpecialRuleDesc(int layer)
    {
        switch (layer)
        {
            case 1: return "使用修正后，已翻开最后3张洗回牌堆";
            case 2: return "修正窗口提前关闭（翻到N-3张时关闭）";
            case 3: return "使用策略牌后下一张为褪色牌；翻到褪色牌后禁用观察类策略牌";
            case 4: return "双重目标，两个都须达成，各揭示一张明牌";
            default: return "未知";
        }
    }

    // ==================== 策略牌使用回调 ====================

    /// <summary>
    /// 检查策略牌是否可用（第3层特殊规则：翻过褪色牌后禁用观察类）
    /// </summary>
    public bool CanUseStrategyCard(string cardName)
    {
        if (isSpecialStage && currentLayer == 3 && observeCardsDisabled)
        {
            if (ObserveCardNames.Contains(cardName))
            {
                Debug.LogWarning($"[规则] 第3层特殊规则：已翻过褪色牌，无法使用观察类策略牌 [{cardName}]");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 策略牌使用后回调（第3层特殊规则：触发下一张褪色）
    /// </summary>
    public void OnStrategyCardUsed()
    {
        if (isSpecialStage && currentLayer == 3)
        {
            nextCardIsFaded = true;
            Debug.Log("[规则] 第3层特殊规则：使用策略牌后，下一张翻开的牌将为褪色牌");
        }
    }

    // ==================== 翻牌回调 ====================

    /// <summary>
    /// 是否需要抽褪色牌（第3层特殊规则）
    /// </summary>
    public bool ShouldDrawFadedCard()
    {
        if (isSpecialStage && currentLayer == 3 && nextCardIsFaded)
        {
            nextCardIsFaded = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 翻牌后回调（第3层特殊规则：翻到褪色牌后禁用观察类）
    /// </summary>
    public void OnCardDrawn(bool wasFadedCard)
    {
        if (isSpecialStage && currentLayer == 3 && wasFadedCard && !observeCardsDisabled)
        {
            observeCardsDisabled = true;
            Debug.Log("[规则] 第3层特殊规则：已翻到褪色牌，本局无法再使用观察类策略牌");
        }
    }

    // ==================== 修正回调 ====================

    /// <summary>
    /// 修正使用后回调（第1层特殊规则：洗回最后3张已翻牌）
    /// </summary>
    private void OnCorrectionUsed(int remainingCorrections, int cost)
    {
        if (isSpecialStage && currentLayer == 1)
        {
            deck.ReturnLastDrawnToDeck(3);
            Debug.Log("[规则] 第1层特殊规则：已翻开最后3张已洗回牌堆");
        }
    }

    // ==================== 第2层特殊规则：修正窗口提前关闭 ====================

    /// <summary>
    /// 获取修正窗口偏移量（第2层特殊规则：提前3张关闭）
    /// </summary>
    public int GetCorrectionWindowOffset()
    {
        if (isSpecialStage && currentLayer == 2)
            return 3;
        return 0;
    }

    // ==================== 第4层特殊规则：双重目标 ====================

    private void ApplyDoubleTargetRule()
    {
        var (target1, target2) = targetHandManager.GetDoubleTargets();
        if (target1 != null && target2 != null)
        {
            secondTarget = target2;
            Debug.Log($"[规则] 第4层特殊规则：双重目标模式，两个目标都须达成！");

            RevealHintCardForTarget(target1, out revealedCardForTarget1, out revealedCardPosition1, 1);
            RevealHintCardForTarget(target2, out revealedCardForTarget2, out revealedCardPosition2, 2);
        }
    }

    private void RevealHintCardForTarget(TargetHand target, out Card revealedCard, out int position, int targetIndex)
    {
        revealedCard = null;
        position = -1;

        var remaining = deck.GetRemainingDeck();
        if (remaining.Count == 0) return;

        var drawnCards = deck.GetDrawnCards();
        Card bestCard = null;
        int bestPosition = -1;

        // 优先找一张能帮助达成目标的牌
        foreach (var card in remaining.Select((c, i) => new { Card = c, Index = i }))
        {
            var simulated = new List<Card>(drawnCards) { card.Card };
            if (target.checkCondition(simulated))
            {
                bestCard = card.Card;
                bestPosition = card.Index + 1;
                break;
            }
        }

        // 如果找不到关键牌，随机选一张
        if (bestCard == null && remaining.Count > 0)
        {
            System.Random rng = new System.Random();
            int randomIndex = rng.Next(remaining.Count);
            bestCard = remaining[randomIndex];
            bestPosition = randomIndex + 1;
        }

        revealedCard = bestCard;
        position = bestPosition;

        if (revealedCard != null)
        {
            Debug.Log($"[规则] 目标{targetIndex} [{target.handName}] 提示明牌：第{position}张 - {revealedCard}");
        }
    }

    /// <summary>
    /// 检查双重目标是否都已达成（第4层特殊规则）
    /// </summary>
    public bool CheckDoubleTargetsAchieved(List<Card> drawnCards)
    {
        if (!isSpecialStage || currentLayer != 4 || secondTarget == null)
            return false;

        var primaryTarget = targetHandManager.GetCurrentTarget();
        bool primaryAchieved = primaryTarget != null && primaryTarget.checkCondition(drawnCards);
        bool secondaryAchieved = secondTarget.checkCondition(drawnCards);

        return primaryAchieved && secondaryAchieved;
    }

    public TargetHand GetSecondTarget() => secondTarget;

    // ==================== 状态查询 ====================

    public bool IsSpecialStage() => isSpecialStage;
    public int GetCurrentLayer() => currentLayer;

    public bool IsDoubleTargetMode()
    {
        return isSpecialStage && currentLayer == 4 && secondTarget != null;
    }

    public bool IsObserveCardsDisabled() => observeCardsDisabled;

    public (Card card, int position) GetRevealedHintForTarget1()
    {
        return (revealedCardForTarget1, revealedCardPosition1);
    }

    public (Card card, int position) GetRevealedHintForTarget2()
    {
        return (revealedCardForTarget2, revealedCardPosition2);
    }

    /// <summary>
    /// 重置所有规则状态
    /// </summary>
    public void ResetAll()
    {
        nextCardIsFaded = false;
        observeCardsDisabled = false;
        secondTarget = null;
        revealedCardForTarget1 = null;
        revealedCardForTarget2 = null;
        Debug.Log("[规则] 所有规则状态已重置");
    }
}