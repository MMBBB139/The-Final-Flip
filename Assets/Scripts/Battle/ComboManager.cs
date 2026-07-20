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
        // 2连奖励
        if (data.nextCardDoubled && drawn.color == data.comboColor)
        {
            finalCardAtk *= 2;
            data.nextCardDoubled = false;
        }

        // 更新连击计数
        if (drawn.color == data.comboColor)
            data.comboCount++;
        else
        {
            data.comboColor = drawn.color;
            data.comboCount = 1;
        }

        // 统一2连奖励
        if (data.comboCount == 2)
        {
            data.nextCardDoubled = true;
            GameEvents.RaiseFloatingText($"Combo x2! Next {drawn.color} card ATK doubled!");
        }

        // 统一4连奖励：额外触发一次该花色基础效果
        if (data.comboCount >= 4)
        {
            ApplyExtraColorEffect(drawn, ref finalCardAtk);
        }
    }

    private void ApplyExtraColorEffect(RuntimeCard drawn, ref int finalCardAtk)
    {
        switch (drawn.color)
        {
            case CardColor.Blue:
                data.policy++;
                GameEvents.RaiseFloatingText("Combo x4! Extra policy +1.");
                break;
            case CardColor.Yellow:
                data.bonusYellowMult += 0.1f;
                GameEvents.RaiseFloatingText("Combo x4! Extra combo mult +0.1.");
                break;
            case CardColor.Red:
                finalCardAtk += 1;
                GameEvents.RaiseFloatingText("Combo x4! Extra ATK +1.");
                break;
        }
    }

    public int CalculateExtraDamage()
    {
        return 0; // 简化版无额外伤害
    }
}