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

    // --- 核心数值 ---
    public int currentAttack { get; set; }

    private int _playerHp = 20;
    public int playerHp
    {
        get => _playerHp;
        set => _playerHp = Mathf.Clamp(value, 0, maxPlayerHp);
    }
    public int maxPlayerHp = 20;

    // 保单系统
    private int _policy;
    public int policy
    {
        get => _policy;
        set => _policy = Mathf.Clamp(value, 0, maxPolicy);
    }
    public int maxPolicy = 12;

    // 债痕系统
    private int _debtCount;
    public int debtCount
    {
        get => _debtCount;
        set => _debtCount = Mathf.Clamp(value, 0, debtThreshold);
    }
    public int debtThreshold = 4;
    public bool hasUsedActiveClearDebtThisTurn = false;
    public int redCardsPlayedThisTurn = 0;

    // 债痕清算相关
    public bool isWaitingForDebtChoice = false;
    public List<DebtOption> currentDebtOptions = null;
    public bool isDesperado = false; // 亡命徒标记
    public bool hasAbyssEngraving = false; // 深渊刻印标记

    // 清算效果标记
    public bool nextCardAttackDoubled = false;
    public int forcedDrawCount = 0;
    public int forcedDrawBonus = 0;
    public bool shouldStopImmediately = false;
    public bool colorLockActive = false;
    public int colorLockBonus = 0;
    public bool policyOverdraftNextTurn = false;
    public bool loanSharkActive = false;
    public bool peekDeckActive = false;
    public bool cutCardsActive = false;
    public bool allOrNothingActive = false;
    public bool rainbowRunActive = false;
    public bool nearDeathActive = false;

    // --- 连击系统 ---
    public int comboCount = 0;
    public CardColor? comboColor = null;
    public float bonusYellowMult = 0f;

    public bool hasUsedSkipThisTurn = false;
    public bool nextCardPlusOneAttack = false;
    public bool yellow4SmoothSailing = false;
    public bool blueComboSafetyNet = false;
    public bool blue6SettleReady = false;
    public bool red6DetonateReady = false;

    // --- 关卡信息 ---
    private int _bossHp = 100;
    public int bossHp { get => _bossHp; set => _bossHp = Mathf.Max(0, value); }
    public int maxBossHp = 100;
    public int BossDamage = 5;

    private int _currentTurn = 1;
    public int currentTurn { get => _currentTurn; set => _currentTurn = Mathf.Clamp(value, 1, maxTurns); }
    public int maxTurns = 4;
    public int maxHandSize = 7;

    public bool isPlayerDead => playerHp <= 0;
    public bool isBossDead => bossHp <= 0;
    public bool isMaxTurnsReached => currentTurn >= maxTurns;

    public void TakeDamage(int damage)
    {
        if (damage > 0) playerHp -= damage;
    }

    public void Heal(int amount)
    {
        if (amount > 0) playerHp += amount;
    }

    public int GetMainColorBonus(CardColor color)
    {
        if (selectedMainColor == null || color != selectedMainColor.Value) return 0;
        return selectedMainColor.Value switch
        {
            CardColor.Blue => 1,
            CardColor.Yellow => 2,
            CardColor.Red => 3,
            _ => 0
        };
    }

    public void ResetTurnData()
    {
        comboCount = 0;
        comboColor = null;
        bonusYellowMult = 0f;
        hasUsedSkipThisTurn = false;
        nextCardPlusOneAttack = false;
        yellow4SmoothSailing = false;
        blueComboSafetyNet = false;
        blue6SettleReady = false;
        red6DetonateReady = false;
        redCardsPlayedThisTurn = 0;
        hasUsedActiveClearDebtThisTurn = false;

        // 重置清算效果标记
        nextCardAttackDoubled = false;
        forcedDrawCount = 0;
        forcedDrawBonus = 0;
        shouldStopImmediately = false;
        colorLockActive = false;
        colorLockBonus = 0;
        allOrNothingActive = false;
        rainbowRunActive = false;
        nearDeathActive = false;
    }
}