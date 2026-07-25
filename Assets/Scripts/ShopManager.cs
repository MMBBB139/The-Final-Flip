// ShopManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 商店系统 - 管理策略牌的展示、刷新、购买、升级
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("依赖组件")]
    [SerializeField] private StrategyCardManager strategyCardManager;
    [SerializeField] private ChipsManager chipsManager;

    [Header("商店参数")]
    [SerializeField] private int shopSlotCount = 3;
    [SerializeField] private int refreshCost = 20;

    private List<StrategyCard> currentShopItems;           // 当前展示的3张牌（来自定义库的引用）

    void Awake()
    {
        currentShopItems = new List<StrategyCard>();
    }

    /// <summary>
    /// 新关卡开始时刷新商店
    /// </summary>
    public void RefreshShopForNewStage()
    {
        GenerateNewShopItems(excludeNames: null);
    }

    /// <summary>
    /// 刷新商店（消耗20筹码），新商品与当前展示的不同
    /// </summary>
    public bool RefreshShop()
    {
        int currentChips = chipsManager != null ? chipsManager.GetChips() : 0;
        if (currentChips < refreshCost)
        {
            Debug.LogWarning($"[商店] 筹码不足！刷新需要{refreshCost}筹码，当前仅有{currentChips}筹码");
            return false;
        }

        chipsManager.AddChips(-refreshCost);

        // 排除当前展示的牌名
        var excludeNames = currentShopItems.Select(item => item.cardName).ToList();
        GenerateNewShopItems(excludeNames);
        Debug.Log($"[商店] 刷新完成，消耗{refreshCost}筹码");
        return true;
    }

    /// <summary>
    /// 生成3张新的商店商品
    /// </summary>
    /// <param name="excludeNames">需要排除的牌名列表（刷新时排除当前展示的）</param>
    private void GenerateNewShopItems(List<string> excludeNames)
    {
        currentShopItems.Clear();

        var ownedCards = strategyCardManager.GetOwnedCards();
        var allDefs = strategyCardManager.GetAllDefinitions();

        // 筛选可用牌：排除无用牌 + 排除指定名称
        List<StrategyCard> availableDefs = allDefs
            .Where(def => IsCardUseful(def, ownedCards))
            .Where(def => excludeNames == null || !excludeNames.Contains(def.cardName))
            .ToList();

        // 如果排除后不够3张，放宽限制（允许重复出现）
        if (availableDefs.Count < shopSlotCount)
        {
            Debug.Log($"[商店] 排除后可用牌仅{availableDefs.Count}种，放宽限制");
            availableDefs = allDefs
                .Where(def => IsCardUseful(def, ownedCards))
                .ToList();
        }

        // 随机选取3张
        System.Random rng = new System.Random();
        List<StrategyCard> shuffled = availableDefs.OrderBy(_ => rng.Next()).ToList();
        int count = Mathf.Min(shopSlotCount, shuffled.Count);
        currentShopItems = shuffled.Take(count).ToList();

        if (currentShopItems.Count < shopSlotCount)
            Debug.LogWarning($"[商店] 仅生成{currentShopItems.Count}张商品（可用牌不足）");
    }

    /// <summary>
    /// 判断一张牌是否"有用"：
    /// - 未拥有 → 有用
    /// - 已拥有且可升级 → 有用
    /// - 已拥有且满级 → 无用
    /// </summary>
    private bool IsCardUseful(StrategyCard definition, List<StrategyCard> ownedCards)
    {
        var owned = ownedCards.Find(c => c.cardName == definition.cardName);
        if (owned == null) return true;
        return owned.IsUpgradable;
    }

    /// <summary>
    /// 购买商店中的牌
    /// </summary>
    public bool BuyCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentShopItems.Count)
        {
            Debug.LogWarning($"[商店] 无效槽位: {slotIndex}");
            return false;
        }

        var item = currentShopItems[slotIndex];
        string cardName = item.cardName;

        var ownedCards = strategyCardManager.GetOwnedCards();
        var owned = ownedCards.Find(c => c.cardName == cardName);

        if (owned != null)
        {
            return UpgradeCardInShop(cardName);
        }
        else
        {
            return PurchaseNewCard(cardName, item.price);
        }
    }

    /// <summary>
    /// 购买新牌
    /// </summary>
    private bool PurchaseNewCard(string cardName, int price)
    {
        int currentChips = chipsManager != null ? chipsManager.GetChips() : 0;
        if (currentChips < price)
        {
            Debug.LogWarning($"[商店] 筹码不足！购买{cardName}需要{price}筹码，当前仅有{currentChips}");
            return false;
        }

        var ownedCards = strategyCardManager.GetOwnedCards();
        if (ownedCards.Count >= 4)
        {
            Debug.LogWarning($"[商店] 策略牌已满（最多4张），请先卖出");
            return false;
        }

        chipsManager.AddChips(-price);
        bool success = strategyCardManager.AddCard(cardName);

        if (success)
        {
            Debug.Log($"[商店] 购买成功: {cardName}，消耗{price}筹码");
            RemoveShopItem(cardName);
        }

        return success;
    }

    /// <summary>
    /// 升级已拥有的牌
    /// </summary>
    private bool UpgradeCardInShop(string cardName)
    {
        var ownedCards = strategyCardManager.GetOwnedCards();
        var owned = ownedCards.Find(c => c.cardName == cardName);

        if (owned == null || !owned.IsUpgradable)
        {
            Debug.LogWarning($"[商店] {cardName}无法升级");
            return false;
        }

        int upgradeCost = owned.GetUpgradeCost();
        int currentChips = chipsManager != null ? chipsManager.GetChips() : 0;
        if (currentChips < upgradeCost)
        {
            Debug.LogWarning($"[商店] 筹码不足！升级需要{upgradeCost}筹码");
            return false;
        }

        bool success = strategyCardManager.UpgradeCard(cardName);
        if (success)
        {
            ownedCards = strategyCardManager.GetOwnedCards();
            var updated = ownedCards.Find(c => c.cardName == cardName);
            if (updated != null && !updated.IsUpgradable)
            {
                RemoveShopItem(cardName);
                Debug.Log($"[商店] {cardName}已满级，从商店移除");
            }
        }

        return success;
    }

    /// <summary>
    /// 卖出已拥有的牌（回收50%筹码）
    /// </summary>
    public bool SellCard(string cardName)
    {
        var ownedCards = strategyCardManager.GetOwnedCards();
        var owned = ownedCards.Find(c => c.cardName == cardName);

        if (owned == null)
        {
            Debug.LogWarning($"[商店] 未拥有{cardName}，无法卖出");
            return false;
        }

        int sellPrice = owned.GetSellPrice();
        bool success = strategyCardManager.SellCard(cardName);

        if (success)
        {
            Debug.Log($"[商店] 卖出成功: {cardName}，回收{sellPrice}筹码");
        }

        return success;
    }

    /// <summary>
    /// 从当前展示中移除指定牌
    /// </summary>
    private void RemoveShopItem(string cardName)
    {
        var item = currentShopItems.Find(i => i.cardName == cardName);
        if (item != null)
        {
            currentShopItems.Remove(item);
        }
    }

    /// <summary>
    /// 获取当前商店商品列表
    /// </summary>
    public List<StrategyCard> GetCurrentShopItems()
    {
        return new List<StrategyCard>(currentShopItems);
    }

    /// <summary>
    /// 获取刷新费用
    /// </summary>
    public int GetRefreshCost() => refreshCost;
}