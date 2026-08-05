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

    // 改规则被动效果状态
    private int nearErrorBonus;
    private int earlyBirdThreshold = int.MaxValue;
    private int earlyBirdBonus;
    private int zeroErrorBonus;
    private bool deathDefy;
    private int errorToleranceBonus;
    private bool allInMode;
    private float perfectMultiplier = 1f;
    private int freeCorrections;

    // 成长型牌的累计数据（cardName -> 累计值）
    private Dictionary<string, int> accumulatedValues = new Dictionary<string, int>();

    // 预览相关
    private bool previewEnabled;
    private int previewCount;
    private List<Card> previewCards = new List<Card>();

    public Deck Deck => deck;
    public ChipsManager ChipsManager => chipsManager;
    public TargetHandManager TargetHandManager => targetHandManager;
    public CorrectionManager CorrectionManager => correctionManager;
    public RuleManager RuleManager => ruleManager;
    public List<Card> PreviewCards => previewCards;

    public System.Action<List<Card>> OnRequestSinkOneFromPeek;
    public System.Action<List<Card>> OnRequestTopOneFromPeek;
    public System.Action<int> OnRequestSinkChoice;
    public System.Action<int> OnRequestTopChoice;
    public System.Action<int> OnRequestDeleteDrawnCards;
    public System.Action<bool> OnRequestCopyDrawnCard;
    public System.Action<bool> OnRequestRankSearch;
    public System.Action<bool> OnRequestSuitSearch;
    public System.Action<List<TargetHand>> OnRequestTargetChoice;
    public System.Action<int, System.Action<int>> OnRequestFastForwardSelect;
    public System.Action OnPreviewTriggered;

    void Awake()
    {
        if (cardDatabase != null)
            allData = cardDatabase.cards;
        else
            allData = new StrategyCardData[0];

        allDefinitions = StrategyCardDefinitions.CreateAll(allData);

        if (chipsManager != null)
            chipsManager.OnChipsChanged.AddListener(OnChipsChanged);
    }

    public StrategyCardData GetCardData(string cardName)
    {
        foreach (var data in allData)
            if (data.cardName == cardName) return data;
        return null;
    }

    /// <summary>
    /// 获取指定层级解锁的所有策略牌定义
    /// </summary>
    public List<StrategyCard> GetDefinitionsByLayer(int layer)
    {
        return allDefinitions.FindAll(d => d.unlockLayer <= layer && !d.isOncePerGame);
    }

    /// <summary>
    /// 获取指定层级新解锁的牌（之前层级没有的）
    /// </summary>
    public List<StrategyCard> GetNewDefinitionsForLayer(int layer)
    {
        return allDefinitions.FindAll(d => d.unlockLayer == layer);
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
        if (accumulatedValues.ContainsKey(cardName))
            newCard.accumulatedValue = accumulatedValues[cardName];

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

        // 成长型牌卖掉后清零累计
        if (accumulatedValues.ContainsKey(cardName))
            accumulatedValues.Remove(cardName);

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

        if (card.type == StrategyCardData.CardType.主动 || card.type == StrategyCardData.CardType.一次性)
        {
            ruleManager?.OnStrategyCardUsed();
        }

        if (card.isOncePerGame || card.type == StrategyCardData.CardType.一次性)
        {
            card.usedThisGame = true;
            ownedCards.Remove(card);
            if (accumulatedValues.ContainsKey(cardName))
                accumulatedValues.Remove(cardName);
        }
        return true;
    }

    public int GetCardLevel(string name) => ownedCards.Find(c => c.cardName == name)?.currentLevel ?? 1;

    public List<StrategyCard> GetOwnedCards() => new List<StrategyCard>(ownedCards);

    /// <summary>
    /// 检查是否拥有某张牌
    /// </summary>
    public bool HasCard(string cardName) => ownedCards.Exists(c => c.cardName == cardName);

    // 被动效果查询
    public int GetNearErrorBonus() => HasCard("近误差红利") ? nearErrorBonus : 0;
    public int GetEarlyBirdThreshold() => HasCard("早鸟优惠") ? earlyBirdThreshold : int.MaxValue;
    public int GetEarlyBirdBonus() => HasCard("早鸟优惠") ? earlyBirdBonus : 0;
    public int GetZeroErrorBonus() => HasCard("零误差红利") ? zeroErrorBonus : 0;
    public bool HasDeathDefy() => HasCard("不死鸟") ? deathDefy : false;
    public int GetErrorToleranceBonus() => errorToleranceBonus;
    public float GetPerfectMultiplier() => HasCard("完美风暴") ? perfectMultiplier : 1f;
    public bool IsAllInMode() => allInMode;

    // 被动效果设置
    public void SetNearErrorBonus(int b) => nearErrorBonus = b;
    public void SetEarlyBird(int threshold, int bonus) { earlyBirdThreshold = threshold; earlyBirdBonus = bonus; }
    public void SetZeroErrorBonus(int b) => zeroErrorBonus = b;
    public void SetDeathDefy(bool v) => deathDefy = v;
    public void ConsumeDeathDefy() => deathDefy = false;
    public void AddErrorToleranceBonus(int b) => errorToleranceBonus += b;
    public void SetAllInMode(bool v) => allInMode = v;
    public void SetPerfectMultiplier(float m) => perfectMultiplier = m;
    public void SetFreeCorrections(int count) => freeCorrections = count;

    // 预览相关
    public void SetPreview(int count) { previewEnabled = true; previewCount = count; }
    public bool IsPreviewEnabled() => previewEnabled;
    public int GetPreviewCount() => previewCount;
    public void ClearPreview() { previewEnabled = false; previewCards.Clear(); }

    /// <summary>
    /// 执行预览：翻顶部N张牌检查是否直接达成
    /// </summary>
    public bool ExecutePreview()
    {
        if (!previewEnabled) return false;
        int count = Mathf.Min(previewCount, deck.GetRemainingCount());
        previewCards = deck.PeekTop(count);
        var simCards = new List<Card>(deck.GetDrawnCards());
        simCards.AddRange(previewCards);
        bool hits = targetHandManager.CheckTarget(simCards);
        if (hits)
            OnPreviewTriggered?.Invoke();
        return hits;
    }

    // 成长型牌的累计值管理
    public int GetAccumulatedValue(string cardName)
    {
        if (accumulatedValues.ContainsKey(cardName))
            return accumulatedValues[cardName];
        return 0;
    }

    public void AddAccumulatedValue(string cardName, int amount)
    {
        if (!accumulatedValues.ContainsKey(cardName))
            accumulatedValues[cardName] = 0;
        accumulatedValues[cardName] += amount;
    }

    public void ResetAccumulatedValue(string cardName)
    {
        if (accumulatedValues.ContainsKey(cardName))
            accumulatedValues.Remove(cardName);
    }

    // 成长型效果查询
    public int GetDeviationMasterBonus()
    {
        var card = ownedCards.Find(c => c.cardName == "偏差大师");
        if (card == null) return 0;
        int perTrigger = card.currentLevel >= 2 ? 8 : 5;
        return card.accumulatedValue * perTrigger;
    }

    public int GetSteadyBonus()
    {
        var card = ownedCards.Find(c => c.cardName == "稳扎稳打");
        if (card == null) return 0;
        int perTrigger = card.currentLevel >= 2 ? 6 : 4;
        return card.accumulatedValue * perTrigger;
    }

    public int GetCorrectionArtistBonus()
    {
        var card = ownedCards.Find(c => c.cardName == "修正艺术家");
        if (card == null) return 0;
        int perTrigger = card.currentLevel >= 2 ? 8 : 5;
        return card.accumulatedValue * perTrigger;
    }

    public int GetSpeedRunBonus()
    {
        var card = ownedCards.Find(c => c.cardName == "速攻");
        if (card == null) return 0;
        int perTrigger = card.currentLevel >= 2 ? 8 : 5;
        return card.accumulatedValue * perTrigger;
    }

    // 十次修正
    public bool HasTenCorrections() => HasCard("十次修正");

    // 命运之轮
    public int GetFateWheelBonus() => HasCard("命运之轮") ? 15 : 0;

    // 天启
    public bool HasApocalypse() => HasCard("天启");
    public int GetApocalypsePreviewCount() => 44;

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
        foreach (var c in ownedCards)
        {
            if (c.type != StrategyCardData.CardType.被动)
                c.usedThisRound = false;
        }
        nearErrorBonus = 0;
        earlyBirdThreshold = int.MaxValue;
        earlyBirdBonus = 0;
        zeroErrorBonus = 0;
        allInMode = false;
        freeCorrections = 0;
        previewEnabled = false;
        previewCards.Clear();
    }

    public void ResetAllForNewGame()
    {
        ownedCards.Clear();
        deathDefy = false;
        errorToleranceBonus = 0;
        perfectMultiplier = 1f;
        accumulatedValues.Clear();
        ResetAllForNewStage();
    }

    private void OnChipsChanged(int newChips)
    {
        if (deathDefy && newChips <= 0)
        {
            chipsManager.SetChips(1);
            deathDefy = false;
            ownedCards.RemoveAll(c => c.cardName == "不死鸟");
            Debug.Log("[不死鸟] 触发，筹码保留1");
        }
    }
}