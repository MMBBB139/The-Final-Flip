using System.Collections.Generic;
using UnityEngine;

public class StrategyCardManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private Deck deck;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private SettlementManager settlementManager;
    [SerializeField] private CorrectionManager correctionManager;
    [SerializeField] private RuleManager ruleManager;

    [Header("策略牌限制")]
    [SerializeField] private int maxCarryCards = 4;

    private List<StrategyCard> ownedCards;
    private List<StrategyCard> allDefinitions;

    private float settlementMultiplier = 1.0f;
    private int errorTolerance = 0;
    private int lossCap = int.MaxValue;
    private bool deathSaveActive = false;
    private bool allInMode = false;
    private bool keepPreviousGuess = false;
    private int previousGuessN = 0;

    public Deck Deck => deck;
    public ChipsManager ChipsManager => chipsManager;
    public TargetHandManager TargetHandManager => targetHandManager;
    public SettlementManager SettlementManager => settlementManager;
    public CorrectionManager CorrectionManager => correctionManager;

    void Awake()
    {
        ownedCards = new List<StrategyCard>();
        allDefinitions = StrategyCardDefinitions.CreateAll();

        if (chipsManager != null)
            chipsManager.OnChipsChanged.AddListener(OnChipsChanged);

        if (correctionManager != null)
            correctionManager.OnNewGuess.AddListener(OnBeforeCorrection);
    }

    public bool AddCard(string cardName)
    {
        if (ownedCards.Count >= maxCarryCards)
        {
            Debug.LogWarning($"[策略牌] 已达最大携带数量{maxCarryCards}");
            return false;
        }

        var existing = ownedCards.Find(c => c.cardName == cardName);
        if (existing != null)
        {
            if (existing.IsUpgradable)
                return UpgradeCard(cardName);
            Debug.LogWarning($"[策略牌] 已拥有{cardName}且已满级");
            return false;
        }

        var definition = allDefinitions.Find(c => c.cardName == cardName);
        if (definition == null)
        {
            Debug.LogError($"[策略牌] 未找到定义: {cardName}");
            return false;
        }

        var newCard = new StrategyCard(
            definition.cardName,
            definition.description,
            definition.price,
            definition.maxLevel,
            definition.executeEffect,
            definition.canUseCondition
        )
        {
            upgradePrice = definition.upgradePrice,
            isOncePerGame = definition.isOncePerGame
        };

        ownedCards.Add(newCard);
        Debug.Log($"[策略牌] 获得: {cardName}");
        return true;
    }

    public bool SellCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null)
        {
            Debug.LogWarning($"[策略牌] 未拥有: {cardName}");
            return false;
        }

        int sellPrice = card.GetSellPrice();
        ownedCards.Remove(card);
        chipsManager?.AddChips(sellPrice);
        Debug.Log($"[策略牌] 卖出{cardName}，回收{sellPrice}筹码");
        return true;
    }

    public bool UpgradeCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null)
        {
            Debug.LogWarning($"[策略牌] 未拥有: {cardName}");
            return false;
        }

        if (!card.IsUpgradable)
        {
            Debug.LogWarning($"[策略牌] {cardName}已满级或不可升级");
            return false;
        }

        int cost = card.GetUpgradeCost();
        int currentChips = chipsManager != null ? chipsManager.GetChips() : 0;
        if (currentChips < cost)
        {
            Debug.LogWarning($"[策略牌] 筹码不足，升级需要{cost}");
            return false;
        }

        chipsManager?.AddChips(-cost);
        card.Upgrade();
        Debug.Log($"[策略牌] {cardName}升级到Lv.{card.currentLevel}，消耗{cost}筹码");
        return true;
    }

    public bool UseCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null)
        {
            Debug.LogWarning($"[策略牌] 未拥有: {cardName}");
            return false;
        }

        if (!card.IsAvailableThisRound())
        {
            Debug.LogWarning($"[策略牌] {cardName}本局不可用");
            return false;
        }

        if (ruleManager != null && !ruleManager.CanUseStrategyCard(cardName))
            return false;

        if (!card.canUseCondition(this))
        {
            Debug.LogWarning($"[策略牌] {cardName}当前无法使用");
            return false;
        }

        card.executeEffect(this);
        card.usedThisRound = true;

        if (ruleManager != null)
            ruleManager.OnStrategyCardUsed();

        if (card.isOncePerGame)
        {
            card.usedThisGame = true;
            ownedCards.Remove(card);
            Debug.Log($"[策略牌] {cardName}已使用并消失");
        }

        return true;
    }

    public void ResetAllForNewStage()
    {
        foreach (var card in ownedCards)
            card.usedThisRound = false;

        settlementMultiplier = 1.0f;
        errorTolerance = 0;
        lossCap = int.MaxValue;
        deathSaveActive = false;
        allInMode = false;
        keepPreviousGuess = false;
        previousGuessN = 0;

        Debug.Log("[策略牌] 本局状态已重置");
    }

    public void ResetAllForNewGame()
    {
        ownedCards.Clear();
        ResetAllForNewStage();
        Debug.Log("[策略牌] 全部重置");
    }

    public int GetCardLevel(string cardName)
    {
        return ownedCards.Find(c => c.cardName == cardName)?.currentLevel ?? 1;
    }

    public List<StrategyCard> GetOwnedCards()
    {
        return new List<StrategyCard>(ownedCards);
    }

    public List<StrategyCard> GetAllDefinitions()
    {
        return new List<StrategyCard>(allDefinitions);
    }

    public void SetSettlementMultiplier(float m) => settlementMultiplier = m;
    public float GetSettlementMultiplier() => settlementMultiplier;

    public void SetErrorTolerance(int t) => errorTolerance = t;
    public int GetErrorTolerance() => errorTolerance;

    public void SetLossCap(int cap) => lossCap = cap;
    public int GetLossCap() => lossCap;

    public void SetDeathSave(bool active) => deathSaveActive = active;

    public void SetAllInMode(bool active) => allInMode = active;
    public bool IsAllInMode() => allInMode;

    public void SetKeepPreviousGuess(bool active) => keepPreviousGuess = active;
    public bool IsKeepPreviousGuess() => keepPreviousGuess;
    public int GetPreviousGuess() => previousGuessN;

    public int ApplyAllInSettlement(int baseChange)
    {
        if (!allInMode) return baseChange;
        allInMode = false;
        if (baseChange > 0) return baseChange * 3;
        if (baseChange < 0) return -chipsManager.GetChips();
        return 0;
    }

    public int ApplyLossCap(int chipChange)
    {
        if (chipChange < -lossCap)
        {
            Debug.Log($"[亏损封顶] 损失从{chipChange}限制为{-lossCap}");
            return -lossCap;
        }
        return chipChange;
    }

    public bool IsWithinTolerance(int error)
    {
        return error <= errorTolerance;
    }

    private void OnChipsChanged(int newChips)
    {
        if (deathSaveActive && newChips <= 0)
        {
            chipsManager.SetChips(1);
            deathSaveActive = false;
            Debug.Log("[免死] 触发！筹码保留为1");
        }
    }

    private void OnBeforeCorrection(int newGuess)
    {
        if (keepPreviousGuess)
        {
            previousGuessN = correctionManager.GetLastGuess();
            Debug.Log($"[保留猜测] 保留修正前猜测: {previousGuessN}");
        }
    }
}