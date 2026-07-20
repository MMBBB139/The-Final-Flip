using UnityEngine;

public class ComboManager
{
    private BattleData data;

    public ComboManager(BattleData data)
    {
        this.data = data;
    }

    /// <summary>
    /// 处理收牌后的连击更新和技能触发
    /// </summary>
    public void ProcessCombo(RuntimeCard drawn, ref int finalCardAtk)
    {
        // 连击期待（3连以上且同色）
        if (data.comboCount >= 3 && data.comboColor == drawn.color)
        {
            finalCardAtk += 1;
            if (drawn.color == CardColor.Blue) data.policy += 1;
            if (drawn.color == CardColor.Yellow) data.bonusYellowMult += 0.1f;
        }

        // 黄2连蓄力
        if (data.nextCardPlusOneAttack)
        {
            finalCardAtk += 1;
            data.nextCardPlusOneAttack = false;
        }

        // 更新连击计数
        if (drawn.color == data.comboColor)
            data.comboCount++;
        else
        {
            data.comboColor = drawn.color;
            data.comboCount = 1;
        }

        // 技能树触发
        ApplySkillTree(drawn, ref finalCardAtk);
    }

    private void ApplySkillTree(RuntimeCard drawn, ref int finalCardAtk)
    {
        if (data.comboColor == CardColor.Blue)
        {
            if (data.comboCount == 2) data.policy += 1;
            if (data.comboCount >= 4) data.blueComboSafetyNet = true;
            if (data.comboCount >= 6) data.blue6SettleReady = true;
        }
        else if (data.comboColor == CardColor.Yellow)
        {
            if (data.comboCount == 2) data.nextCardPlusOneAttack = true;
            if (data.comboCount >= 4) data.yellow4SmoothSailing = true;
        }
        else if (data.comboColor == CardColor.Red)
        {
            if (data.comboCount == 2) finalCardAtk += 2;
            if (data.comboCount >= 6) data.red6DetonateReady = true;
        }
    }

    /// <summary>
    /// 停手时计算额外伤害（蓝6清算、红6引爆）
    /// </summary>
    public int CalculateExtraDamage()
    {
        int extraDmg = 0;

        if (data.blue6SettleReady)
            extraDmg += data.policy * 3;

        if (data.red6DetonateReady)
        {
            extraDmg += Mathf.FloorToInt(data.currentAttack * 0.2f * data.debtCount);
            data.TakeDamage(2);
        }

        return extraDmg;
    }
}