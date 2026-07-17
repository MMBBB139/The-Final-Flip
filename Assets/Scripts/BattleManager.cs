using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 用于重新加载场景

public class BattleManager : MonoBehaviour
{
    [Header("Core Managers")]
    public DeckManager deckManager;

    [Header("UI References - 手牌与牌堆")]
    public Transform handArea;
    public Button drawButton;
    public Button stopButton;          // 新增：停手按钮
    public RectTransform drawPileVisual;
    public GameObject cardUIPrefab;
    public TextMeshProUGUI comboText;

    [Header("UI References - 数值面板")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI playerHpText; // 新增：玩家血量
    public TextMeshProUGUI curseText;    // 新增：诅咒层数
    public TextMeshProUGUI bossHpText;   // 新增：Boss血量
    public TextMeshProUGUI turnText;     // 新增：回合显示

    [Header("UI References - 爆牌反馈")]
    public CanvasGroup explosionCanvasGroup; // 用于控制FadeIn/Out
    public TextMeshProUGUI explosionText;    // 爆牌文字

    [Header("UI References - 结算面板")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitle;
    public Button restartButton;

    // --- 战斗状态数据 ---
    private List<RuntimeCard> levelDeck; // 本层初始牌库（用于记录诅咒污染后的状态）
    private List<RuntimeCard> drawPile;  // 当前回合的抽牌堆
    private List<RuntimeCard> handCards = new List<RuntimeCard>();

    private int currentAttack = 0;

    // --- 实体属性 ---
    private int playerHp = 20;           // 玩家初始血量
    private int curseCount = 0;          // 诅咒层数
    private int bossHp = 180;            // Boss初始血量
    private int currentTurn = 1;
    private const int MAX_TURNS = 3;

    void Start()
    {
        // 绑定按钮事件
        drawButton.onClick.AddListener(OnDeckClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        restartButton.onClick.AddListener(RestartGame);

        // 隐藏不需要的UI
        explosionCanvasGroup.alpha = 0;
        gameOverPanel.SetActive(false);

        // 初始化本层牌库，并开始第一回合
        levelDeck = deckManager.GenerateInitialDeck(12);
        StartNewTurn();
    }

    private void StartNewTurn()
    {
        // 清理上一回合的手牌UI
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }
        handCards.Clear();
        currentAttack = 0;
        comboText.text = "";

        // 从本层牌库复制一份作为本回合抽牌堆，并洗牌
        drawPile = new List<RuntimeCard>(levelDeck);
        ShuffleList(drawPile);

        // 恢复按钮交互
        drawButton.interactable = true;
        stopButton.interactable = true;

        UpdateAllUI();
    }

    private void OnDeckClicked()
    {
        if (handCards.Count >= 7 || drawPile.Count == 0) return;
        DrawCard();
    }

    private void DrawCard()
    {
        RuntimeCard drawnCard = drawPile[0];
        drawPile.RemoveAt(0);
        handCards.Add(drawnCard);

        int drawCount = handCards.Count;

        // 1. 爆牌判定 (第5张起)
        if (CheckExplosion(drawCount))
        {
            HandleExplosion();
            return; // 爆牌直接中断后续卡牌效果
        }

        // 2. 基础攻击力增加 (调用原有的 GetAttackValue)
        currentAttack += drawnCard.GetAttackValue();

        // 3. 【三色牌特殊效果判定】
        ApplyCardColorEffect(drawnCard);

        // 4. 生成 UI 并播放动画
        GameObject newCardObj = Instantiate(cardUIPrefab, handArea);
        CardUI cardUI = newCardObj.GetComponent<CardUI>();
        if (cardUI != null) cardUI.Init(drawnCard);

        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());
        StartCoroutine(cardUI.AnimateDraw(drawPileVisual.position));

        UpdateAllUI();

        // 检查玩家是否因为黄牌把自己扣死了
        if (playerHp <= 0) GameOver(false);
    }

    private void ApplyCardColorEffect(RuntimeCard card)
    {
        if (card.color == CardColor.Yellow)
        {
            // 黄牌：扣3点血
            playerHp -= 3;
        }
        else if (card.color == CardColor.Red)
        {
            // 红牌：扣3点血和诅咒+1
            playerHp -= 3;
            curseCount++;
            if (curseCount >= 4) TriggerCursePenalty();
        }
    }

    private void TriggerCursePenalty()
    {
        curseCount = 0; // 重置诅咒层数
        Debug.Log("<color=purple>诅咒爆发！本层所有蓝牌变为黄牌！</color>");

        // 把本回合牌堆和本层总牌库里的蓝牌全染成黄牌
        foreach (var c in drawPile) if (c.color == CardColor.Blue) c.color = CardColor.Yellow;
        foreach (var c in levelDeck) if (c.color == CardColor.Blue) c.color = CardColor.Yellow;
    }

    private bool CheckExplosion(int count)
    {
        float risk = 0f;
        if (count == 5) risk = 0.05f;
        else if (count == 6) risk = 0.15f;
        else if (count == 7) risk = 0.30f;

        return Random.value < risk;
    }

    private void HandleExplosion()
    {
        // 禁用抽牌
        drawButton.interactable = false;
        UpdateAllUI();

        // 播放爆牌反馈动画
        StartCoroutine(ShowExplosionFeedback());

        // 爆牌惩罚：死
        DelayedDefeat();
    }

    private IEnumerator ShowExplosionFeedback()
    {
        explosionText.text = "Explode!";
        // Fade in
        float t = 0;
        while (t < 0.3f) { t += Time.deltaTime; explosionCanvasGroup.alpha = t / 0.3f; yield return null; }
        // Stay
        yield return new WaitForSeconds(0.8f);
        // Fade out
        t = 0;
        while (t < 0.3f) { t += Time.deltaTime; explosionCanvasGroup.alpha = 1 - (t / 0.3f); yield return null; }
        explosionCanvasGroup.alpha = 0;
    }

    private void OnStopClicked()
    {
        drawButton.interactable = false;
        stopButton.interactable = false;

        // 1. 计算牌型加成
        PokerHandType finalHand;
        int handBonus = PokerHandEvaluator.GetHandBonus(handCards, out finalHand);

        int totalDamage = currentAttack + handBonus;

        // 2. UI 演出展示（如果有加成的话）
        if (finalHand != PokerHandType.HighCard)
        {
            string handName = PokerHandEvaluator.GetHandName(finalHand);
            comboText.text = $"<color=yellow>{handName}</color> +{handBonus}!";
            Debug.Log($"结算牌型：{handName}，获得额外伤害 {handBonus}");
        }
        else
        {
            comboText.text = ""; // 没有加成就不显示
        }

        // 3. 对Boss造成伤害 (使用总伤害)
        bossHp -= totalDamage;
        if (bossHp < 0) bossHp = 0;
        UpdateAllUI();

        // 4. 判定胜负或进入下一回合
        if (bossHp <= 0)
        {
            GameOver(true);
        }
        else
        {
            if (currentTurn >= MAX_TURNS)
            {
                GameOver(false);
            }
            else
            {
                currentTurn++;
                StartCoroutine(NextTurnRoutine());
            }
        }
    }

    private IEnumerator NextTurnRoutine()
    {
        yield return new WaitForSeconds(1.0f); // 停顿一秒让玩家看清伤害
        StartNewTurn();
    }
    private IEnumerator DelayedVictory()
    {
        yield return new WaitForSeconds(1.2f);
        GameOver(true);
    }

    private IEnumerator DelayedDefeat()
    {
        yield return new WaitForSeconds(1.2f);
        GameOver(false);
    }

    private void GameOver(bool isWin)
    {
        drawButton.interactable = false;
        stopButton.interactable = false;
        gameOverPanel.SetActive(true);
        gameOverTitle.text = isWin ? "<color=green>Victory!</color>" : "<color=red>Defeat!</color>";
    }

    private void RestartGame()
    {
        // 重新加载当前场景，最干净的重置方式
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateAllUI()
    {
        attackText.text = $"Damage: {currentAttack}";
        playerHpText.text = $"HP: {playerHp}/20";
        curseText.text = $"Curse: {curseCount}/4";
        bossHpText.text = $"Boss: {bossHp}/180";
        turnText.text = $"Turn: {currentTurn}/{MAX_TURNS}";
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }
}