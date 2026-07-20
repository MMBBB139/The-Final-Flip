using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 债痕系统管理器：负责债痕累积、清算触发、主动/被动清债
/// </summary>
public class DebtManager
{
    private BattleData data;

    // 基础清算池（10项）
    private static readonly List<DebtOption> baseDebtOptions = new List<DebtOption>
    {
        new DebtOption
        {
            id = 1,
            name = "Double or Nothing",
            description = "Next card's attack is doubled. If bust, all damage this turn becomes 0 (ignores policy).",
            onSelect = (data, card) =>
            {
                // 标记下一张牌攻击力翻倍
                data.nextCardAttackDoubled = true;
                GameEvents.RaiseFloatingText("Next card: Double or Nothing!");
            }
        },
        new DebtOption
        {
            id = 2,
            name = "Forced Gamble",
            description = "Must draw at least 2 more cards this turn. Each card gains +1 attack.",
            onSelect = (data, card) =>
            {
                data.forcedDrawCount += 2;
                data.forcedDrawBonus += 1;
                GameEvents.RaiseFloatingText("Forced to draw 2+ cards! Each +1 ATK.");
            }
        },
        new DebtOption
        {
            id = 3,
            name = "Quit While Ahead",
            description = "Stop immediately and settle. Total attack +3.",
            onSelect = (data, card) =>
            {
                data.currentAttack += 3;
                data.shouldStopImmediately = true;
                GameEvents.RaiseFloatingText("Stop now! ATK +3.");
            }
        },
        new DebtOption
        {
            id = 4,
            name = "Color Lock",
            description = "Cannot draw non-bet color cards this turn (discard if drawn). Bet color cards gain +2 attack.",
            onSelect = (data, card) =>
            {
                data.colorLockActive = true;
                data.colorLockBonus = 2;
                GameEvents.RaiseFloatingText("Color locked! Only bet color cards allowed.");
            }
        },
        new DebtOption
        {
            id = 5,
            name = "Policy Overdraft",
            description = "Gain 4 policies. At the start of next turn, clear all policies.",
            onSelect = (data, card) =>
            {
                data.policy += 4;
                data.policyOverdraftNextTurn = true;
                GameEvents.RaiseFloatingText("Policy overdraft! +4 policies.");
            }
        },
        new DebtOption
        {
            id = 6,
            name = "Loan Shark",
            description = "Lose 2 HP immediately. Gain a random trump card at the start of next floor.",
            onSelect = (data, card) =>
            {
                data.TakeDamage(2);
                data.loanSharkActive = true;
                GameEvents.RaiseFloatingText("Loan shark! -2 HP, trump card next floor.");
            }
        },
        new DebtOption
        {
            id = 7,
            name = "Repentance",
            description = "Lose 2 debt stacks (no liquidation trigger). Last card drawn this turn loses all attack.",
            onSelect = (data, card) =>
            {
                data.debtCount = Mathf.Max(0, data.debtCount - 2);
                if (data.handCards.Count > 0)
                {
                    var lastCard = data.handCards[data.handCards.Count - 1];
                    data.currentAttack -= lastCard.GetAttackValue();
                    lastCard.bonusDamage = -lastCard.rank + 1;
                    data.currentAttack += lastCard.GetAttackValue();
                }
                GameEvents.RaiseFloatingText("Repent! -2 debt, last card weakened.");
            }
        },
        new DebtOption
        {
            id = 8,
            name = "Blood Contract",
            description = "The card just drawn gains +3 attack and permanently becomes Red (keeps HP cost and debt effect).",
            onSelect = (data, card) =>
            {
                if (data.handCards.Count > 0)
                {
                    var lastCard = data.handCards[data.handCards.Count - 1];
                    lastCard.bonusDamage += 3;
                    lastCard.color = CardColor.Red;
                    data.currentAttack += 3;
                    GameEvents.RaiseFloatingText("Blood contract! Card becomes Red, +3 ATK.");
                }
            }
        },
        new DebtOption
        {
            id = 9,
            name = "Peek Deck",
            description = "View all remaining cards in deck. Choose one to discard (won't appear this turn).",
            onSelect = (data, card) =>
            {
                data.peekDeckActive = true;
                GameEvents.RaiseFloatingText("Peek deck! Choose a card to discard.");
            }
        },
        new DebtOption
        {
            id = 10,
            name = "Cut Cards",
            description = "Next turn, you decide the order of the first 3 draws (choose from deck without seeing card faces).",
            onSelect = (data, card) =>
            {
                data.cutCardsActive = true;
                GameEvents.RaiseFloatingText("Cut cards! Next turn choose draw order.");
            }
        }
    };

