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
    public TextMeshProUGUI policyText;     // 原 shieldText，现改为保单
    public TextMeshProUGUI debtText;       // 原 curseText，现改为债痕
    public TextMeshProUGUI comboText;      // 新增：连击显示
    public TextMeshProUGUI bustRateText;   // 新增：爆牌率显示
    public TextMeshProUGUI bossHpText;
    public TextMeshProUGUI turnText;

    [Header("主动决策区")]
    public GameObject pendingActionsPanel; // 包含“收下”和“跳过”按钮的父节点
    public Button takeButton;
    public Button skipButton;
    public Transform pendingCardArea;      // 发牌中间停顿点

    [Header("结算公式面板")]
    public TextMeshProUGUI calcFormulaText;
    public TextMeshProUGUI floatingText;   // 用于浮动提示(保单生效/回血等)

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

    [Header("牌堆信息")]
    public TextMeshProUGUI totalRemainingText;
    public TextMeshProUGUI blueRemainingText;
    public TextMeshProUGUI yellowRemainingText;
    public TextMeshProUGUI redRemainingText;

    [Header("牌堆查看面板")]
    public GameObject drawPileDetailPanel;
    public Transform drawPileDetailContent;
    public Button closeDrawPileDetailButton;

    private List<RuntimeCard> currentDrawPile;
    private List<GameObject> detailCardObjects = new List<GameObject>();
    private GameObject currentPendingCardObj;

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

        drawPile.onClick.AddListener(ShowDrawPileDetailPanel);

        // 初始化新UI状态
        SetPendingState(false);
        if (calcFormulaText != null) calcFormulaText.gameObject.SetActive(false);
        if (floatingText != null) floatingText.gameObject.SetActive(false);
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

        if (policyText != null)
        {
            policyText.text = $"Policy: {data.policy}/{data.maxPolicy}";
            policyText.gameObject.SetActive(data.policy > 0);
        }

        if (comboText != null)
        {
            float finalMult = BattleRules.GetBaseComboMultiplier(data.comboCount) + data.bonusYellowMult;
            comboText.text = data.comboCount > 0
                ? $"Combo: {data.comboCount} (x{finalMult:F1})"
                : "Combo: 0 (x1.0)";
        }

        if (bustRateText != null)
        {
            float nextBustRate = BattleRules.GetNextBustRate(data);
            bustRateText.text = nextBustRate == 0f
                ? "Bust: 0% (Safe)"
                : $"Bust: {nextBustRate:P0}";
        }

        debtText.text = $"Debt: {data.debtCount}/{data.debtThreshold}";
        bossHpText.text = $"Boss: {data.bossHp}/{data.maxBossHp}";
        turnText.text = $"Turn: {data.currentTurn}/{data.maxTurns}";
    }

    // ==========================================
    // 新增：挂起/停顿确认区 (跳过或收下)
    // ==========================================
    public void ShowPendingCard(RuntimeCard card)
    {
        if (pendingCardArea == null) return;
        if (currentPendingCardObj != null) Destroy(currentPendingCardObj);

        currentPendingCardObj = Instantiate(cardUIPrefab, pendingCardArea);
        currentPendingCardObj.GetComponent<CardUI>()?.Init(card);
    }

    public void HidePendingCard()
    {
        if (currentPendingCardObj != null) Destroy(currentPendingCardObj);
    }

    public void SetPendingState(bool isPending)
    {
        if (pendingActionsPanel != null) pendingActionsPanel.SetActive(isPending);
    }

    // ==========================================
    // 新增：公式与浮动提示
    // ==========================================
    public void ShowCalcFormula(int baseAtk, float mult, int extra, float weakMult, int total)
    {
        if (calcFormulaText == null) return;
        calcFormulaText.gameObject.SetActive(true);
        calcFormulaText.text = $"({baseAtk} x {mult:F1} + {extra}) x {weakMult:F1} = <color=red>{total}</color>";
    }

    public void HideCalcFormula()
    {
        if (calcFormulaText != null) calcFormulaText.gameObject.SetActive(false);
    }

    public void ShowFloatingText(string msg)
    {
        if (floatingText == null) return;
        StopCoroutine("AnimateFloatingText");
        StartCoroutine("AnimateFloatingText", msg);
    }

    private IEnumerator AnimateFloatingText(string msg)
    {
        floatingText.text = msg;
        floatingText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        floatingText.gameObject.SetActive(false);
    }

    // ==========================================
    // 牌堆信息与查看 (已完整恢复)
    // ==========================================
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

        if (totalRemainingText != null) totalRemainingText.text = $"{drawPile.Count}";
        if (blueRemainingText != null) blueRemainingText.text = $"<color=blue>{blueCount}</color>";
        if (yellowRemainingText != null) yellowRemainingText.text = $"<color=yellow>{yellowCount}</color>";
        if (redRemainingText != null) redRemainingText.text = $"<color=red>{redCount}</color>";
    }

    public void HideDrawPileInfo()
    {
        HideDrawPileDetailPanel();
    }

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

        foreach (var obj in detailCardObjects)
        {
            Destroy(obj);
        }
        detailCardObjects.Clear();

        List<RuntimeCard> sortedList = new List<RuntimeCard>(currentDrawPile);
        sortedList.Sort((a, b) =>
        {
            int colorCompare = GetColorPriority(a.color).CompareTo(GetColorPriority(b.color));
            if (colorCompare != 0) return colorCompare;
            return a.GetAttackValue().CompareTo(b.GetAttackValue());
        });

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

    // ==========================================
    // 基础牌桌逻辑与爆牌动画 (已完整恢复)
    // ==========================================
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
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            explosionCanvasGroup.alpha = t / 0.3f;
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);

        t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            explosionCanvasGroup.alpha = 1 - t / 0.3f;
            yield return null;
        }
        explosionCanvasGroup.alpha = 0;
    }
}