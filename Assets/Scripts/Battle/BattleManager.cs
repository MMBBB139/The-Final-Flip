using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    public DeckManager deckManager;
    public BattleUI battleUI;

    [Header("Buttons")]
    public Button drawButton;
    public Button stopButton;

    private BattleData data;
    private bool isBusted;

    void Start()
    {
        drawButton.onClick.AddListener(OnDrawClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        battleUI.restartButton.onClick.AddListener(RestartGame);
        battleUI.OnMainColorSelected += OnMainColorSelectedHandler;

        battleUI.explosionCanvasGroup.alpha = 0;
        battleUI.gameOverPanel.SetActive(false);

        data = new BattleData();
        data.levelDeck = deckManager.GenerateInitialDeck(12);

        // 初始隐藏牌堆信息（等选完主色才显示）
        battleUI.HideDrawPileInfo();

        // 第一回合开始，先选主色
        StartMainColorSelection();
    }

    // ==================== 主色选择 ====================
    private void StartMainColorSelection()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;

        // 第1层Boss固定黄牌弱点
        data.bossWeaknessColor = CardColor.Yellow;

        var probabilities = new Dictionary<CardColor, float>
        {
            { CardColor.Blue, 0.50f },
            { CardColor.Yellow, 0.33f },
            { CardColor.Red, 0.17f }
        };

        battleUI.ShowMainColorPanel(data.bossWeaknessColor, probabilities);
    }

    private void OnMainColorSelectedHandler(CardColor color)
    {
        data.selectedMainColor = color;
        data.firstCardGuaranteed = true;

        // 红主色：诅咒+2
        if (color == CardColor.Red)
        {
            data.curseCount += 2;
            if (data.isCurseReady)
                BattleRules.TriggerCursePenalty(data);
        }

        StartNewTurn();
    }

    // ==================== 回合流程 ====================
    private void StartNewTurn()
    {
        // 每回合重新生成带权重的抽牌堆
        data.drawPile = BattleRules.GenerateWeightedDrawPile(data.levelDeck, data.selectedMainColor);
        BattleRules.EnsureFirstCardIsMainColor(data.drawPile, data.selectedMainColor);

        battleUI.ClearHand();
        data.handCards.Clear();
        data.currentAttack = 0;
        isBusted = false;

        drawButton.interactable = true;
        stopButton.interactable = true;
        battleUI.UpdateAllUI(data);

        // ========== 新增：回合开始时显示牌堆信息 ==========
        battleUI.UpdateDrawPileInfo(data.drawPile);
    }

    // ==================== 抽牌 ====================
    private void OnDrawClicked()
    {
        if (isBusted || data.handCards.Count >= data.maxHandSize || data.drawPile.Count == 0)
            return;

        RuntimeCard drawn = data.drawPile[0];
        data.drawPile.RemoveAt(0);
        data.handCards.Add(drawn);
        data.firstCardGuaranteed = false;

        // ========== 新增：抽牌后实时更新牌堆信息 ==========
        battleUI.UpdateDrawPileInfo(data.drawPile);

        // 爆牌判定
        if (BattleRules.CheckExplosion(data.handCards.Count, data.GetSafeZoneSize()))
        {
            HandleExplosion();
            return;
        }

        // 攻击力累加
        int mainColorBonus = data.GetMainColorBonus(drawn.color);
        data.currentAttack += drawn.GetAttackValue() + mainColorBonus;

        // 颜色效果
        BattleRules.ApplyCardColorEffect(drawn, data);

        // 诅咒判定
        if (data.isCurseReady)
            BattleRules.TriggerCursePenalty(data);

        // UI生成
        GameObject cardObj = battleUI.CreateCardUI(drawn);
        LayoutRebuilder.ForceRebuildLayoutImmediate(battleUI.handArea.GetComponent<RectTransform>());
        StartCoroutine(cardObj.GetComponent<CardUI>().AnimateDraw(battleUI.drawPileVisual.position));

        battleUI.UpdateAllUI(data);

        if (data.isPlayerDead)
            StartCoroutine(DelayedEnd(false));
    }

    private void HandleExplosion()
    {
        isBusted = true;
        drawButton.interactable = false;
        battleUI.UpdateAllUI(data);

        // ========== 新增：爆牌时隐藏牌堆信息 ==========
        battleUI.HideDrawPileInfo();

        StartCoroutine(battleUI.ShowExplosionFeedback());
        data.playerHp -= 2;
        data.currentAttack = 0;

        if (data.isPlayerDead)
        {
            StartCoroutine(DelayedEnd(false));
            return;
        }

        OnStopClicked();
    }

    // ==================== 停手 ====================
    private void OnStopClicked()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;

        // ========== 新增：停手时隐藏牌堆信息 ==========
        battleUI.HideDrawPileInfo();

        float weaknessMultiplier = 1.5f;
        int finalDamage = Mathf.RoundToInt(data.currentAttack * weaknessMultiplier);
        data.bossHp -= finalDamage;

        battleUI.UpdateAllUI(data);

        if (data.isBossDead)
        {
            StartCoroutine(DelayedEnd(true));
        }
        else if (data.isMaxTurnsReached)
        {
            StartCoroutine(DelayedEnd(false));
        }
        else
        {
            data.currentTurn++;
            StartCoroutine(DelayNextTurn());
        }
    }

    private IEnumerator DelayNextTurn()
    {
        yield return new WaitForSeconds(1f);
        // 每回合重新选主色
        StartMainColorSelection();
    }

    private IEnumerator DelayedEnd(bool isWin)
    {
        yield return new WaitForSeconds(1.2f);
        drawButton.interactable = false;
        stopButton.interactable = false;
        battleUI.ShowGameOver(isWin);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}