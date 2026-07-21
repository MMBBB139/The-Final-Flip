// Card.cs
using UnityEngine;

/// <summary>花色枚举</summary>
public enum Suit
{
    Spades,     // 黑桃
    Hearts,     // 红心
    Clubs,      // 梅花
    Diamonds    // 方块
}

/// <summary>点数枚举，数值即为对应的扑克值（J=11, Q=12, K=13, A=14）</summary>
public enum Rank
{
    Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Jack, Queen, King, Ace
}

/// <summary>一张扑克牌的运行时数据</summary>
[System.Serializable]
public class Card
{
    public Suit suit;
    public Rank rank;

    // 是否被“金卡”或道具修改过（供后续道具系统使用）
    public bool isWild;
    public Suit? customSuit;
    public Rank? customRank;

    public Card(Suit suit, Rank rank)
    {
        this.suit = suit;
        this.rank = rank;
        isWild = false;
        customSuit = null;
        customRank = null;
    }

    /// <summary>获取当前有效点数（考虑自定义修改）</summary>
    public int GetPointValue()
    {
        if (customRank.HasValue)
            return (int)customRank.Value;
        return (int)rank;
    }

    /// <summary>获取当前有效花色</summary>
    public Suit GetEffectiveSuit()
    {
        if (customSuit.HasValue)
            return customSuit.Value;
        return suit;
    }

    /// <summary>判断花色是否为红色（红心/方块）</summary>
    public bool IsRed()
    {
        Suit s = GetEffectiveSuit();
        return s == Suit.Hearts || s == Suit.Diamonds;
    }

    /// <summary>判断花色是否为黑色（黑桃/梅花）</summary>
    public bool IsBlack()
    {
        return !IsRed();
    }

    /// <summary>完全重置所有自定义修改</summary>
    public void ResetCustom()
    {
        isWild = false;
        customSuit = null;
        customRank = null;
    }

    public override string ToString()
    {
        return $"{GetEffectiveSuit()} {GetPointValue()}";
    }
}