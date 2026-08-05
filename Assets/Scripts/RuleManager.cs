using UnityEngine;

public class RuleManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    [SerializeField] private Deck deck;
    [SerializeField] private CorrectionManager correctionManager;

    private int currentLayer;
    private bool specialRuleDisabled;
    private bool nextCardFaded;
    private int fadedRevealCount;

    void Awake()
    {
        if (correctionManager != null)
            correctionManager.OnCorrectionUsed.AddListener(OnCorrectionUsed);
    }

    void OnDestroy()
    {
        if (correctionManager != null)
            correctionManager.OnCorrectionUsed.RemoveListener(OnCorrectionUsed);
    }

    public void InitializeForStage(int layer, int stage)
    {
        currentLayer = layer;
        specialRuleDisabled = false;
        nextCardFaded = false;
        fadedRevealCount = 0;
    }

    public void DisableSpecialRule()
    {
        specialRuleDisabled = true;
        nextCardFaded = false;
        Debug.Log("[消除特殊] 当前全局特殊规则已禁用");
    }

    /// <summary>
    /// 使用主动策略牌后触发（第4层全局规则：下一张为褪色牌）
    /// </summary>
    public void OnStrategyCardUsed()
    {
        if (currentLayer == 4 && !specialRuleDisabled)
        {
            nextCardFaded = true;
            Debug.Log("[规则] 使用策略牌，下一张将为褪色牌");
        }
    }

    public bool ShouldDrawFadedCard()
    {
        if (currentLayer == 4 && !specialRuleDisabled && nextCardFaded)
        {
            nextCardFaded = false;
            return true;
        }
        return false;
    }

    public void OnCardDrawn(bool wasFaded)
    {
    }

    public bool IsLayer3LimitReached(int drawnCount)
    {
        if (currentLayer == 3 && !specialRuleDisabled)
        {
            int limit = config != null ? config.layer3MaxDrawLimit : 25;
            return drawnCount > limit;
        }
        return false;
    }

    private void OnCorrectionUsed(int remaining, int cost)
    {
        // 第1层无特殊规则，已删除洗回逻辑
    }

    public int GetCorrectionWindowOffset()
    {
        if (currentLayer == 2 && !specialRuleDisabled)
            return config != null ? config.layer2CorrectionWindowOffset : 3;
        return 0;
    }

    /// <summary>
    /// 恢复褪色牌显示（第4层全局规则），价格递增
    /// </summary>
    public bool RevealFadedCard(int cardIndex, ChipsManager chips)
    {
        int baseCost = config != null ? config.layer4FadedBaseCost : 10;
        int cost = baseCost * (fadedRevealCount + 1);

        if (chips.GetChips() < cost) return false;
        chips.AddChips(-cost);
        deck.RevealFadedCard(cardIndex);
        fadedRevealCount++;
        Debug.Log($"[规则] 恢复褪色牌，消耗{cost}，下次恢复价格{baseCost * (fadedRevealCount + 1)}");
        return true;
    }

    public bool IsLayer4FadedRule() => currentLayer == 4 && !specialRuleDisabled;
    public int GetCurrentLayer() => currentLayer;

    public void ResetAll()
    {
        specialRuleDisabled = false;
        nextCardFaded = false;
        fadedRevealCount = 0;
    }
}