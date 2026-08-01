using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;
    [SerializeField] private StrategyCardManager strategyCardManager;
    [SerializeField] private ChipsManager chipsManager;

    private List<StrategyCard> currentShopItems = new List<StrategyCard>();

    public void RefreshShopForNewStage()
    {
        GenerateNewShopItems(null);
    }

    public bool RefreshShop()
    {
        int cost = config != null ? config.refreshCost : 5;
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
        var allDefs = strategyCardManager.GetAllDefinitions();

        // 过滤：已满级的牌不出现在商店
        var available = allDefs
            .Where(d => IsCardUseful(d, owned))
            .Where(d => excludeNames == null || !excludeNames.Contains(d.cardName))
            .ToList();

        int slotCount = config != null ? config.shopSlotCount : 2;
        System.Random rng = new System.Random();
        int count = Mathf.Min(slotCount, available.Count);
        currentShopItems = available.OrderBy(_ => rng.Next()).Take(count).ToList();
    }

    /// <summary>
    /// 判断牌是否还有用（未拥有或可升级），满级牌不再出现
    /// </summary>
    private bool IsCardUseful(StrategyCard def, List<StrategyCard> owned)
    {
        var own = owned.Find(c => c.cardName == def.cardName);
        if (own == null) return true;
        return own.IsUpgradable;
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