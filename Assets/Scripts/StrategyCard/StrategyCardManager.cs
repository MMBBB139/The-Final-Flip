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

    private int nearErrorBonus;
    private int earlyBirdThreshold = int.MaxValue;
    private int earlyBirdBonus;
    private int zeroErrorBonus;
    private bool deathDefy;
    private int errorToleranceBonus;
    private bool allInMode;
    private float perfectMultiplier = 1f;

    private Dictionary<string, int> accumulatedValues = new Dictionary<string, int>();

    private bool previewEnabled;
    private int previewCount;
    private List<Card> previewCards = new List<Card>();

    public Deck Deck => deck;
    public ChipsManager ChipsManager => chipsManager;
    public TargetHandManager TargetHandManager => targetHandManager;
    public CorrectionManager CorrectionManager => correctionManager;
    public RuleManager RuleManager => ruleManager;
    public List<Card> PreviewCards => previewCards;

    public System.Action<string, string> OnEffectTextUpdate;
    public System.Action<string, List<string>, System.Action<int>, System.Action> OnRequestSelection;
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

    public List<StrategyCard> GetDefinitionsByLayer(int layer)
    {
        return allDefinitions.FindAll(d => d.unlockLayer <= layer && !d.isOncePerGame);
    }

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
        UpdateEffectText(cardName);
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

        UpdateEffectText(cardName);
        return true;
    }

    public bool SellCard(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null) return false;
        int sellPrice = card.GetSellPrice();
        ownedCards.Remove(card);

        if (accumulatedValues.ContainsKey(cardName))
            accumulatedValues.Remove(cardName);

        chipsManager.AddChips(sellPrice);
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

    public bool HasCard(string cardName) => ownedCards.Exists(c => c.cardName == cardName);

    public int GetNearErrorBonus() => HasCard("近误差红利") ? nearErrorBonus : 0;
    public int GetEarlyBirdThreshold() => HasCard("早鸟优惠") ? earlyBirdThreshold : int.MaxValue;
    public int GetEarlyBirdBonus() => HasCard("早鸟优惠") ? earlyBirdBonus : 0;
    public int GetZeroErrorBonus() => HasCard("零误差红利") ? zeroErrorBonus : 0;
    public bool HasDeathDefy() => HasCard("不死鸟") ? deathDefy : false;
    public int GetErrorToleranceBonus() => errorToleranceBonus;
    public float GetPerfectMultiplier() => HasCard("完美风暴") ? perfectMultiplier : 1f;
    public bool IsAllInMode() => allInMode;

    public void SetNearErrorBonus(int b) { nearErrorBonus = b; UpdateEffectText("近误差红利"); }
    public void SetEarlyBird(int threshold, int bonus) { earlyBirdThreshold = threshold; earlyBirdBonus = bonus; UpdateEffectText("早鸟优惠"); }
    public void SetZeroErrorBonus(int b) { zeroErrorBonus = b; UpdateEffectText("零误差红利"); }
    public void SetDeathDefy(bool v) => deathDefy = v;
    public void ConsumeDeathDefy() => deathDefy = false;
    public void AddErrorToleranceBonus(int b) { errorToleranceBonus += b; UpdateEffectText("宽容"); }
    public void SetAllInMode(bool v) => allInMode = v;
    public void SetPerfectMultiplier(float m) { perfectMultiplier = m; UpdateEffectText("完美风暴"); }

    public void SetPreview(int count) { previewEnabled = true; previewCount = count; }
    public bool IsPreviewEnabled() => previewEnabled;
    public int GetPreviewCount() => previewCount;
    public void ClearPreview() { previewEnabled = false; previewCards.Clear(); }

    public bool ExecutePreview()
    {
        if (!previewEnabled) return false;
        int count = Mathf.Min(previewCount, deck.GetRemainingCount());
        previewCards = deck.PeekTop(count);
        var simCards = new List<Card>(deck.GetDrawnCards());
        simCards.AddRange(previewCards);
        bool hits = targetHandManager.CheckTarget(simCards);
        if (hits) OnPreviewTriggered?.Invoke();
        return hits;
    }

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
        UpdateEffectText(cardName);
    }

    public void ResetAccumulatedValue(string cardName)
    {
        if (accumulatedValues.ContainsKey(cardName))
            accumulatedValues.Remove(cardName);
        UpdateEffectText(cardName);
    }

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

    public bool HasTenCorrections() => HasCard("十次修正");
    public int GetFateWheelBonus() => HasCard("命运之轮") ? 15 : 0;
    public bool HasApocalypse() => HasCard("天启");
    public int GetApocalypsePreviewCount() => 44;

    public void RequestSelection(string prompt, List<string> options, System.Action<int> callback, System.Action onCancel = null)
    {
        OnRequestSelection?.Invoke(prompt, options, callback, onCancel);
    }

    private void UpdateEffectText(string cardName)
    {
        var card = ownedCards.Find(c => c.cardName == cardName);
        if (card == null) return;
        string text = BuildEffectText(card);
        OnEffectTextUpdate?.Invoke(cardName, text);
    }

    private string BuildEffectText(StrategyCard card)
    {
        int lv = card.currentLevel;

        switch (card.cardName)
        {
            case "偏差大师":
                int devBase = lv >= 2 ? 25 : 15;
                int devPer = lv >= 2 ? 8 : 5;
                int devTotal = devBase + card.accumulatedValue * devPer;
                return $"误差≥3且存活，额外+{devTotal}。永久累计+{devPer}/次";
            case "稳扎稳打":
                int steadyBase = lv >= 2 ? 12 : 8;
                int steadyPer = lv >= 2 ? 6 : 4;
                int steadyTotal = steadyBase + card.accumulatedValue * steadyPer;
                return $"误差≤2，额外+{steadyTotal}。连续触发每局叠加+{steadyPer}，中断重置";
            case "修正艺术家":
                int artBase = lv >= 2 ? 30 : 20;
                int artPer = lv >= 2 ? 8 : 5;
                int artTotal = artBase + card.accumulatedValue * artPer;
                return $"使用修正且误差=0，额外+{artTotal}。永久累计+{artPer}/次";
            case "速攻":
                int speedBase = lv >= 2 ? 30 : 20;
                int speedPer = lv >= 2 ? 8 : 5;
                int speedTotal = speedBase + card.accumulatedValue * speedPer;
                return $"15张内达成且误差≤1，额外+{speedTotal}。永久累计+{speedPer}/次";
            case "完美风暴":
                float mult = lv >= 2 ? 2f : 1.5f;
                return $"误差=0时收入x{mult}";
            case "宽容":
                int tolBonus = lv >= 2 ? 2 : 1;
                return $"本层误差容忍度+{tolBonus}";
            case "零误差红利":
                int zeroB = lv >= 2 ? 35 : 20;
                return $"误差=0额外+{zeroB}筹码";
            case "早鸟优惠":
                int ebBonus = lv >= 2 ? 20 : 12;
                return $"翻牌≤10达成，额外+{ebBonus}筹码";
            case "近误差红利":
                int nearB = lv >= 2 ? 15 : 8;
                return $"误差=1时额外+{nearB}筹码";
            case "宽限":
                int ext = lv >= 2 ? 4 : 2;
                return $"修正窗口延长{ext}张";
            case "再修一次":
                int extra = lv >= 2 ? 2 : 1;
                return $"每局修正次数+{extra}";
            case "修正促销":
                return lv >= 2 ? "修正消耗降为0且使用后额外+3筹码" : "修正消耗降为0";
            default:
                if (lv >= 2 && card.description.Contains("升级："))
                {
                    int idx = card.description.IndexOf("升级：");
                    if (idx >= 0)
                    {
                        string upgradedPart = card.description.Substring(idx + 3);
                        return "已升级：" + upgradedPart;
                    }
                }
                return card.description;
        }
    }

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
        }
    }
}