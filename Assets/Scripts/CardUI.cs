using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public TextMeshProUGUI rankText;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // 初始化卡牌数据
    public void Init(RuntimeCard cardData)
    {
        // 设置颜色
        Color color = Color.white;
        switch (cardData.color)
        {
            case CardColor.Blue: color = Color.blue; break;
            case CardColor.Yellow: color = Color.yellow; break;
            case CardColor.Red: color = Color.red; break;
        }
        rankText.color = color;

        // 设置点数文字
        rankText.text = cardData.rank.ToString();
    }

    // 从牌堆飞入手牌区的动画
    public IEnumerator AnimateDraw(Vector3 startScreenPos, float duration = 0.3f)
    {
        Vector3 targetPos = rectTransform.position;
        rectTransform.position = startScreenPos;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            rectTransform.position = Vector3.Lerp(startScreenPos, targetPos, t);
            yield return null;
        }

        rectTransform.position = targetPos;
    }
}