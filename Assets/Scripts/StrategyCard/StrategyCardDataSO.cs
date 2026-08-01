using UnityEngine;

/// <summary>
/// 单张策略牌的静态数据
/// </summary>
[System.Serializable]
public class StrategyCardData
{
    public string cardName;
    [TextArea] public string description;
    public int price;
    public int maxLevel = 1;
    public int upgradePrice;
    public bool isOncePerGame;
    public string category;
}

/// <summary>
/// 策略牌数据库 - 存放所有策略牌的初始定义
/// </summary>
[CreateAssetMenu(fileName = "StrategyCardDatabase", menuName = "地下赌场/策略牌数据库")]
public class StrategyCardDataSO : ScriptableObject
{
    public StrategyCardData[] cards;
}