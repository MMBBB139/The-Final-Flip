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
    public Button drawPile;
    public GameObject cardUIPrefab;

    // ========== 牌堆信息UI ==========
    [Header("牌堆信息")]
    public TextMeshProUGUI totalRemainingText;
    public TextMeshProUGUI blueRemainingText;
    public TextMeshProUGUI yellowRemainingText;
    public TextMeshProUGUI redRemainingText;

    // ========== 牌堆查看面板 ==========
    [Header("牌堆查看面板")]
    public GameObject drawPileDetailPanel;
    public Transform drawPileDetailContent;
    public Button closeDrawPileDetailButton;

    private List<RuntimeCard> currentDrawPile;
    private List<GameObject> detailCardObjects = new List<GameObject>();

    public event Action<CardColor> OnMainColorSelected;

    private void Awake()
    {
        blueButton.onClick.AddListener(() => SelectMainColor(CardColor.Blue));
        yellowButton.onClick.AddListener(() => SelectMainColor(CardColor.Yellow));
        redButton.onClick.AddListener(() => SelectMainColor(CardColor.Red));

        if (drawPileDetailPanel != null)
            drawPileDetailPanel.SetActive(false);

        if (closeDrawPileDetailButton != null)
            closeDrawPileDetailButton.onClick.AddListener(HideDrawPileDetailPanel);

        // 牌堆点击打开详情面板
        drawPile.onClick.AddListener(ShowDrawPileDetailPanel);
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

        HideDrawPileDetailPanel();
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

    public void UpdateDrawPileInfo(List<RuntimeCard> drawPile)
    {
        currentDrawPile = drawPile;

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

        if (totalRemainingText != null)
            totalRemainingText.text = $"{drawPile.Count}";

        if (blueRemainingText != null)
            blueRemainingText.text = $"<color=blue>{blueCount}</color>";

        if (yellowRemainingText != null)
            yellowRemainingText.text = $"<color=yellow>{yellowCount}</color>";

        if (redRemainingText != null)
            redRemainingText.text = $"<color=red>{redCount}</color>";
    }

    public void HideDrawPileInfo()
    {
        HideDrawPileDetailPanel();
    }

    // ==================== 牌堆查看面板 ====================

    public void ShowDrawPileDetailPanel()
    {
        if (drawPileDetailPanel == null || currentDrawPile == null || currentDrawPile.Count == 0)
            return;

        drawPileDetailPanel.SetActive(true);
        RefreshDrawPileDetailContent();
    }

    public void HideDrawPileDetailPanel()
    {
        if (drawPileDetailPanel != null)
            drawPileDetailPanel.SetActive(false);
    }

    private void RefreshDrawPileDetailContent()
    {
        if (drawPileDetailContent == null || cardUIPrefab == null || currentDrawPile == null)
            return;

        // 清空旧的卡牌对象
        foreach (var obj in detailCardObjects)
        {
            Destroy(obj);
        }
        detailCardObjects.Clear();

        // 排序：先按颜色分组（蓝→黄→红），同色内攻击力从低到高
        List<RuntimeCard> sortedList = new List<RuntimeCard>(currentDrawPile);
        sortedList.Sort((a, b) =>
        {
            int colorCompare = GetColorPriority(a.color).CompareTo(GetColorPriority(b.color));
            if (colorCompare != 0) return colorCompare;
            return a.GetAttackValue().CompareTo(b.GetAttackValue());
        });

        // 复用 cardUIPrefab 生成卡牌
        foreach (var card in sortedList)
        {
            GameObject cardObj = Instantiate(cardUIPrefab, drawPileDetailContent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Init(card);
            }
            detailCardObjects.Add(cardObj);
        }
    }

    private int GetColorPriority(CardColor color)
    {
        return color switch
        {
            CardColor.Blue => 0,
            CardColor.Yellow => 1,
            CardColor.Red => 2,
            _ => 3
        };
    }

    public void ShowGameOver(bool isWin)
    {
        gameOverPanel.SetActive(true);
        gameOverTitle.text = isWin ? "<color=green>Victory!</color>" : "<color=red>Defeat!</color>";
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