using UnityEngine;
using System.Collections.Generic;

// 颜色枚举
public enum CardColor { Blue, Yellow, Red }

// 牌的数据结构
[System.Serializable]
public class CardData
{
    public CardColor color;
    public int minRank;
    public int maxRank;

    public CardData(CardColor color, int minRank, int maxRank)
    {
        this.color = color;
        this.minRank = minRank;
        this.maxRank = maxRank;
    }
}

// 运行时数据
[System.Serializable]
public class RuntimeCard
{
    public CardColor color;
    public int rank;           // 实际点数
    public int bonusDamage = 0;

    public RuntimeCard(CardColor color, int rank)
    {
        this.color = color;
        this.rank = rank;
    }

    // 计算这张牌的实际攻击力
    public int GetAttackValue()
    {
        return rank + bonusDamage;
    }
}