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
    private RuntimeCard pendingCard;
    private ComboManager comboManager;
    private CardEffectExecutor cardExecutor;
    private DebtManager debtManager;

    void Start()
    {
        data = new BattleData();

        comboManager = new ComboManager(data);
        cardExecutor = new CardEffectExecutor(data, comboManager);
        debtManager = cardExecutor.GetDebtManager();

        drawButton.onClick.AddListener(OnDrawClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        battleUI.restartButton.onClick.AddListener(RestartGame);
        battleUI.OnMainColorSelected += OnMainColorSelectedHandler;
        if (battleUI.takeButton != null) battleUI.takeButton.onClick.AddListener(OnTakePendingCard);
        if (battleUI.skipButton != null) battleUI.skipButton.onClick.AddListener(OnSkipPendingCard);

        // 监听清算触发事件
        GameEvents.OnDebtLiquidationTriggered += OnDebtLiquidationTriggeredHandler;

        GameEvents.OnFloatingText += msg => battleUI.ShowFloatingText(msg);

        data.levelDeck = deckManager.GenerateInitialDeck(12);
        data.maxPolicy = data.levelDeck.Count;

        battleUI.HideDrawPileInfo();
        StartMainColorSelection();
    }

    private void OnDebtLiquidationTriggeredHandler(List<DebtOption> options)
    {
        // 显示清算选择面板
        battleUI.ShowDebtLiquidationPanel(options);

        // 暂停游戏流程
        drawButton.interactable = false;
        stopButton.interactable = false;
    }

    public void OnDebtOptionSelected(int optionIndex)
    {
        if (data.currentDebtOptions == null || optionIndex >= data.currentDebtOptions.Count)
            return;

        DebtOption selected = data.currentDebtOptions[optionIndex];
        debtManager.OnDebtOptionSelected(selected);

        // 隐藏面板
        battleUI.HideDebtLiquidationPanel();

        // 检查是否需要立即停手
        if (data.shouldStopImmediately)
        {
            data.shouldStopImmediately = false;
            OnStopClicked();
        }
        else
        {
            // 恢复游戏流程
            drawButton.interactable = !isBusted;
            stopButton.interactable = !isBusted;
        }

        RefreshUI();
    }

    /// <summary>
    /// 处理主动清债请求（由UI按钮触发）
    /// </summary>
    public void RequestActiveClearDebt()
    {
        bool success = debtManager.ActiveClearDebt();
        if (success)
        {
            battleUI.ShowFloatingText("Debt cleared! -3 HP");
        }
        else
        {
            battleUI.ShowFloatingText("Cannot clear debt now!");
        }
        RefreshUI();
    }

    private void StartMainColorSelection()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;
        data.bossWeaknessColor = CardColor.Yellow;

        int blueCount = 0, yellowCount = 0, redCount = 0;
        foreach (var card in data.levelDeck)
        {
            switch (card.color)
            {
                case CardColor.Blue: blueCount++; break;
                case CardColor.Yellow: yellowCount++; break;
                case CardColor.Red: redCount++; break;
            }
        }

        int total = data.levelDeck.Count;
        var probabilities = new Dictionary<CardColor, float>
        {
            { CardColor.Blue, total > 0 ? (float)blueCount / total : 0f },
            { CardColor.Yellow, total > 0 ? (float)yellowCount / total : 0f },
            { CardColor.Red, total > 0 ? (float)redCount / total : 0f }
        };

        GameEvents.RaiseMainColorSelectionStarted();
        battleUI.ShowMainColorPanel(data.bossWeaknessColor, probabilities);
    }

    private void OnMainColorSelectedHandler(CardColor color)
    {
        data.selectedMainColor = color;
        data.firstCardGuaranteed = true;

        if (color == CardColor.Blue)
            data.policy = Mathf.Min(data.policy + 2, data.maxPolicy);

        GameEvents.RaiseMainColorSelected(color);
        StartNewTurn();
    }

    private void StartNewTurn()
    {
        data.ResetTurnData();
        data.drawPile = BattleRules.GenerateWeightedDrawPile(data.levelDeck, data.selectedMainColor);
        BattleRules.EnsureFirstCardIsMainColor(data.drawPile, data.selectedMainColor);

        battleUI.ClearHand();
        data.handCards.Clear();
        data.currentAttack = 0;
        isBusted = false;
        pendingCard = null;

        drawButton.interactable = true;
        stopButton.interactable = true;

        RefreshUI();
        GameEvents.RaiseTurnStarted();
    }

    private void OnDrawClicked()
    {
        if (isBusted || data.handCards.Count >= data.maxHandSize ||
            data.drawPile.Count == 0 || pendingCard != null) return;

        pendingCard = data.drawPile[0];
        data.drawPile.RemoveAt(0);

        float bustRate = BattleRules.GetNextBustRate(data);

        if (data.nearDeathActive)
            bustRate = 0f;

        if (data.allOrNothingActive)
            bustRate *= 2f;

        if (Random.value < bustRate)
        {
            if (data.policy > 0)
            {
                data.policy--;
                GameEvents.RaiseFloatingText("Policy active! Bust prevented!");
            }
            else
            {
                HandleExplosion();
                return;
            }
        }

        drawButton.interactable = false;
        stopButton.interactable = false;
        GameEvents.RaiseCardDrawn(pendingCard);
        battleUI.ShowPendingCard(pendingCard);

        if (data.policy > 0 && !data.hasUsedSkipThisTurn && battleUI.pendingActionsPanel != null)
            battleUI.SetPendingState(true);
        else
            OnTakePendingCard();
    }

    private void OnSkipPendingCard()
    {
        if (pendingCard == null || data.policy <= 0 || data.hasUsedSkipThisTurn) return;

        data.policy--;
        data.hasUsedSkipThisTurn = true;
        data.drawPile.Add(pendingCard);

        var skippedCard = pendingCard;
        pendingCard = null;
        GameEvents.RaiseCardSkipped(skippedCard);

        battleUI.SetPendingState(false);
        battleUI.HidePendingCard();

        drawButton.interactable = true;
        stopButton.interactable = true;
        RefreshUI();
    }

    private void OnTakePendingCard()
    {
        if (pendingCard == null) return;

        RuntimeCard drawn = pendingCard;
        pendingCard = null;
        battleUI.SetPendingState(false);
        battleUI.HidePendingCard();

        data.handCards.Add(drawn);
        data.firstCardGuaranteed = false;

        cardExecutor.ExecuteTakeCard(drawn);
        GameEvents.RaiseCardTaken(drawn);

        if (data.isPlayerDead)
        {
            StartCoroutine(DelayedEnd(false));
            return;
        }

        if (data.shouldStopImmediately)
        {
            data.shouldStopImmediately = false;
            OnStopClicked();
            return;
        }

        GameObject cardObj = battleUI.CreateCardUI(drawn);
        if (battleUI.handArea != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(battleUI.handArea as RectTransform);

        Vector3 spawnPos = battleUI.drawPile.transform.position;
        StartCoroutine(cardObj.GetComponent<CardUI>().AnimateDraw(spawnPos));

        RefreshUI();
        drawButton.interactable = true;
        stopButton.interactable = true;

        if (data.forcedDrawCount > 0 && !isBusted && data.drawPile.Count > 0)
        {
            StartCoroutine(AutoDrawNextCard());
        }
    }

    private IEnumerator AutoDrawNextCard()
    {
        yield return new WaitForSeconds(0.5f);
        if (data.forcedDrawCount > 0 && !isBusted && data.drawPile.Count > 0 && pendingCard == null)
        {
            OnDrawClicked();
        }
    }

    private void HandleExplosion()
    {
        isBusted = true;
        battleUI.SetPendingState(false);
        battleUI.HidePendingCard();
        drawButton.interactable = false;
        stopButton.interactable = false;

        int bustDmg = Mathf.FloorToInt((data.handCards.Count + 1) / 2f);
        data.TakeDamage(bustDmg);
        data.currentAttack = 0;

        GameEvents.RaiseBusted();
        RefreshUI();
        battleUI.HideDrawPileInfo();
        StartCoroutine(battleUI.ShowExplosionFeedback());

        if (data.isPlayerDead)
        {
            StartCoroutine(DelayedEnd(false));
            return;
        }

        StartCoroutine(ExecuteBustEndTurn());
    }

    private IEnumerator ExecuteBustEndTurn()
    {
        yield return new WaitForSeconds(1.5f);
        debtManager.PassiveClearDebt();
        StartCoroutine(BossAttackPhase());
    }

    private void OnStopClicked()
    {
        if (data.isWaitingForDebtChoice) return;

        drawButton.interactable = false;
        stopButton.interactable = false;
        battleUI.HideDrawPileInfo();

        if (data.nearDeathActive)
        {
            data.playerHp = 1;
            data.nearDeathActive = false;
        }

        float finalMult = BattleRules.GetBaseComboMultiplier(data.comboCount) + data.bonusYellowMult;
        int extraDmg = comboManager.CalculateExtraDamage();

        float totalBase = (data.currentAttack * finalMult) + extraDmg;
        float weakMult = (data.selectedMainColor == data.bossWeaknessColor) ? 1.5f : 1.0f;
        int finalDamage = Mathf.CeilToInt(totalBase * weakMult);

        int healAmt = data.policy / 2;
        if (healAmt > 0)
        {
            data.Heal(healAmt);
            GameEvents.RaiseFloatingText($"Rest Heal: +{healAmt}");
        }

        battleUI.ShowCalcFormula(data.currentAttack, finalMult, extraDmg, weakMult, finalDamage);
        data.bossHp -= finalDamage;

        debtManager.PassiveClearDebt();

        RefreshUI();

        GameEvents.RaiseTurnEnded();

        if (data.isBossDead)
            StartCoroutine(DelayedEnd(true));
        else if (data.isMaxTurnsReached)
            StartCoroutine(DelayedEnd(false));
        else
            StartCoroutine(BossAttackPhase());
    }

    private IEnumerator BossAttackPhase()
    {
        yield return new WaitForSeconds(1.5f);
        battleUI.HideCalcFormula();

        data.TakeDamage(data.BossDamage);
        RefreshUI();

        if (data.isPlayerDead)
        {
            StartCoroutine(DelayedEnd(false));
            yield break;
        }

        yield return new WaitForSeconds(1f);
        data.currentTurn++;
        StartMainColorSelection();
    }

    private IEnumerator DelayedEnd(bool isWin)
    {
        yield return new WaitForSeconds(1.2f);
        drawButton.interactable = false;
        stopButton.interactable = false;
        GameEvents.RaiseGameEnded(isWin);
        battleUI.ShowGameOver(isWin);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RefreshUI()
    {
        battleUI.UpdateAllUI(data);
        battleUI.UpdateDrawPileInfo(data.drawPile);
    }

    private void OnDestroy()
    {
        GameEvents.OnFloatingText -= msg => battleUI.ShowFloatingText(msg);
        GameEvents.OnDebtLiquidationTriggered -= OnDebtLiquidationTriggeredHandler;
    }
}