    // 扩展清算池（深渊刻印解锁3项）
    private static readonly List<DebtOption> extendedDebtOptions = new List<DebtOption>
    {
        new DebtOption
        {
            id = 11,
            name = "All or Nothing",
            description = "This turn, bust rate is doubled, but all drawn cards gain +2 attack.",
            onSelect = (data, card) =>
            {
                data.allOrNothingActive = true;
                GameEvents.RaiseFloatingText("All or nothing! Double bust rate, +2 ATK per card.");
            }
        },
        new DebtOption
        {
            id = 12,
            name = "Rainbow Run",
            description = "This turn, all drawn cards count as bet color (keep original effects and value).",
            onSelect = (data, card) =>
            {
                data.rainbowRunActive = true;
                GameEvents.RaiseFloatingText("Rainbow run! All cards count as bet color.");
            }
        },
        new DebtOption
        {
            id = 13,
            name = "Near Death Experience",
            description = "This turn, no bust risk when drawing cards, but HP drops to 1 after settlement.",
            onSelect = (data, card) =>
            {
                data.nearDeathActive = true;
                GameEvents.RaiseFloatingText("Near death! No bust risk, HP to 1 after settle.");
            }
        }
    };

    public DebtManager(BattleData data)
    {
        this.data = data;
    }

    /// <summary>
    /// 收红牌时增加债痕
    /// </summary>
    public void AddDebt()
    {
        data.debtCount++;
        data.redCardsPlayedThisTurn++;

        if (data.debtCount >= data.debtThreshold)
        {
            // 触发清算
            TriggerLiquidation();
        }
    }

    /// <summary>
    /// 触发清算：从池中随机抽取选项
    /// </summary>
    private void TriggerLiquidation()
    {
        List<DebtOption> availableOptions = new List<DebtOption>(baseDebtOptions);

        // 如果有深渊刻印，加入扩展选项
        if (data.hasAbyssEngraving)
        {
            availableOptions.AddRange(extendedDebtOptions);
        }

        // 随机抽取（亡命徒4选1，其余3选1）
        int optionCount = data.isDesperado ? 4 : 3;
        List<DebtOption> selected = GetRandomOptions(availableOptions, Mathf.Min(optionCount, availableOptions.Count));

        // 暂停游戏流程，等待玩家选择
        data.isWaitingForDebtChoice = true;
        data.currentDebtOptions = selected;
        GameEvents.RaiseDebtLiquidationTriggered(selected);
    }

    /// <summary>
    /// 玩家选择清算选项后的处理
    /// </summary>
    public void OnDebtOptionSelected(DebtOption option)
    {
        if (option == null || !data.isWaitingForDebtChoice) return;

        // 执行选项效果
        option.onSelect?.Invoke(data, null);

        // 重置债痕
        data.debtCount = 0;
        data.isWaitingForDebtChoice = false;
        data.currentDebtOptions = null;

        GameEvents.RaiseFloatingText($"Debt cleared! {option.name} activated.");
        GameEvents.RaiseDataChanged(data);
    }

    /// <summary>
    /// 主动清债：回合开始耗3血-3债痕（每层限1次）
    /// </summary>
    public bool ActiveClearDebt()
    {
        if (data.hasUsedActiveClearDebtThisTurn || data.debtCount <= 0)
            return false;

        data.hasUsedActiveClearDebtThisTurn = true;
        data.TakeDamage(3);
        data.debtCount = Mathf.Max(0, data.debtCount - 3);

        GameEvents.RaiseFloatingText($"Active debt clear! -3 HP, -3 debt.");
        GameEvents.RaiseDataChanged(data);
        return true;
    }

    /// <summary>
    /// 被动清债：回合结束时若本回合收下红牌≤2张，债痕-2
    /// </summary>
    public void PassiveClearDebt()
    {
        if (data.redCardsPlayedThisTurn <= 2)
        {
            int clearAmount = Mathf.Min(2, data.debtCount);
            data.debtCount -= clearAmount;
            if (clearAmount > 0)
            {
                GameEvents.RaiseFloatingText($"Passive debt clear! -{clearAmount} debt.");
            }
        }
    }

    /// <summary>
    /// 从列表中随机抽取指定数量的选项
    /// </summary>
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

/// <summary>
/// 清算选项数据结构
/// </summary>
[System.Serializable]
public class DebtOption
{
    public int id;
    public string name;
    public string description;
    public System.Action<BattleData, RuntimeCard> onSelect;
}