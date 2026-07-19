using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("数值面板")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI curseText;
    public TextMeshProUGUI bossHpText;
    public TextMeshProUGUI turnText;

    [Header("主色选择面板")]
    public GameObject mainColorPanel;
    public Button blueButton;
    public Button yellowButton;
    public Button redButton;
    public TextMeshProUGUI bossWeaknessText;
    public TextMeshProUGUI blueProbText;
    public TextMeshProUGUI yellowProbText;
    public TextMeshProUGUI redProbText;

    [Header("爆牌反馈")]
    public CanvasGroup explosionCanvasGroup;
    public TextMeshProUGUI explosionText;

    [Header("结算面板")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitle;
    public Button restartButton;

    [Header("手牌区域")]
    public Transform handArea;
    public RectTransform drawPileVisual;
    public GameObject cardUIPrefab;

    // ========== 新增：牌堆信息UI ==========
    [Header("牌堆信息")]
    public GameObject drawPileInfoPanel;           // 牌堆信息面板（常驻显示）
    public TextMeshProUGUI totalRemainingText;      // 总剩余张数
    public TextMeshProUGUI blueRemainingText;       // 蓝牌剩余
    public TextMeshProUGUI yellowRemainingText;     // 黄牌剩余
    public TextMeshProUGUI redRemainingText;        // 红牌剩余

    public event Action<CardColor> OnMainColorSelected;

    private void Awake()
    {
        blueButton.onClick.AddListener(() => SelectMainColor(CardColor.Blue));
        yellowButton.onClick.AddListener(() => SelectMainColor(CardColor.Yellow));
        redButton.onClick.AddListener(() => SelectMainColor(CardColor.Red));
    }

    private void SelectMainColor(CardColor color)
    {
        OnMainColorSelected?.Invoke(color);
        HideMainColorPanel();
    }

    public void ShowMainColorPanel(CardColor bossWeakness, Dictionary<CardColor, float> probabilities)
    {
        mainColorPanel.SetActive(true);

        string weaknessColorName = bossWeakness switch
        {
            CardColor.Blue => "<color=blue>Blue</color>",
            CardColor.Yellow => "<color=yellow>Yellow</color>",
            CardColor.Red => "<color=red>Red</color>",
            _ => "Unknown"
        };
        bossWeaknessText.text = $"Boss Weakness: {weaknessColorName}\nDamage x1.5";

        blueProbText.text = $"Rate: {probabilities[CardColor.Blue]:P0}";
        yellowProbText.text = $"Rate: {probabilities[CardColor.Yellow]:P0}";
        redProbText.text = $"Rate: {probabilities[CardColor.Red]:P0}";

        // 主色选择时隐藏牌堆信息（还没开始抽牌）
        if (drawPileInfoPanel != null)
            drawPileInfoPanel.SetActive(false);
    }

    public void HideMainColorPanel()
    {
        mainColorPanel.SetActive(false);
    }

    public void UpdateAllUI(BattleData data)
    {
        attackText.text = $"Damage: \n{data.currentAttack}";
        playerHpText.text = $"HP: {data.playerHp}/{data.maxPlayerHp}";
        curseText.text = $"Curse: {data.curseCount}/{data.curseThreshold}";
        bossHpText.text = $"Boss: {data.bossHp}/{data.maxBossHp}";
        turnText.text = $"Turn: {data.currentTurn}/{data.maxTurns}";
    }

    // ========== 新增：更新牌堆剩余信息 ==========
    public void UpdateDrawPileInfo(List<RuntimeCard> drawPile)
    {
        if (drawPileInfoPanel == null) return;

        // 显示面板
        drawPileInfoPanel.SetActive(true);

        // 计算各颜色剩余数量
        int blueCount = 0, yellowCount = 0, redCount = 0;
        foreach (var card in drawPile)
        {
            switch (card.color)
            {
                case CardColor.Blue: blueCount++; break;
                case CardColor.Yellow: yellowCount++; break;
                case CardColor.Red: redCount++; break;
            }
        }

        // 更新文本
        if (totalRemainingText != null)
            totalRemainingText.text = $"{drawPile.Count}";

        if (blueRemainingText != null)
            blueRemainingText.text = $"<color=blue>{blueCount}</color>";

        if (yellowRemainingText != null)
            yellowRemainingText.text = $"<color=yellow>{yellowCount}</color>";

        if (redRemainingText != null)
            redRemainingText.text = $"<color=red>{redCount}</color>";
    }

    // 隐藏牌堆信息（回合结束/爆牌时调用）
    public void HideDrawPileInfo()
    {
        if (drawPileInfoPanel != null)
            drawPileInfoPanel.SetActive(false);
    }

    public void ShowGameOver(bool isWin)
    {
        gameOverPanel.SetActive(true);
        gameOverTitle.text = isWin ? "<color=green>Victory!</color>" : "<color=red>Defeat!</color>";

        // 游戏结束隐藏牌堆信息
        HideDrawPileInfo();
    }

    public GameObject CreateCardUI(RuntimeCard card)
    {
        GameObject obj = Instantiate(cardUIPrefab, handArea);
        CardUI cardUI = obj.GetComponent<CardUI>();
        cardUI?.Init(card);
        return obj;
    }

    public void ClearHand()
    {
        foreach (Transform child in handArea)
            Destroy(child.gameObject);
    }

    public IEnumerator ShowExplosionFeedback()
    {
        explosionText.text = "BUST!";
        float t = 0;
        while (t < 0.3f) { t += Time.deltaTime; explosionCanvasGroup.alpha = t / 0.3f; yield return null; }
        yield return new WaitForSeconds(0.8f);
        t = 0;
        while (t < 0.3f) { t += Time.deltaTime; explosionCanvasGroup.alpha = 1 - t / 0.3f; yield return null; }
        explosionCanvasGroup.alpha = 0;
    }
}