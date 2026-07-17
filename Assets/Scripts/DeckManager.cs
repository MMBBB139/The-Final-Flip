using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    [Header("Data Config")]
    public List<CardData> all52Cards; // 在Inspector中拖入52张基础牌的SO

    public List<RuntimeCard> GenerateInitialDeck(int totalCards = 12)
    {
        List<RuntimeCard> deck = new List<RuntimeCard>();

        // 1. 根据当前总牌数计算颜色比例
        int blueCount = Mathf.RoundToInt(totalCards * 0.50f);
        int yellowCount = Mathf.RoundToInt(totalCards * 0.33f);
        int redCount = totalCards - blueCount - yellowCount; // 剩余全给红色，保证总数严丝合缝

        // 2. 构建颜色池并打乱
        List<CardColor> colorPool = new List<CardColor>();
        for (int i = 0; i < blueCount; i++) colorPool.Add(CardColor.Blue);
        for (int i = 0; i < yellowCount; i++) colorPool.Add(CardColor.Yellow);
        for (int i = 0; i < redCount; i++) colorPool.Add(CardColor.Red);
        
        ShuffleList(colorPool);

        // 3. 从52张基础牌中随机抽取指定数量的牌
        // 此处复制一份全集，避免破坏原列表
        List<CardData> tempCardPool = new List<CardData>(all52Cards);
        ShuffleList(tempCardPool);

        // 4. 组装运行时牌堆
        for (int i = 0; i < totalCards; i++)
        {
            // 如果玩家修改导致牌堆上限超过52，这里需要加个越界保护或者重新洗入tempCardPool
            CardData selectedData = tempCardPool[i];
            CardColor assignedColor = colorPool[i];

            deck.Add(new RuntimeCard(selectedData, assignedColor));
        }

        return deck;
    }

    // 通用洗牌算法 (Fisher-Yates)
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}