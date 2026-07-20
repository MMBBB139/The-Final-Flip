using System.Collections.Generic;
using UnityEngine;

public class DebtManager
{
    private BattleData data;

    private static readonly List<DebtOption> debtOptions = new List<DebtOption>
    {
        new DebtOption
        {
            id = 1,
            name = "Double or Nothing",
            description = "Next card's attack is doubled. If bust, all damage this turn becomes 0.",
            onSelect = (data) =>
            {
                data.nextCardAttackDoubled = true;
                GameEvents.RaiseFloatingText("Next card: Double or Nothing!");
            }
        },
        new DebtOption
        {
            id = 2,
            name = "Quit While Ahead",
            description = "Stop immediately and settle. Total attack +3.",
            onSelect = (data) =>
            {
                data.currentAttack += 3;
                data.shouldStopImmediately = true;
                GameEvents.RaiseFloatingText("Stop now! ATK +3.");
            }
        },
        new DebtOption
        {
            id = 3,
            name = "Color Lock",
            description = "Non-bet color cards have halved attack. Bet color cards gain +2 attack.",
            onSelect = (data) =>
            {
                data.colorLockActive = true;
                data.colorLockBonus = 2;
                GameEvents.RaiseFloatingText("Color locked! Non-bet colors halved, bet color +2.");
            }
        },
        new DebtOption
        {
            id = 4,
            name = "Repentance",
            description = "Lose 2 debt stacks (no liquidation). Last card drawn this turn loses all bonus attack.",
            onSelect = (data) =>
            {
                data.debtCount = Mathf.Max(0, data.debtCount - 2);
                if (data.handCards.Count > 0)
                {
                    var lastCard = data.handCards[data.handCards.Count - 1];
                    data.currentAttack -= lastCard.GetAttackValue();
                    lastCard.bonusDamage = -lastCard.rank + 1;
                    data.currentAttack += Mathf.Max(1, lastCard.GetAttackValue());
                }
                GameEvents.RaiseFloatingText("Repent! -2 debt, last card weakened.");
            }
        },
        new DebtOption
        {
            id = 5,
            name = "Peek Deck",
            description = "View all remaining cards in deck. Choose one to discard.",
            onSelect = (data) =>
            {
                data.peekDeckActive = true;
                GameEvents.RaiseFloatingText("Peek deck! Choose a card to discard.");
            }
        }
    };

    public DebtManager(BattleData data)
    {
        this.data = data;
    }

    public void AddDebt()
    {
        data.debtCount++;

        if (data.debtCount >= data.debtThreshold)
        {
            TriggerLiquidation();
        }
    }

    private void TriggerLiquidation()
    {
        data.TakeDamage(2);
        GameEvents.RaiseFloatingText("Debt collected! -2 HP.");

        List<DebtOption> availableOptions = new List<DebtOption>(debtOptions);
        int optionCount = data.isDesperado ? 4 : 3;
        List<DebtOption> selected = GetRandomOptions(availableOptions, Mathf.Min(optionCount, availableOptions.Count));

        data.isWaitingForDebtChoice = true;
        data.currentDebtOptions = selected;
        GameEvents.RaiseDebtLiquidationTriggered(selected);
    }

    public void OnDebtOptionSelected(DebtOption option)
    {
        if (option == null || !data.isWaitingForDebtChoice) return;

        option.onSelect?.Invoke(data);
        data.debtCount = 0;
        data.isWaitingForDebtChoice = false;
        data.currentDebtOptions = null;

        GameEvents.RaiseFloatingText($"Debt cleared! {option.name} activated.");
        GameEvents.RaiseDataChanged(data);
    }

    private List<DebtOption> GetRandomOptions(List<DebtOption> source, int count)
    {
        List<DebtOption> pool = new List<DebtOption>(source);
        List<DebtOption> result = new List<DebtOption>();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}

[System.Serializable]
public class DebtOption
{
    public int id;
    public string name;
    public string description;
    public System.Action<BattleData> onSelect;
}