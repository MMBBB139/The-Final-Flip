using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    private Dictionary<int, List<StrategyCard>> cardPool;
    public List<StrategyCard> currentShop { get; private set; }
    private List<StrategyCard> ownedCards;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildCardPool();
    }

    private void BuildCardPool()
    {
        cardPool = new Dictionary<int, List<StrategyCard>>();

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(StrategyCard)));

            foreach (var type in types)
            {
                var card = (StrategyCard)System.Activator.CreateInstance(type);

                if (!cardPool.ContainsKey(card.stage))
                    cardPool[card.stage] = new List<StrategyCard>();

                cardPool[card.stage].Add(card);
            }
        }
    }

    public void OpenShop(List<StrategyCard> owned, int currentStage)
    {
        ownedCards = owned;
        currentShop = new List<StrategyCard>();

        StrategyCard first = GetFirstCard(currentStage);
        if (first != null) currentShop.Add(first);

        StrategyCard second = GetRandomCard(currentStage);
        if (second != null) currentShop.Add(second);
    }

    private StrategyCard GetFirstCard(int stage)
    {
        if (!cardPool.ContainsKey(stage)) return null;

        List<StrategyCard> newCards = cardPool[stage]
            .Where(c => !IsOwned(c.cardName) && !IsGoneForever(c))
            .ToList();

        if (newCards.Count == 0)
            return GetRandomCard(stage);

        return newCards[Random.Range(0, newCards.Count)];
    }

    private StrategyCard GetRandomCard(int stage)
    {
        List<StrategyCard> available = new List<StrategyCard>();
        for (int i = 1; i <= stage; i++)
        {
            if (!cardPool.ContainsKey(i)) continue;

            foreach (var card in cardPool[i])
            {
                if (!IsOwned(card.cardName) && !IsGoneForever(card) && !currentShop.Contains(card))
                    available.Add(card);
            }
        }

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    public bool RefreshShop()
    {
        if (GameManager.Instance.chips < 8) return false;

        GameManager.Instance.chips -= 8;

        int stage = StageManager.Instance.currentStage;
        StrategyCard newCard;
        int attempts = 0;
        do
        {
            newCard = GetRandomCard(stage);
            attempts++;
        }
        while (newCard != null && currentShop.Contains(newCard) && attempts < 100);

        if (newCard == null) return false;

        int index = Random.Range(0, currentShop.Count);
        currentShop[index] = newCard;
        return true;
    }

    public bool BuyCard(int shopIndex)
    {
        if (shopIndex < 0 || shopIndex >= currentShop.Count) return false;

        StrategyCard card = currentShop[shopIndex];
        if (card == null) return false;

        int price = card.buyPrice;
        if (GameManager.Instance.chips < price) return false;

        GameManager.Instance.chips -= price;
        ownedCards.Add(card);
        currentShop[shopIndex] = null;
        return true;
    }

    public bool UpgradeCard(StrategyCard card)
    {
        if (!card.canUpgrade || card.isUpgraded) return false;

        int price = card.upgradePrice;
        if (GameManager.Instance.chips < price) return false;

        GameManager.Instance.chips -= price;
        card.isUpgraded = true;
        return true;
    }

    public void SellCard(StrategyCard card)
    {
        int refund = (card.buyPrice + (card.isUpgraded ? card.upgradePrice : 0)) / 2;
        GameManager.Instance.chips += refund;
        ownedCards.Remove(card);
        card.OnRemove();
    }

    private bool IsOwned(string name)
    {
        return ownedCards.Any(c => c.cardName == name);
    }

    private bool IsGoneForever(StrategyCard card)
    {
        if (card is SC_Phoenix phoenix)
            return phoenix.isGoneForever;
        return false;
    }
}