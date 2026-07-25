// StrategyCard.cs
using System;

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

    public StrategyCard(string name, string desc, int price, int maxLevel,
        Action<StrategyCardManager> effect, Func<StrategyCardManager, bool> condition = null)
    {
        cardName = name;
        description = desc;
        this.price = price;
        this.maxLevel = maxLevel;
        this.currentLevel = 1;
        this.upgradePrice = 0;
        this.isOncePerGame = false;
        this.usedThisRound = false;
        this.usedThisGame = false;
        this.executeEffect = effect;
        this.canUseCondition = condition ?? (ctx => true);
    }

    public bool IsUpgradable => maxLevel > 1 && currentLevel < maxLevel;

    public int GetUpgradeCost()
    {
        if (currentLevel >= maxLevel) return -1;
        return upgradePrice;
    }

    public int GetSellPrice()
    {
        int totalInvested = price;
        for (int i = 1; i < currentLevel; i++)
            totalInvested += upgradePrice;
        return totalInvested / 2;
    }

    public bool IsAvailableThisRound()
    {
        if (isOncePerGame && usedThisGame) return false;
        if (usedThisRound) return false;
        return true;
    }

    public bool Upgrade()
    {
        if (currentLevel >= maxLevel) return false;
        currentLevel++;
        return true;
    }
}