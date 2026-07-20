using UnityEngine;

/// <summary>
/// 卡牌效果执行器：负责收牌时的攻击力计算、卡色效果触发、债痕更新
/// </summary>
public class CardEffectExecutor
{
    private BattleData data;
    private ComboManager comboManager;

    public CardEffectExecutor(BattleData data, ComboManager comboManager)
    {
        this.data = data;
        this.comboManager = comboManager;
    }

    /// <summary>
    /// 执行收牌效果：计算攻击力、触发连击、应用卡色效果
    /// </summary>
    public void ExecuteTakeCard(RuntimeCard drawn)
    {
        // 计算底数 + 押注加成
        int baseAtk = drawn.rank;
        int betBonus = data.GetMainColorBonus(drawn.color);

        // 黄4连：手风大顺，押注加成翻倍
        if (data.yellow4SmoothSailing && drawn.color == CardColor.Yellow)
            betBonus *= 2;

        int finalCardAtk = baseAtk + betBonus;

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
    /// - 蓝牌：获得1份保单
    /// - 黄牌：增加连击倍率
    /// - 红牌：自伤 + 累积债痕
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
                data.debtCount++;

                if (data.debtCount >= data.debtThreshold)
                {
                    data.debtCount = 0;
                    GameEvents.RaiseFloatingText("Debt full! Liquidation triggered!");
                }
                break;
        }
    }
}