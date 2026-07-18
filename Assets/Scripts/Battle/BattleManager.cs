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

        battleUI.explosionCanvasGroup.alpha = 0;
        battleUI.gameOverPanel.SetActive(false);

        data = new BattleData();
        data.levelDeck = deckManager.GenerateInitialDeck(12);
        StartNewTurn();
    }

    // ==================== 回合流程 ====================
    private void StartNewTurn()
    {
        battleUI.ClearHand();
        data.handCards.Clear();
        data.currentAttack = 0;
        isBusted = false;

        data.drawPile = new List<RuntimeCard>(data.levelDeck);
        BattleRules.ShuffleList(data.drawPile);

        drawButton.interactable = true;
        stopButton.interactable = true;
        battleUI.UpdateAllUI(data);
    }

    // ==================== 抽牌 ====================
    private void OnDrawClicked()
    {
        if (isBusted || data.handCards.Count >= data.maxHandSize || data.drawPile.Count == 0)
            return;

        RuntimeCard drawn = data.drawPile[0];
        data.drawPile.RemoveAt(0);
        data.handCards.Add(drawn);

        // 爆牌判定（优先级最高）
        if (BattleRules.CheckExplosion(data.handCards.Count))
        {
            HandleExplosion();
            return;
        }

        // 攻击力累加
        data.currentAttack += drawn.GetAttackValue();

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

        // 死亡判定（黄牌或红牌可能导致血量归零）
        if (data.isPlayerDead)
            StartCoroutine(DelayedEnd(false));
    }

    private void HandleExplosion()
    {
        isBusted = true;
        drawButton.interactable = false;
        battleUI.UpdateAllUI(data);
        StartCoroutine(battleUI.ShowExplosionFeedback());
        data.playerHp -= 2;
        data.currentAttack = 0;
        OnStopClicked();
    }

    // ==================== 停手 ====================
    private void OnStopClicked()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;

        // 造成伤害
        data.bossHp -= data.currentAttack;
        battleUI.UpdateAllUI(data);

        // 结算判定
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

    // ==================== 协程辅助 ====================
    private IEnumerator DelayNextTurn()
    {
        yield return new WaitForSeconds(1f);
        StartNewTurn();
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