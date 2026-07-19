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

        battleUI.HideDrawPileInfo();

        StartMainColorSelection();
    }

    private void StartMainColorSelection()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;

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

        if (color == CardColor.Red)
        {
            data.curseCount += 2;
            if (data.isCurseReady)
                BattleRules.TriggerCursePenalty(data);
        }

        StartNewTurn();
    }

    private void StartNewTurn()
    {
        // 每回合开始护盾衰减2点
        data.DecayShield(2);

        data.drawPile = BattleRules.GenerateWeightedDrawPile(data.levelDeck, data.selectedMainColor);
        BattleRules.EnsureFirstCardIsMainColor(data.drawPile, data.selectedMainColor);

        battleUI.ClearHand();
        data.handCards.Clear();
        data.currentAttack = 0;
        isBusted = false;

        drawButton.interactable = true;
        stopButton.interactable = true;
        battleUI.UpdateAllUI(data);

        battleUI.UpdateDrawPileInfo(data.drawPile);
    }

    private void OnDrawClicked()
    {
        if (isBusted || data.handCards.Count >= data.maxHandSize || data.drawPile.Count == 0)
            return;

        RuntimeCard drawn = data.drawPile[0];
        data.drawPile.RemoveAt(0);
        data.handCards.Add(drawn);
        data.firstCardGuaranteed = false;

        battleUI.UpdateDrawPileInfo(data.drawPile);

        if (BattleRules.CheckExplosion(data.handCards.Count, data.GetSafeZoneSize()))
        {
            HandleExplosion();
            return;
        }

        int mainColorBonus = data.GetMainColorBonus(drawn.color);
        int cardAttack = drawn.GetAttackValue() + mainColorBonus;

        // 弱点倍率提前计算，让玩家实时看到增益后的伤害
        if (drawn.color == data.bossWeaknessColor)
        {
            cardAttack = Mathf.RoundToInt(cardAttack * 1.5f);
        }

        data.currentAttack += cardAttack;

        BattleRules.ApplyCardColorEffect(drawn, data);

        if (data.isCurseReady)
            BattleRules.TriggerCursePenalty(data);

        GameObject cardObj = battleUI.CreateCardUI(drawn);
        LayoutRebuilder.ForceRebuildLayoutImmediate(battleUI.handArea.GetComponent<RectTransform>());
        StartCoroutine(cardObj.GetComponent<CardUI>().AnimateDraw(battleUI.drawPile.transform.position));

        battleUI.UpdateAllUI(data);

        if (data.isPlayerDead)
            StartCoroutine(DelayedEnd(false));
    }

    private void HandleExplosion()
    {
        isBusted = true;
        drawButton.interactable = false;
        battleUI.UpdateAllUI(data);

        battleUI.HideDrawPileInfo();

        StartCoroutine(battleUI.ShowExplosionFeedback());

        // 爆牌扣2血，优先消耗护盾
        data.TakeDamage(2);
        data.currentAttack = 0;

        if (data.isPlayerDead)
        {
            StartCoroutine(DelayedEnd(false));
            return;
        }

        OnStopClicked();
    }

    private void OnStopClicked()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;

        battleUI.HideDrawPileInfo();

        // 弱点倍率已在抽牌时计算，这里直接使用currentAttack
        int finalDamage = data.currentAttack;

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
            StartCoroutine(BossAttackPhase());
        }
    }

    private IEnumerator BossAttackPhase()
    {
        yield return new WaitForSeconds(0.5f);

        // Boss攻击，优先消耗护盾
        data.TakeDamage(data.BossDamage);

        battleUI.UpdateAllUI(data);

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
        battleUI.ShowGameOver(isWin);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}