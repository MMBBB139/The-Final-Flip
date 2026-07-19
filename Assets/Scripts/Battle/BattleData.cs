using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BattleData
{
    public List<RuntimeCard> levelDeck;
    public List<RuntimeCard> drawPile;
    public List<RuntimeCard> handCards = new();

    public CardColor? selectedMainColor = null;
    public CardColor bossWeaknessColor;
    public bool firstCardGuaranteed = true;

    private int _currentAttack;
    public int currentAttack
    {
        get => _currentAttack;
        set => _currentAttack = Mathf.Max(0, value);
    }

    private int _playerHp = 20;
    public int playerHp
    {
        get => _playerHp;
        set => _playerHp = Mathf.Clamp(value, 0, maxPlayerHp);
    }

    private int _curseCount;
    public int curseCount
    {
        get => _curseCount;
        set => _curseCount = Mathf.Clamp(value, 0, curseThreshold);
    }

    private int _bossHp = 280;
    public int bossHp
    {
        get => _bossHp;
        set => _bossHp = Mathf.Max(0, value);
    }

    private int _currentTurn = 1;
    public int currentTurn
    {
        get => _currentTurn;
        set => _currentTurn = Mathf.Clamp(value, 1, maxTurns);
    }

    public int maxTurns = 4;
    public int maxPlayerHp = 20;
    public int maxHandSize = 7;
    public int curseThreshold = 3;
    public int maxBossHp = 280;

    public bool isPlayerDead => playerHp <= 0;
    public bool isBossDead => bossHp <= 0;

    // 【Bug修复：这里由 > 改为 >=】4回合为限制，如果第4回合结束（停手）仍未胜利，立即满足最大回合条件，游戏结束
    public bool isMaxTurnsReached => currentTurn >= maxTurns;

    public bool isCurseReady => curseCount >= curseThreshold;

    // 获取主色攻击力加成
    public int GetMainColorBonus(CardColor color)
    {
        if (selectedMainColor == null || color != selectedMainColor.Value)
            return 0;

        return selectedMainColor.Value switch
        {
            CardColor.Blue => 5,
            CardColor.Yellow => 10,
            CardColor.Red => 15,
            _ => 0
        };
    }

    // 获取爆牌安全区大小
    public int GetSafeZoneSize()
    {
        // 蓝主色安全区扩展至前6张
        if (selectedMainColor == CardColor.Blue)
            return 6;
        return 4;
    }
}