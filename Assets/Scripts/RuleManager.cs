using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 规则管理器 - 管理全局规则和特殊规则
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

    // 第3层全局规则状态
    private bool nextCardIsFaded;

    // 第3层特殊规则状态
    private bool observeCardsDisabled;

    // 第4层全局规则状态
    private bool settlementPenaltyDoubled;

    // 第4层特殊规则状态
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
            correctionManager.OnNewGuess.AddListener(OnNewGuess);
        }
    }

    void OnDestroy()
    {
        if (correctionManager != null)
        {
            correctionManager.OnCorrectionUsed.RemoveListener(OnCorrectionUsed);
            correctionManager.OnNewGuess.RemoveListener(OnNewGuess);
        }
    }

    public void InitializeForStage(int layer, int stage)
    {
        currentLayer = layer;
        isSpecialStage = (stage == 3);

        nextCardIsFaded = false;
        observeCardsDisabled = false;
        settlementPenaltyDoubled = false;
        secondTarget = null;
        revealedCardForTarget1 = null;
        revealedCardForTarget2 = null;

        Debug.Log($"[规则] 第{layer}层 第{stage}关 - 全局规则: {GetGlobalRuleDesc(layer)}, 特殊规则: {(isSpecialStage ? GetSpecialRuleDesc(layer) : "无")}");

        if (layer >= 2)
        {
            deck.RandomRemoveConsecutive(3);
            Debug.Log("[规则] 第2层全局规则：已随机移除连续3张牌");
        }

        if (isSpecialStage && layer == 4)
        {
            ApplyDoubleTargetRule();
        }
    }

    private string GetGlobalRuleDesc(int layer)
    {
        switch (layer)
        {
            case 1: return "无干扰，标准52张牌";
            case 2: return "开局前随机移除连续3张牌";
            case 3: return "使用策略牌后下一张为褪色牌";
            case 4: return "修正后揭示一张牌，关键牌惩罚翻倍";
            default: return "未知";
        }
    }

    private string GetSpecialRuleDesc(int layer)
    {
        switch (layer)
        {
            case 1: return "使用修正后，已翻开最后3张洗回牌堆";
            case 2: return "修正窗口提前关闭（翻到N-3张时关闭）";
            case 3: return "翻到褪色牌后禁用观察类策略牌";
            case 4: return "双重目标，两个都须达成，各揭示一张明牌";
            default: return "未知";
        }
    }

    // ==================== 策略牌使用回调 ====================

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

    public void OnStrategyCardUsed()
    {
        if (currentLayer >= 3)
        {
            nextCardIsFaded = true;
            Debug.Log("[规则] 第3层全局规则：下一张翻开的牌将为褪色牌（隐藏花色点数）");
        }
    }

    // ==================== 翻牌回调 ====================

    public bool ShouldDrawFadedCard()
    {
        if (nextCardIsFaded)
        {
            nextCardIsFaded = false;
            return true;
        }
        return false;
    }

    public void OnCardDrawn(bool wasFadedCard)
    {
        if (isSpecialStage && currentLayer == 3 && wasFadedCard && !observeCardsDisabled)
        {
            observeCardsDisabled = true;
            Debug.Log("[规则] 第3层特殊规则：已翻到褪色牌，本局无法再使用观察类策略牌");
        }
    }

    // ==================== 修正回调 ====================

    private void OnCorrectionUsed(int remainingCorrections, int cost)
    {
        if (isSpecialStage && currentLayer == 1)
        {
            deck.ReturnLastDrawnToDeck(3);
            Debug.Log("[规则] 第1层特殊规则：已翻开最后3张已洗回牌堆");
        }
    }

    private void OnNewGuess(int newGuess)
    {
        if (currentLayer >= 4)
        {
            RevealCardAndCheckPenalty();
        }
    }

    private void RevealCardAndCheckPenalty()
    {
        var remaining = deck.GetRemainingDeck();
        if (remaining.Count == 0) return;

        System.Random rng = new System.Random();
        int randomIndex = rng.Next(remaining.Count);
        Card revealedCard = remaining[randomIndex];

        Debug.Log($"[规则] 第4层全局规则：揭示第{randomIndex + 1}张未翻牌 - {revealedCard}");

        if (IsCardKeyToTarget(revealedCard))
        {
            settlementPenaltyDoubled = true;
            Debug.Log($"[规则] 揭示的牌 {revealedCard} 是目标达成的关键牌！本次结算惩罚将翻倍！");
        }
    }

    private bool IsCardKeyToTarget(Card card)
    {
        var drawnCards = deck.GetDrawnCards();
        var currentTarget = targetHandManager.GetCurrentTarget();
        if (currentTarget == null) return false;

        bool currentlyAchieved = currentTarget.checkCondition(drawnCards);
        if (currentlyAchieved) return false;

        var simulatedCards = new List<Card>(drawnCards) { card };
        bool wouldAchieve = currentTarget.checkCondition(simulatedCards);

        if (wouldAchieve)
        {
            var remaining = deck.GetRemainingDeck();
            var otherCards = remaining.Where(c => c != card).Take(5);
            foreach (var otherCard in otherCards)
            {
                var altSimulation = new List<Card>(drawnCards) { otherCard };
                if (currentTarget.checkCondition(altSimulation))
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    // ==================== 第2层特殊规则 ====================

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

    public bool CheckDoubleTargetsAchieved(List<Card> drawnCards)
    {
        if (!isSpecialStage || currentLayer != 4 || secondTarget == null)
            return false;

        var primaryTarget = targetHandManager.GetCurrentTarget();
        bool primaryAchieved = primaryTarget != null && primaryTarget.checkCondition(drawnCards);
        bool secondaryAchieved = secondTarget.checkCondition(drawnCards);

        return primaryAchieved && secondaryAchieved;
    }

    public TargetHand GetSecondTarget()
    {
        return secondTarget;
    }

    // ==================== 结算相关 ====================

    public bool IsSettlementPenaltyDoubled()
    {
        if (currentLayer >= 4 && settlementPenaltyDoubled)
        {
            settlementPenaltyDoubled = false;
            return true;
        }
        return false;
    }

    public int ApplyPenaltyDouble(int chipChange)
    {
        if (chipChange < 0 && IsSettlementPenaltyDoubled())
        {
            int doubled = chipChange * 2;
            Debug.Log($"[规则] 第4层全局规则：关键牌惩罚翻倍！{chipChange} → {doubled}");
            return doubled;
        }
        return chipChange;
    }

    // ==================== 状态查询 ====================

    public bool IsDoubleTargetMode()
    {
        return isSpecialStage && currentLayer == 4 && secondTarget != null;
    }

    public bool IsObserveCardsDisabled()
    {
        return observeCardsDisabled;
    }

    public (Card card, int position) GetRevealedHintForTarget1()
    {
        return (revealedCardForTarget1, revealedCardPosition1);
    }

    public (Card card, int position) GetRevealedHintForTarget2()
    {
        return (revealedCardForTarget2, revealedCardPosition2);
    }

    public void ResetAll()
    {
        nextCardIsFaded = false;
        observeCardsDisabled = false;
        settlementPenaltyDoubled = false;
        secondTarget = null;
        revealedCardForTarget1 = null;
        revealedCardForTarget2 = null;
        Debug.Log("[规则] 所有规则状态已重置");
    }
}