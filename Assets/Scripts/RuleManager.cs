using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuleManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    [SerializeField] private Deck deck;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private StrategyCardManager strategyCardManager;
    [SerializeField] private CorrectionManager correctionManager;

    private int currentLayer;
    private bool isSpecialStage;
    private bool nextCardIsFaded;
    private bool observeCardsDisabled;
    private TargetHand secondTarget;
    private Card revealedCardForTarget1, revealedCardForTarget2;
    private int revealedPos1, revealedPos2;

    void Awake()
    {
        if (correctionManager != null)
            correctionManager.OnCorrectionUsed.AddListener(OnCorrectionUsed);
    }

    void OnDestroy()
    {
        if (correctionManager != null)
            correctionManager.OnCorrectionUsed.RemoveListener(OnCorrectionUsed);
    }

    public void InitializeForStage(int layer, int stage)
    {
        currentLayer = layer;
        isSpecialStage = (stage == 3);
        nextCardIsFaded = false;
        observeCardsDisabled = false;
        secondTarget = null;

        if (isSpecialStage && layer == 4)
            ApplyDoubleTargetRule();
    }

    public bool CanUseStrategyCard(string cardName)
    {
        if (isSpecialStage && currentLayer == 3 && observeCardsDisabled)
        {
            var data = strategyCardManager.GetCardData(cardName);
            if (data != null && data.isObservable)
            {
                Debug.LogWarning($"[规则] 观察类策略卡不可用：{cardName}");
                return false;
            }
        }
        return true;
    }

    public void OnStrategyCardUsed()
    {
        if (isSpecialStage && currentLayer == 3)
        {
            nextCardIsFaded = true;
            Debug.Log("[规则] 下一张牌将为褪色牌");
        }
    }

    public bool ShouldDrawFadedCard()
    {
        if (isSpecialStage && currentLayer == 3 && nextCardIsFaded)
        {
            nextCardIsFaded = false;
            return true;
        }
        return false;
    }

    public void OnCardDrawn(bool wasFaded)
    {
        if (isSpecialStage && currentLayer == 3 && wasFaded && !observeCardsDisabled)
        {
            observeCardsDisabled = true;
            Debug.Log("[规则] 已翻褪色牌，观察类策略卡禁用");
        }
    }

    private void OnCorrectionUsed(int remaining, int cost)
    {
        if (isSpecialStage && currentLayer == 1)
        {
            int returnCount = config != null ? config.layer1ReturnCardCount : 3;
            deck.ReturnLastDrawnToDeck(returnCount);
            Debug.Log($"[规则] 已洗回最后{returnCount}张已翻牌");
        }
    }

    public int GetCorrectionWindowOffset()
    {
        if (isSpecialStage && currentLayer == 2)
            return config != null ? config.layer2CorrectionWindowOffset : 3;
        return 0;
    }

    private void ApplyDoubleTargetRule()
    {
        var (t1, t2) = targetHandManager.GetDoubleTargets();
        if (t1 != null && t2 != null)
        {
            secondTarget = t2;
            RevealHint(t1, out revealedCardForTarget1, out revealedPos1);
            RevealHint(t2, out revealedCardForTarget2, out revealedPos2);
        }
    }

    private void RevealHint(TargetHand target, out Card card, out int pos)
    {
        card = null; pos = -1;
        var remaining = deck.GetRemainingDeck();
        if (remaining.Count == 0) return;
        var drawn = deck.GetDrawnCards();

        foreach (var c in remaining.Select((c, i) => new { Card = c, Index = i }))
        {
            var sim = new List<Card>(drawn) { c.Card };
            if (target.checkCondition(sim)) { card = c.Card; pos = c.Index + 1; break; }
        }
        if (card == null && remaining.Count > 0)
        {
            int rnd = new System.Random().Next(remaining.Count);
            card = remaining[rnd];
            pos = rnd + 1;
        }
        if (card != null) Debug.Log($"[规则] 目标[{target.handName}]提示牌：第{pos}张 {card}");
    }

    public bool CheckDoubleTargetsAchieved(List<Card> drawn)
    {
        if (!isSpecialStage || currentLayer != 4 || secondTarget == null) return false;
        var primary = targetHandManager.GetCurrentTarget();
        return primary != null && primary.checkCondition(drawn) && secondTarget.checkCondition(drawn);
    }

    public TargetHand GetSecondTarget() => secondTarget;
    public bool IsDoubleTargetMode() => isSpecialStage && currentLayer == 4 && secondTarget != null;
    public (Card, int) GetRevealed1() => (revealedCardForTarget1, revealedPos1);
    public (Card, int) GetRevealed2() => (revealedCardForTarget2, revealedPos2);

    public void ResetAll()
    {
        nextCardIsFaded = false;
        observeCardsDisabled = false;
        secondTarget = null;
    }
}