using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    [SerializeField] private StrategyCardManager strategyCardManager;
    [SerializeField] private ChipsManager chipsManager;
    [SerializeField] private LevelManager levelManager;

    private List<StrategyCard> currentShopItems = new List<StrategyCard>();

    public void RefreshShopForNewStage()
    {
        GenerateNewShopItems(null);
    }

    public bool RefreshShop()
    {
        int cost = config != null ? config.refreshCost : 10;
        if (chipsManager.GetChips() < cost)
        {
            Debug.LogWarning($"刷新需要{cost}筹码");
            return false;
        }
        chipsManager.AddChips(-cost);
        GenerateNewShopItems(currentShopItems.Select(c => c.cardName).ToList());
        Debug.Log($"刷新商店，消耗{cost}");
        return true;
    }

    private void GenerateNewShopItems(List<string> excludeNames)
    {
        currentShopItems.Clear();
        var owned = strategyCardManager.GetOwnedCards();
        var layer = levelManager != null ? levelManager.GetCurrentStageInfo().layer : 1;

        // 按层级解锁牌池
        var available = strategyCardManager.GetDefinitionsByLayer(layer)
            .Where(d => !strategyCardManager.HasCard(d.cardName))
            .Where(d => excludeNames == null || !excludeNames.Contains(d.cardName))
            .ToList();

        int slotCount = config != null ? config.shopSlotCount : 2;
        System.Random rng = new System.Random();

        if (available.Count == 0)
        {
            Debug.LogWarning("无可用的策略牌");
            return;
        }

        // 刚进入新层级时，第一张必定是新解锁的牌
        if (levelManager != null && levelManager.IsJustEnteredNewLayer())
        {
            var newCards = strategyCardManager.GetNewDefinitionsForLayer(layer)
                .Where(d => !strategyCardManager.HasCard(d.cardName))
                .ToList();
            if (newCards.Count > 0)
            {
                var guaranteed = newCards[rng.Next(newCards.Count)];
                currentShopItems.Add(guaranteed);
                available.RemoveAll(d => d.cardName == guaranteed.cardName);
                slotCount--;
            }
        }

        int count = Mathf.Min(slotCount, available.Count);
        currentShopItems.AddRange(available.OrderBy(_ => rng.Next()).Take(count));
    }

    public bool BuyCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentShopItems.Count) return false;
        var item = currentShopItems[slotIndex];
        var owned = strategyCardManager.GetOwnedCards().Find(c => c.cardName == item.cardName);

        if (owned != null)
        {
            return UpgradeCard(item.cardName);
        }
        else
        {
            return PurchaseNewCard(item.cardName, item.price);
        }
    }

    public bool SellCard(string cardName)
    {
        return strategyCardManager.SellCard(cardName);
    }

    private bool PurchaseNewCard(string name, int price)
    {
        if (chipsManager.GetChips() < price) return false;
        int maxCarry = config != null ? config.maxCarryCards : 4;
        if (strategyCardManager.GetOwnedCards().Count >= maxCarry)
        {
            Debug.LogWarning("策略牌已满");
            return false;
        }
        chipsManager.AddChips(-price);
        if (strategyCardManager.AddCard(name))
        {
            RemoveShopItem(name);
            return true;
        }
        return false;
    }

    private bool UpgradeCard(string name)
    {
        var owned = strategyCardManager.GetOwnedCards().Find(c => c.cardName == name);
        if (owned == null || !owned.IsUpgradable) return false;
        int cost = owned.GetUpgradeCost();
        if (chipsManager.GetChips() < cost) return false;
        if (strategyCardManager.UpgradeCard(name))
        {
            var updated = strategyCardManager.GetOwnedCards().Find(c => c.cardName == name);
            if (updated != null && !updated.IsUpgradable)
                RemoveShopItem(name);
            return true;
        }
        return false;
    }

    private void RemoveShopItem(string name) => currentShopItems.RemoveAll(c => c.cardName == name);

    public List<StrategyCard> GetCurrentShopItems() => new List<StrategyCard>(currentShopItems);
}