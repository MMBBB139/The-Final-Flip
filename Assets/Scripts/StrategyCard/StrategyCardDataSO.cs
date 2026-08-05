using UnityEngine;

[System.Serializable]
public class StrategyCardData
{
    public string cardName;
    [TextArea] public string description;
    public int price;
    public int maxLevel = 1;
    public bool isOncePerGame;
    public string category;
    public CardType type;
    public int unlockLayer;

    public enum CardType
    {
        主动,
        被动,
        一次性
    }

    public int GetUpgradePrice()
    {
        return Mathf.CeilToInt(price * 1.5f);
    }

    public int GetSellPrice(int currentLevel)
    {
        int total = price;
        if (currentLevel >= 2) total += GetUpgradePrice();
        return total / 2;
    }
}

[CreateAssetMenu(fileName = "StrategyCardDatabase", menuName = "地下赌场/策略牌数据库")]
public class StrategyCardDataSO : ScriptableObject
{
    public StrategyCardData[] cards;
}