using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    // 回合流程
    public static event Action OnTurnStarted;
    public static event Action OnTurnEnded;
    public static event Action<bool> OnGameEnded;

    // 卡牌操作
    public static event Action<RuntimeCard> OnCardDrawn;
    public static event Action<RuntimeCard> OnCardTaken;
    public static event Action<RuntimeCard> OnCardSkipped;
    public static event Action OnBusted;

    // 数据变化
    public static event Action<BattleData> OnDataChanged;
    public static event Action<string> OnFloatingText;

    // 主色选择
    public static event Action<CardColor> OnMainColorSelected;
    public static event Action OnMainColorSelectionStarted;

    // 债痕清算
    public static event Action<List<DebtOption>> OnDebtLiquidationTriggered;
    public static event Action<DebtOption> OnDebtOptionSelected;

    public static void RaiseTurnStarted() => OnTurnStarted?.Invoke();
    public static void RaiseTurnEnded() => OnTurnEnded?.Invoke();
    public static void RaiseGameEnded(bool isWin) => OnGameEnded?.Invoke(isWin);
    public static void RaiseCardDrawn(RuntimeCard card) => OnCardDrawn?.Invoke(card);
    public static void RaiseCardTaken(RuntimeCard card) => OnCardTaken?.Invoke(card);
    public static void RaiseCardSkipped(RuntimeCard card) => OnCardSkipped?.Invoke(card);
    public static void RaiseBusted() => OnBusted?.Invoke();
    public static void RaiseDataChanged(BattleData data) => OnDataChanged?.Invoke(data);
    public static void RaiseFloatingText(string msg) => OnFloatingText?.Invoke(msg);
    public static void RaiseMainColorSelected(CardColor color) => OnMainColorSelected?.Invoke(color);
    public static void RaiseMainColorSelectionStarted() => OnMainColorSelectionStarted?.Invoke();
    public static void RaiseDebtLiquidationTriggered(List<DebtOption> options) => OnDebtLiquidationTriggered?.Invoke(options);
    public static void RaiseDebtOptionSelected(DebtOption option) => OnDebtOptionSelected?.Invoke(option);
}