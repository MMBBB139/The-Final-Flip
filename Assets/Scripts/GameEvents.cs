// GameEvents.cs
using System;
using UnityEngine;

/// <summary>全局静态事件总线，所有模块通过它通信，无需直接引用</summary>
public static class GameEvents
{
    // ---------- 全局状态变化 ----------
    /// <summary>信用值变化(int newCredit)</summary>
    public static event Action<int> OnCreditChanged;
    /// <summary>金币变化(int newGold)</summary>
    public static event Action<int> OnGoldChanged;
    /// <summary>章节变化(int newChapter)</summary>
    public static event Action<int> OnChapterChanged;
    /// <summary>游戏结束(bool isWin)</summary>
    public static event Action<bool> OnGameOver;

    // ---------- 关卡流程事件 ----------
    /// <summary>关卡开始(LevelData level)</summary>
    public static event Action<LevelData> OnLevelStarted;
    /// <summary>玩家下注(int betN)</summary>
    public static event Action<int> OnBetPlaced;
    /// <summary>一张牌被翻开(Card card)</summary>
    public static event Action<Card> OnCardDrawn;
    /// <summary>牌型目标达成(int achieveDrawCount)</summary>
    public static event Action<int> OnTargetMatched;
    /// <summary>关卡结算(BetResult result)</summary>
    public static event Action<BetResult> OnLevelSettled;
    /// <summary>关卡结束（无论成败）</summary>
    public static event Action OnLevelFinished;

    // ---------- 道具相关 ----------
    /// <summary>道具使用(ItemData item)</summary>
    public static event Action<ItemData> OnItemUsed;
    /// <summary>道具获得(ItemData item)</summary>
    public static event Action<ItemData> OnItemAcquired;

    // ---------- UI/通用 ----------
    /// <summary>飘字提示(string message)</summary>
    public static event Action<string> OnFloatingTextRequested;

    // ---------- 引发方法 ----------
    public static void RaiseCreditChanged(int newCredit) => OnCreditChanged?.Invoke(newCredit);
    public static void RaiseGoldChanged(int newGold) => OnGoldChanged?.Invoke(newGold);
    public static void RaiseChapterChanged(int chapter) => OnChapterChanged?.Invoke(chapter);
    public static void RaiseGameOver(bool win) => OnGameOver?.Invoke(win);
    public static void RaiseLevelStarted(LevelData level) => OnLevelStarted?.Invoke(level);
    public static void RaiseBetPlaced(int n) => OnBetPlaced?.Invoke(n);
    public static void RaiseCardDrawn(Card card) => OnCardDrawn?.Invoke(card);
    public static void RaiseTargetMatched(int drawCount) => OnTargetMatched?.Invoke(drawCount);
    public static void RaiseLevelSettled(BetResult result) => OnLevelSettled?.Invoke(result);
    public static void RaiseLevelFinished() => OnLevelFinished?.Invoke();
    public static void RaiseItemUsed(ItemData item) => OnItemUsed?.Invoke(item);
    public static void RaiseItemAcquired(ItemData item) => OnItemAcquired?.Invoke(item);
    public static void RaiseFloatingText(string msg) => OnFloatingTextRequested?.Invoke(msg);
}