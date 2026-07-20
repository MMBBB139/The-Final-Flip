using UnityEngine;

public class CardEffectExecutor
{
    private BattleData data;
    private ComboManager comboManager;
    private DebtManager debtManager;

    public CardEffectExecutor(BattleData data, ComboManager comboManager)
    {
        this.data = data;
        this.comboManager = comboManager;
        this.debtManager = new DebtManager(data);
    }

    public void ExecuteTakeCard(RuntimeCard drawn)
    {
        int baseAtk = drawn.rank;
        int betBonus = data.GetMainColorBonus(drawn.color);

        // 颜色锁定：非押注花色减半
        if (data.colorLockActive)
        {
            if (drawn.color == data.selectedMainColor)
            {
                betBonus += data.colorLockBonus;
            }
            else
            {
                baseAtk = Mathf.CeilToInt(baseAtk / 2f);
                betBonus = 0;
            }
        }

        int finalCardAtk = baseAtk + betBonus;

        // 翻倍或归零
        if (data.nextCardAttackDoubled)
        {
            finalCardAtk *= 2;
            data.nextCardAttackDoubled = false;
        }

        // 连击处理
        comboManager.ProcessCombo(drawn, ref finalCardAtk);

        // 累计攻击力
        data.currentAttack += finalCardAtk;
        drawn.bonusDamage = finalCardAtk - drawn.rank;

        // 触发卡色基础效果
        ApplyColorEffect(drawn, finalCardAtk);

        data.policy = Mathf.Min(data.policy, data.maxPolicy);
    }

    private void ApplyColorEffect(RuntimeCard drawn, int finalCardAtk)
    {
        switch (drawn.color)
        {
            case CardColor.Blue:
                data.policy++;
                break;
            case CardColor.Yellow:
                data.bonusYellowMult += 0.1f;
                break;
            case CardColor.Red:
                int hpDmg = Mathf.CeilToInt(finalCardAtk * 0.2f);
                data.TakeDamage(hpDmg);
                debtManager.AddDebt();
                break;
        }
    }

    public DebtManager GetDebtManager()
    {
        return debtManager;
    }
}