using System.Collections;
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

    public void UpdateAllUI(BattleData data)
    {
        attackText.text = $"Damage: {data.currentAttack}";
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
        explosionText.text = "Explode!";
        float t = 0;
        while (t < 0.3f) { t += Time.deltaTime; explosionCanvasGroup.alpha = t / 0.3f; yield return null; }
        yield return new WaitForSeconds(0.8f);
        t = 0;
        while (t < 0.3f) { t += Time.deltaTime; explosionCanvasGroup.alpha = 1 - t / 0.3f; yield return null; }
        explosionCanvasGroup.alpha = 0;
    }
}