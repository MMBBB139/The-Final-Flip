using System.Collections.Generic;
using UnityEngine;

public class StrategyCardManager : MonoBehaviour
{
    [SerializeField] private StrategyCardDataSO cardDatabase;
    [SerializeField] private GameConfigSO config;
    [SerializeField] private Deck deck;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private SettlementManager settlementManager;
    [SerializeField] private CorrectionManager correctionManager;
    [SerializeField] private RuleManager ruleManager;

    private List<StrategyCard> ownedCards = new List<StrategyCard>();
    private List<StrategyCard> allDefinitions;
    private StrategyCardData[] allData;

    private float settlementMultiplier = 1f;
    private int errorTolerance = 0;
    private int lossCap = int.MaxValue;
    private bool deathSave;
    private bool allInMode;
    private bool keepPreviousGuess;
    private int previousGuessN;

    public Deck Deck => deck;
    public ChipsManager ChipsManager => chipsManager;
    public TargetHandManager TargetHandManager => targetHandManager;
    public CorrectionManager CorrectionManager => correctionManager;

    void Awake()
    {
        if (cardDatabase != null)
            allData = cardDatabase.cards;
        else
            allData = new StrategyCardData[0];

        allDefinitions = StrategyCardDefinitions.CreateAll(allData);

        if (chipsManager != null)
            chipsManager.OnChipsChanged.AddListener(OnChipsChanged);
        if (correctionManager != null)
            correctionManager.OnBeforeGuessChanged.AddListener(OnBeforeGuessChanged);
    }

    public StrategyCardData GetCardData(string cardName)
    {
        foreach (var data in allData)
        {
            if (data.cardName == cardName)
                return data;
        }
        return null;
    }

    public bool AddCard(string cardName)
    {
        if (ownedCards.Count >= (config != null ? config.maxCarryCards : 4))
            return false;

        var existing = ownedCards.Find(c => c.cardName == cardName);
        if (existing != null)
            return existing.IsUpgradable && UpgradeCard(cardName);

        var def = allDefinitions.Find(c => c.cardName == cardName);
        if (def == null) return false;

        var data = GetCardData(cardName);
        if (data == null) return false;

        var newCard = new StrategyCard(data, def.executeEffect, def.canUseCondition);
        ownedCards.Add(newCard);
        Debug.Log($"获得策略牌: {cardName}");
        return true;
    }

    public bool UpgradeCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null || !card.IsUpgradable) return false;
        int cost = card.GetUpgradeCost();
        if (chipsManager.GetChips() < cost) return false;
        chipsManager.AddChips(-cost);
        card.Upgrade();
        Debug.Log($"{cardName}升级至Lv.{card.currentLevel}");
        return true;
    }

    public bool SellCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null) return false;
        int sellPrice = card.GetSellPrice();
        ownedCards.Remove(card);
        chipsManager.AddChips(sellPrice);
        Debug.Log($"卖出{cardName}，回收{sellPrice}筹码");
        return true;
    }

    public bool UseCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null || !card.IsAvailableThisRound()) return false;
        if (ruleManager != null && !ruleManager.CanUseStrategyCard(cardName)) return false;
        if (!card.canUseCondition(this)) return false;

        card.executeEffect(this);
        card.usedThisRound = true;
        ruleManager?.OnStrategyCardUsed();

        if (card.isOncePerGame)
        {
            card.usedThisGame = true;
            ownedCards.Remove(card);
        }
        return true;
    }

    public int GetCardLevel(string name) => ownedCards.Find(c => c.cardName == name)?.currentLevel ?? 1;

    public List<StrategyCard> GetOwnedCards() => new List<StrategyCard>(ownedCards);
    public List<StrategyCard> GetAllDefinitions() => allDefinitions;

    public void SetSettlementMultiplier(float m) => settlementMultiplier = m;
    public float GetSettlementMultiplier() => settlementMultiplier;
    public void SetErrorTolerance(int t) => errorTolerance = t;
    public bool IsWithinTolerance(int error) => error <= errorTolerance;
    public void SetLossCap(int cap) => lossCap = cap;
    public int GetLossCap() => lossCap;
    public void SetDeathSave(bool v) => deathSave = v;
    public void SetAllInMode(bool v) => allInMode = v;
    public bool IsAllInMode() => allInMode;
    public void SetKeepPreviousGuess(bool v) => keepPreviousGuess = v;
    public bool IsKeepPreviousGuess() => keepPreviousGuess;
    public int GetPreviousGuess() => previousGuessN;

    public int ApplyAllInSettlement(int baseChange)
    {
        if (!allInMode) return baseChange;
        allInMode = false;
        return baseChange > 0 ? baseChange * 3 : -chipsManager.GetChips();
    }

    public int ApplyLossCap(int change) => change < -lossCap ? -lossCap : change;

    public void ResetAllForNewStage()
    {
        foreach (var c in ownedCards) c.usedThisRound = false;
        settlementMultiplier = 1f;
        errorTolerance = 0;
        lossCap = int.MaxValue;
        deathSave = false;
        allInMode = false;
        keepPreviousGuess = false;
    }

    public void ResetAllForNewGame()
    {
        ownedCards.Clear();
        ResetAllForNewStage();
    }

    private void OnChipsChanged(int newChips)
    {
        if (deathSave && newChips <= 0)
        {
            chipsManager.SetChips(1);
            deathSave = false;
            Debug.Log("[免死] 触发，筹码保留1");
        }
    }

    private void OnBeforeGuessChanged(int oldGuess)
    {
        if (keepPreviousGuess)
        {
            previousGuessN = oldGuess;
            Debug.Log($"[保留猜测] 保存修正前猜测: {previousGuessN}");
        }
    }
}