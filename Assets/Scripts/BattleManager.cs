using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("Core Managers")]
    public DeckManager deckManager;

    [Header("UI References")]
    public Transform handArea;         // 带有 HorizontalLayoutGroup 的空物体
    public Button drawButton;          // 抽卡按钮
    public RectTransform drawPileVisual;
    public GameObject cardUIPrefab;    // 卡牌预制体
    public TextMeshProUGUI attackText; // 攻击力显示文本

    // 状态数据
    private List<RuntimeCard> drawPile;
    private List<RuntimeCard> handCards = new List<RuntimeCard>();
    private int currentAttack = 0;

    void Start()
    {
        // 初始化牌堆 (MVP默认12张)
        drawPile = deckManager.GenerateInitialDeck(12);

        // 绑定点击事件
        drawButton.onClick.AddListener(OnDeckClicked);
        UpdateUI();
    }

    private void OnDeckClicked()
    {
        // 如果手牌超过7张，或者牌堆空了，则不能翻
        if (handCards.Count >= 7 || drawPile.Count == 0) return;

        DrawCard();
    }

    private void DrawCard()
    {
        // 1. 从牌堆抽出一张
        RuntimeCard drawnCard = drawPile[0];
        drawPile.RemoveAt(0);
        handCards.Add(drawnCard);

        int currentDrawCount = handCards.Count;

        // 2. 【核心】爆牌判定（前4张安全）
        if (CheckExplosion(currentDrawCount))
        {
            Debug.Log($"<color=red>爆牌了！第 {currentDrawCount} 张牌触发。</color>");
            HandleExplosion();
            return;
        }

        // 3. 攻击力累加（调用你在 RuntimeCard 里写的 GetAttackValue()）
        currentAttack += drawnCard.GetAttackValue();
        UpdateUI();

        // 4. 生成UI并播放飞入动画
        GameObject newCardObj = Instantiate(cardUIPrefab, handArea);
        CardUI cardUI = newCardObj.GetComponent<CardUI>();
        cardUI.Init(drawnCard);

        // 强制 LayoutGroup 立即重新计算位置，以便我们能获取到正确的 targetPos
        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());

        // 启动飞入动画，起点是牌堆按钮的屏幕坐标
        StartCoroutine(cardUI.AnimateDraw(drawPileVisual.position));
    }

    private bool CheckExplosion(int drawCount)
    {
        float risk = 0f;
        if (drawCount == 5) risk = 0.10f;
        else if (drawCount == 6) risk = 0.30f;
        else if (drawCount >= 7) risk = 0.60f;

        if (risk > 0f)
        {
            // Random.value 返回 0.0 到 1.0 之间的浮点数
            return Random.value < risk;
        }
        return false;
    }

    private void HandleExplosion()
    {
        // 爆牌惩罚：攻击力清零
        currentAttack = 0;
        UpdateUI();

        // TODO: 禁用牌堆按钮，强制进入结算或结束回合逻辑
        drawButton.interactable = false;
    }

    private void UpdateUI()
    {
        attackText.text = $"Damage: {currentAttack}";
    }
}