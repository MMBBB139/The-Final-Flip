using System;
using UnityEngine;

[Serializable]
public class StrategyCard
{
    public string cardName;
    public string description;
    public int price;
    public int upgradePrice;
    public int maxLevel;
    public int currentLevel;
    public bool isOncePerGame;
    public bool usedThisRound;
    public bool usedThisGame;

    public Action<StrategyCardManager> executeEffect;
    public Func<StrategyCardManager, bool> canUseCondition;

    public StrategyCard(StrategyCardData data, Action<StrategyCardManager> effect, Func<StrategyCardManager, bool> condition = null)
    {
        cardName = data.cardName;
        description = data.description;
        price = data.price;
        maxLevel = data.maxLevel;
        upgradePrice = data.upgradePrice;
        isOncePerGame = data.isOncePerGame;
        currentLevel = 1;
        usedThisRound = false;
        usedThisGame = false;
        executeEffect = effect;
        canUseCondition = condition ?? (ctx => true);
    }

    public bool IsUpgradable => maxLevel > 1 && currentLevel < maxLevel;

    public int GetUpgradeCost() => currentLevel >= maxLevel ? -1 : upgradePrice;

    public int GetSellPrice()
    {
        int total = price;
        for (int i = 1; i < currentLevel; i++) total += upgradePrice;
        return total / 2;
    }

    public bool IsAvailableThisRound()
    {
        if (isOncePerGame && usedThisGame) return false;
        return !usedThisRound;
    }

    public bool Upgrade()
    {
        if (currentLevel >= maxLevel) return false;
        currentLevel++;
        return true;
    }
}