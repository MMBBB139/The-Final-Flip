using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BattleData
{
    public List<RuntimeCard> levelDeck;
    public List<RuntimeCard> drawPile;
    public List<RuntimeCard> handCards = new();

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

    private int _bossHp = 180;
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

    public int maxTurns = 3;
    public int maxPlayerHp = 20;
    public int maxHandSize = 7;
    public int curseThreshold = 4;
    public int maxBossHp = 180;

    public bool isPlayerDead => playerHp <= 0;
    public bool isBossDead => bossHp <= 0;
    public bool isMaxTurnsReached => currentTurn >= maxTurns;
    public bool isCurseReady => curseCount >= curseThreshold;
}