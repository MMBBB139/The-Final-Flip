// BattleUI.cs
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

    public void ShowGameOver(bool isWin)
    {
        gameOverPanel.SetActive(true);
        gameOverTitle.text = isWin ? "<color=green>Victory!</color>" : "<color=red>Defeat!</color>";
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