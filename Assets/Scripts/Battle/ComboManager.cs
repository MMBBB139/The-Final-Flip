using UnityEngine;

public class ComboManager
{
    private BattleData data;

    public ComboManager(BattleData data)
    {
        this.data = data;
    }

    public void ProcessCombo(RuntimeCard drawn, ref int finalCardAtk)
    {
        // 更新连击计数
        if (drawn.color == data.comboColor)
            data.comboCount++;
        else
        {
            data.comboColor = drawn.color;
            data.comboCount = 1;
        }

        // 2连：+3攻
        if (data.comboCount == 2)
        {
            finalCardAtk += 3;
            GameEvents.RaiseFloatingText($"Combo x2! +3 ATK.");
        }

        // 3连+：翻倍
        if (data.comboCount >= 3)
        {
            finalCardAtk *= 2;
            GameEvents.RaiseFloatingText($"Combo x{data.comboCount}! ATK doubled!");
        }
    }
}