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

        // 回合开始时显示牌堆信息
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

        // 抽牌后实时更新牌堆信息
        battleUI.UpdateDrawPileInfo(data.drawPile);

        // 爆牌判定
        if (BattleRules.CheckExplosion(data.handCards.Count, data.GetSafeZoneSize()))
        {
            HandleExplosion();
            return;
        }

        // 计算单张牌的攻击力
        int mainColorBonus = data.GetMainColorBonus(drawn.color);
        int cardAttack = drawn.GetAttackValue() + mainColorBonus;

        // 【Bug修复：移除抽牌时的1.5倍计算，将其移至OnStopClicked结算阶段】

        data.currentAttack += cardAttack;

        // 颜色效果
        BattleRules.ApplyCardColorEffect(drawn, data);

        // 诅咒判定
        if (data.isCurseReady)
            BattleRules.TriggerCursePenalty(data);

        // UI生成
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

        // 爆牌时隐藏牌堆信息
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

        // 停手时隐藏牌堆信息
        battleUI.HideDrawPileInfo();

        // 【Bug修复：在这里进行伤害结算时，乘以Boss弱点倍率】
        int finalDamage = data.currentAttack;

        // 如果没有爆牌，则计算所有打出弱点颜色牌的加成（符合“最后结算”规则）
        if (!isBusted)
        {
            float weaknessMultiplier = 1.5f;
            int weaknessBonusDamage = 0;

            foreach (var card in data.handCards)
            {
                if (card.color == data.bossWeaknessColor)
                {
                    int cardAtk = card.GetAttackValue() + data.GetMainColorBonus(card.color);
                    // 累加额外的那0.5倍伤害
                    weaknessBonusDamage += Mathf.RoundToInt(cardAtk * (weaknessMultiplier - 1f));
                }
            }
            finalDamage += weaknessBonusDamage;
            data.currentAttack = finalDamage; // 更新面板，让玩家看得到加成后的最终攻击力
        }

        data.bossHp -= finalDamage;

        battleUI.UpdateAllUI(data);

        if (data.isBossDead)
        {
            StartCoroutine(DelayedEnd(true));
        }
        // 【Bug修复：判定是否超过最大回合限制（4回合未击杀则游戏结束）】
        else if (data.isMaxTurnsReached)
        {
            StartCoroutine(DelayedEnd(false));
        }
        else
        {
            // Boss攻击玩家
            StartCoroutine(BossAttackPhase());
        }
    }

    // ==================== Boss攻击阶段 ====================
    private IEnumerator BossAttackPhase()
    {
        yield return new WaitForSeconds(0.5f);

        // 第一层Boss攻击力为2
        int bossAttack = 2;
        data.playerHp -= bossAttack;

        // 更新UI，playerHp的text会立即变化
        battleUI.UpdateAllUI(data);

        // 检查玩家是否死亡
        if (data.isPlayerDead)
        {
            StartCoroutine(DelayedEnd(false));
            yield break;
        }

        yield return new WaitForSeconds(1f);

        data.currentTurn++;
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