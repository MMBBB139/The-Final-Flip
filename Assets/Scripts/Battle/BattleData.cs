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

    private int _shield;
    public int shield
    {
        get => _shield;
        set => _shield = Mathf.Clamp(value, 0, maxShield);
    }
    public int maxShield = 10;

    private int _curseCount;
    public int curseCount
    {
        get => _curseCount;
        set => _curseCount = Mathf.Clamp(value, 0, curseThreshold);
    }

    private int _bossHp = 100;
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
    public int maxBossHp = 100;
    public int BossDamage = 5;

    public bool isPlayerDead => playerHp <= 0;
    public bool isBossDead => bossHp <= 0;

    public bool isMaxTurnsReached => currentTurn >= maxTurns;

    public bool isCurseReady => curseCount >= curseThreshold;

    /// <summary>
    /// 受到伤害，优先消耗护盾
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        int shieldDamage = Mathf.Min(damage, shield);
        shield -= shieldDamage;
        int remainingDamage = damage - shieldDamage;

        if (remainingDamage > 0)
        {
            playerHp -= remainingDamage;
        }
    }

    /// <summary>
    /// 治疗生命值，满血时转为护盾
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;

        int hpMissing = maxPlayerHp - playerHp;
        int hpHeal = Mathf.Min(amount, hpMissing);
        playerHp += hpHeal;

        int remainingHeal = amount - hpHeal;
        if (remainingHeal > 0)
        {
            shield += remainingHeal;
        }
    }

    /// <summary>
    /// 护盾衰减（每回合开始时调用）
    /// </summary>
    public void DecayShield(int amount = 2)
    {
        shield = Mathf.Max(0, shield - amount);
    }

    public int GetMainColorBonus(CardColor color)
    {
        if (selectedMainColor == null || color != selectedMainColor.Value)
            return 0;

        return selectedMainColor.Value switch
        {
            CardColor.Blue => 1,
            CardColor.Yellow => 2,
            CardColor.Red => 3,
            _ => 0
        };
    }

    public int GetSafeZoneSize()
    {
        if (selectedMainColor == CardColor.Blue)
            return 6;
        return 4;
    }
}