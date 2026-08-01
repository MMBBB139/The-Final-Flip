using System.Collections.Generic;
using UnityEngine;

public class StrategyCardManager : MonoBehaviour
{
    [SerializeField] private StrategyCardDataSO cardDatabase;
    [SerializeField] private GameConfigSO config;
    [SerializeField] private Deck deck;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private TargetHandManager targetHandManager;
    [SerializeField] private CorrectionManager correctionManager;
    [SerializeField] private RuleManager ruleManager;

    private List<StrategyCard> ownedCards = new List<StrategyCard>();
    private List<StrategyCard> allDefinitions;
    private StrategyCardData[] allData;

    // 改规则类效果状态
    private int nearErrorBonus;
    private int earlyBirdThreshold = int.MaxValue;
    private int earlyBirdBonus;
    private int zeroErrorBonus;
    private bool deathDefy;
    private int errorToleranceBonus;
    private bool keepPreviousGuess;
    private int previousGuessN;

    public Deck Deck => deck;
    public ChipsManager ChipsManager => chipsManager;
    public TargetHandManager TargetHandManager => targetHandManager;
    public CorrectionManager CorrectionManager => correctionManager;
    public RuleManager RuleManager => ruleManager;

    // 交互请求回调
    public System.Action<List<Card>> OnRequestSinkOneFromPeek;
    public System.Action<List<Card>> OnRequestTopOneFromPeek;
    public System.Action<int> OnRequestSinkChoice;
    public System.Action<int> OnRequestTopChoice;
    public System.Action<int> OnRequestDeleteDrawnCards;
    public System.Action<bool> OnRequestCopyDrawnCard;
    public System.Action<bool> OnRequestRankSearch;
    public System.Action<bool> OnRequestSuitSearch;
    public System.Action<List<TargetHand>> OnRequestTargetChoice;

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
            if (data.cardName == cardName) return data;
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

        // 升级后刷新效果
        var data = GetCardData(cardName);
        var newDef = allDefinitions.Find(c => c.cardName == cardName);
        if (newDef != null)
            card.executeEffect = newDef.executeEffect;

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
        if (!card.canUseCondition(this)) return false;

        card.executeEffect(this);
        card.usedThisRound = true;

        // 触发第4层特殊规则
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

    // 改规则效果 getter/setter
    public void SetNearErrorBonus(int b) => nearErrorBonus = b;
    public int GetNearErrorBonus() => nearErrorBonus;
    public void SetEarlyBird(int threshold, int bonus) { earlyBirdThreshold = threshold; earlyBirdBonus = bonus; }
    public int GetEarlyBirdThreshold() => earlyBirdThreshold;
    public int GetEarlyBirdBonus() => earlyBirdBonus;
    public void SetZeroErrorBonus(int b) => zeroErrorBonus = b;
    public int GetZeroErrorBonus() => zeroErrorBonus;
    public void SetDeathDefy(bool v) => deathDefy = v;
    public bool HasDeathDefy() => deathDefy;
    public void ConsumeDeathDefy() => deathDefy = false;
    public void AddErrorToleranceBonus(int b) => errorToleranceBonus += b;
    public int GetErrorToleranceBonus() => errorToleranceBonus;
    public void SetKeepPreviousGuess(bool v) => keepPreviousGuess = v;
    public bool IsKeepPreviousGuess() => keepPreviousGuess;
    public int GetPreviousGuess() => previousGuessN;

    // 交互请求
    public void RequestSinkOneFromPeek(List<Card> cards) => OnRequestSinkOneFromPeek?.Invoke(cards);
    public void RequestTopOneFromPeek(List<Card> cards) => OnRequestTopOneFromPeek?.Invoke(cards);
    public void RequestSinkChoice(int count) => OnRequestSinkChoice?.Invoke(count);
    public void RequestTopChoice(int count) => OnRequestTopChoice?.Invoke(count);
    public void RequestDeleteDrawnCards(int max) => OnRequestDeleteDrawnCards?.Invoke(max);
    public void RequestCopyDrawnCard(bool toTop) => OnRequestCopyDrawnCard?.Invoke(toTop);
    public void RequestRankSearch(bool showAll) => OnRequestRankSearch?.Invoke(showAll);
    public void RequestSuitSearch(bool showAll) => OnRequestSuitSearch?.Invoke(showAll);
    public void RequestTargetChoice(List<TargetHand> options) => OnRequestTargetChoice?.Invoke(options);

    public void ResetAllForNewStage()
    {
        foreach (var c in ownedCards) c.usedThisRound = false;
        nearErrorBonus = 0;
        earlyBirdThreshold = int.MaxValue;
        earlyBirdBonus = 0;
        zeroErrorBonus = 0;
        keepPreviousGuess = false;
    }

    public void ResetAllForNewGame()
    {
        ownedCards.Clear();
        deathDefy = false;
        errorToleranceBonus = 0;
        ResetAllForNewStage();
    }

    private void OnChipsChanged(int newChips)
    {
        if (deathDefy && newChips <= 0)
        {
            chipsManager.SetChips(1);
            deathDefy = false;
            Debug.Log("[绝处逢生] 触发，筹码保留1");
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