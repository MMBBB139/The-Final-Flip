using UnityEngine;

public class RuleManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    [SerializeField] private Deck deck;
    [SerializeField] private CorrectionManager correctionManager;

    private int currentLayer;
    private bool isSpecialStage;
    private bool specialRuleDisabled;
    private bool nextCardFaded;

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
        isSpecialStage = (stage == 3);
        specialRuleDisabled = false;
        nextCardFaded = false;
    }

    public void DisableSpecialRule()
    {
        specialRuleDisabled = true;
        nextCardFaded = false;
        Debug.Log("[消除特殊] 当前特殊规则已禁用");
    }

    /// <summary>
    /// 使用策略牌后触发（第4层特殊：下一张为褪色牌）
    /// </summary>
    public void OnStrategyCardUsed()
    {
        if (isSpecialStage && currentLayer == 4 && !specialRuleDisabled)
        {
            nextCardFaded = true;
            Debug.Log("[规则] 使用策略牌，下一张将为褪色牌");
        }
    }

    /// <summary>
    /// 判断当前是否应该翻褪色牌（第4层特殊规则）
    /// </summary>
    public bool ShouldDrawFadedCard()
    {
        if (isSpecialStage && currentLayer == 4 && !specialRuleDisabled && nextCardFaded)
        {
            nextCardFaded = false;
            return true;
        }
        return false;
    }

    public void OnCardDrawn(bool wasFaded)
    {
        // 褪色牌相关逻辑由Deck处理
    }

    /// <summary>
    /// 第3层特殊：前25张限制检查
    /// </summary>
    public bool IsLayer3LimitReached(int drawnCount)
    {
        if (isSpecialStage && currentLayer == 3 && !specialRuleDisabled)
        {
            int limit = config != null ? config.layer3MaxDrawLimit : 25;
            return drawnCount > limit;
        }
        return false;
    }

    private void OnCorrectionUsed(int remaining, int cost)
    {
        // 第1层特殊：使用修正后洗回最后3张
        if (isSpecialStage && currentLayer == 1 && !specialRuleDisabled)
        {
            int returnCount = config != null ? config.layer1ReturnCardCount : 3;
            deck.ReturnLastDrawnToDeck(returnCount);
            Debug.Log($"[规则] 使用修正，洗回最后{returnCount}张已翻牌");
        }
    }

    /// <summary>
    /// 第2层特殊：修正窗口提前关闭偏移
    /// </summary>
    public int GetCorrectionWindowOffset()
    {
        if (isSpecialStage && currentLayer == 2 && !specialRuleDisabled)
            return config != null ? config.layer2CorrectionWindowOffset : 3;
        return 0;
    }

    /// <summary>
    /// 恢复褪色牌显示（第4层特殊）
    /// </summary>
    public bool RevealFadedCard(int cardIndex, ChipsManager chips)
    {
        int cost = config != null ? config.layer4RevealFadedCost : 20;
        if (chips.GetChips() < cost) return false;
        chips.AddChips(-cost);
        deck.RevealFadedCard(cardIndex);
        Debug.Log($"[规则] 恢复褪色牌，消耗{cost}");
        return true;
    }

    public bool IsSpecialStage() => isSpecialStage && !specialRuleDisabled;
    public int GetCurrentLayer() => currentLayer;

    public void ResetAll()
    {
        specialRuleDisabled = false;
        nextCardFaded = false;
    }
}