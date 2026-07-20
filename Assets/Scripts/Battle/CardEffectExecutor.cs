using UnityEngine;

/// <summary>
/// 卡牌效果执行器：负责收牌时的攻击力计算、卡色效果触发、债痕更新
/// </summary>
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

    /// <summary>
    /// 执行收牌效果：计算攻击力、触发连击、应用卡色效果
    /// </summary>
    public void ExecuteTakeCard(RuntimeCard drawn)
    {
        // 计算底数 + 押注加成
        int baseAtk = drawn.rank;
        int betBonus = data.GetMainColorBonus(drawn.color);

        // 彩虹跑：所有牌视为押注花色
        if (data.rainbowRunActive)
        {
            betBonus = data.GetMainColorBonus(data.selectedMainColor ?? CardColor.Blue);
        }

        // 黄4连：手风大顺，押注加成翻倍
        if (data.yellow4SmoothSailing && drawn.color == CardColor.Yellow)
            betBonus *= 2;

        // 颜色锁定加成
        if (data.colorLockActive)
        {
            if (drawn.color == data.selectedMainColor)
            {
                betBonus += data.colorLockBonus;
            }
        }

        // 强制抽牌加成
        if (data.forcedDrawCount > 0)
        {
            betBonus += data.forcedDrawBonus;
            data.forcedDrawCount--;
        }

        // 孤注一掷加成
        if (data.allOrNothingActive)
        {
            betBonus += 2;
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

        // 修正保单上限
        data.policy = Mathf.Min(data.policy, data.maxPolicy);
    }

    /// <summary>
    /// 应用卡牌颜色对应的基础效果
    /// </summary>
    private void ApplyColorEffect(RuntimeCard drawn, int finalCardAtk)
    {
        switch (drawn.color)
        {
            case CardColor.Blue:
                data.policy++;
                break;

            case CardColor.Yellow:
                float addedMult = data.yellow4SmoothSailing ? 0.2f : 0.1f;
                data.bonusYellowMult += addedMult;
                break;

            case CardColor.Red:
                int hpDmg = Mathf.CeilToInt(finalCardAtk * 0.2f);
                data.TakeDamage(hpDmg);
                debtManager.AddDebt();
                break;
        }
    }

    /// <summary>
    /// 获取DebtManager实例
    /// </summary>
    public DebtManager GetDebtManager()
    {
        return debtManager;
    }
}