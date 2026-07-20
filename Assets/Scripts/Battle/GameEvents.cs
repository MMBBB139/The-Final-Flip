using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnTurnStarted;
    public static event Action OnTurnEnded;
    public static event Action<bool> OnGameEnded;

    public static event Action<RuntimeCard> OnCardDrawn;
    public static event Action<RuntimeCard> OnCardTaken;
    public static event Action<RuntimeCard> OnCardSkipped;
    public static event Action OnBusted;

    public static event Action<BattleData> OnDataChanged;
    public static event Action<string> OnFloatingText;

    public static event Action<CardColor> OnMainColorSelected;
    public static event Action OnMainColorSelectionStarted;

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