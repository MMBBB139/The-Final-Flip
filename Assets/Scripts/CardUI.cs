using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI suitText;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // 初始化卡牌数据
    public void Init(RuntimeCard cardData)
    {
        // 1. 设置底色（蓝、黄、红）
        // 暂时是改text颜色
        Color color = Color.white;
        switch (cardData.color)
        {
            case CardColor.Blue: color = Color.blue; break;
            case CardColor.Yellow: color = Color.yellow; break;
            case CardColor.Red: color = Color.red; break;
        }
        rankText.color = color;
        suitText.color = color;

        // 2. 设置点数文字
        string rankStr = cardData.baseData.rank.ToString();
        if (cardData.baseData.rank == 11) rankStr = "J";
        else if (cardData.baseData.rank == 12) rankStr = "Q";
        else if (cardData.baseData.rank == 13) rankStr = "K";
        else if (cardData.baseData.rank == 14) rankStr = "A";
        rankText.text = rankStr;

        // 3. 设置花色符号
        suitText.text = GetSuitSymbol(cardData.baseData.suit);
    }

    // 从牌堆飞入手牌区的动画
    public IEnumerator AnimateDraw(Vector3 startScreenPos, float duration = 0.3f)
    {
        // 此时卡牌已经是 HandArea 的子物体，布局组件已经计算好了它的最终目标位置
        Vector3 targetPos = rectTransform.position;

        // 强行把它瞬间移到牌堆的位置
        rectTransform.position = startScreenPos;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 使用 SmoothStep 让运动曲线更平滑
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            rectTransform.position = Vector3.Lerp(startScreenPos, targetPos, t);
            yield return null;
        }

        rectTransform.position = targetPos;
    }

    private string GetSuitSymbol(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Spades: return "♠";
            case CardSuit.Hearts: return "♥";
            case CardSuit.Clubs: return "♣";
            case CardSuit.Diamonds: return "♦";
            default: return "";
        }
    }
}