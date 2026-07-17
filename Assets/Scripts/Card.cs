using UnityEngine;
using System.Collections.Generic;

// 基础枚举
public enum CardSuit { Spades, Hearts, Clubs, Diamonds }
public enum CardColor { Blue, Yellow, Red }

// 1. 静态数据：代表标准的52张扑克牌之一
[CreateAssetMenu(fileName = "NewCard", menuName = "PokerRoguelike/CardData")]
public class CardData : ScriptableObject
{
    public CardSuit suit;
    
    [Range(2, 14)]
    [Tooltip("J=11, Q=12, K=13, A=14")]
    public int rank; 
}

// 2. 运行时数据：代表玩家当前牌组里的一张具体的牌
[System.Serializable]
public class RuntimeCard
{
    public CardData baseData;    // 引用静态数据
    public CardColor color;      // 运行时颜色（可被染色）
    public int bonusDamage = 0;  // 升级带来的额外伤害

    public RuntimeCard(CardData data, CardColor color)
    {
        this.baseData = data;
        this.color = color;
    }

    // 计算这张牌的实际攻击力
    public int GetAttackValue()
    {
        int baseAtk = 0;
        switch (color)
        {
            case CardColor.Blue: baseAtk = 0; break;
            case CardColor.Yellow: baseAtk = 5; break;
            case CardColor.Red: baseAtk = 10; break;
        }
        return baseData.rank + baseAtk + bonusDamage;
    }
}