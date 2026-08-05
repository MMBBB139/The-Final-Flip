using System;
using UnityEngine;

[Serializable]
public class StrategyCard
{
    public string cardName;
    public string description;
    public int price;
    public int maxLevel;
    public int currentLevel;
    public bool isOncePerGame;
    public StrategyCardData.CardType type;
    public int unlockLayer;
    public bool usedThisRound;
    public bool usedThisGame;

    public Action<StrategyCardManager> executeEffect;
    public Func<StrategyCardManager, bool> canUseCondition;

    // 成长型牌的累计值
    public int accumulatedValue;

    public StrategyCard(StrategyCardData data, Action<StrategyCardManager> effect, Func<StrategyCardManager, bool> condition = null)
    {
        cardName = data.cardName;
        description = data.description;
        price = data.price;
        maxLevel = data.maxLevel;
        isOncePerGame = data.isOncePerGame;
        type = data.type;
        unlockLayer = data.unlockLayer;
        currentLevel = 1;
        usedThisRound = false;
        usedThisGame = false;
        accumulatedValue = 0;
        executeEffect = effect;
        canUseCondition = condition ?? (ctx => true);
    }

    public bool IsUpgradable => maxLevel > 1 && currentLevel < maxLevel;

    public int GetUpgradeCost()
    {
        if (!IsUpgradable) return -1;
        return Mathf.CeilToInt(price * 1.5f);
    }

    public int GetSellPrice()
    {
        int total = price;
        if (currentLevel >= 2) total += Mathf.CeilToInt(price * 1.5f);
        return total / 2;
    }

    public bool IsAvailableThisRound()
    {
        if (isOncePerGame && usedThisGame) return false;
        if (type == StrategyCardData.CardType.主动 || type == StrategyCardData.CardType.一次性)
            return !usedThisRound;
        return true;
    }

    public bool Upgrade()
    {
        if (currentLevel >= maxLevel) return false;
        currentLevel++;
        return true;
    }

    public void ResetAccumulated()
    {
        accumulatedValue = 0;
    }
}