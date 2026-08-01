#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键生成游戏所需的所有 ScriptableObject 配置文件
/// 菜单路径：Tools > 地下赌场 > 生成所有配置文件
/// </summary>
public class GameConfigGenerator : EditorWindow
{
    [MenuItem("Tools/地下赌场/生成所有配置文件")]
    public static void GenerateAllConfigs()
    {
        EnsureDirectoryExists("Assets/Configs");

        GenerateGameConfig();
        GenerateStrategyCardDatabase();

        AssetDatabase.Refresh();

        Debug.Log("所有配置文件生成完毕！路径：Assets/Configs/");
        EditorUtility.DisplayDialog("生成完成", "GameConfigSO 和 StrategyCardDataSO 已生成在 Assets/Configs/ 目录下", "确定");
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string folder = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void GenerateGameConfig()
    {
        string assetPath = "Assets/Configs/GameConfig.asset";

        if (AssetDatabase.LoadAssetAtPath<GameConfigSO>(assetPath) != null)
        {
            Debug.LogWarning($"GameConfig 已存在于 {assetPath}，跳过生成。如需重新生成请先删除。");
            return;
        }

        GameConfigSO config = ScriptableObject.CreateInstance<GameConfigSO>();

        config.initialChips = 30;
        config.surviveReward = 20;
        config.maxCorrectionsPerRound = 1;
        config.correctionCost = 10;
        config.shopSlotCount = 2;
        config.refreshCost = 5;
        config.maxCarryCards = 4;
        config.errorToleranceByLayer = new int[] { 4, 3, 2, 1 };
        config.layer1ReturnCardCount = 3;
        config.layer2CorrectionWindowOffset = 3;
        config.layer3MaxDrawLimit = 25;
        config.layer4RevealFadedCost = 20;

        AssetDatabase.CreateAsset(config, assetPath);
        Debug.Log($"GameConfig 已生成：{assetPath}");
    }

    private static void GenerateStrategyCardDatabase()
    {
        string assetPath = "Assets/Configs/StrategyCardDatabase.asset";

        if (AssetDatabase.LoadAssetAtPath<StrategyCardDataSO>(assetPath) != null)
        {
            Debug.LogWarning($"StrategyCardDatabase 已存在于 {assetPath}，跳过生成。如需重新生成请先删除。");
            return;
        }

        StrategyCardDataSO database = ScriptableObject.CreateInstance<StrategyCardDataSO>();

        database.cards = new StrategyCardData[]
        {
            // ============ 信息-看牌 ============
            new StrategyCardData
            {
                cardName = "探顶",
                description = "看牌堆顶部3张\n升级：看顶部5张，可选其中1张沉底",
                price = 6,
                maxLevel = 2,
                upgradePrice = 10,
                isOncePerGame = false,
                category = "信息"
            },
            new StrategyCardData
            {
                cardName = "探底",
                description = "看牌堆底部3张\n升级：看底部5张，可选其中1张置顶",
                price = 6,
                maxLevel = 2,
                upgradePrice = 10,
                isOncePerGame = false,
                category = "信息"
            },
            new StrategyCardData
            {
                cardName = "探牌",
                description = "随机显示牌堆中3张未翻过的牌\n升级：随机显示5张",
                price = 8,
                maxLevel = 2,
                upgradePrice = 14,
                isOncePerGame = false,
                category = "信息"
            },
            new StrategyCardData
            {
                cardName = "点数搜索",
                description = "指定1个点数，随机显示牌堆中两张该点数的未翻牌\n升级：显示牌堆中所有该点数的未翻牌",
                price = 16,
                maxLevel = 2,
                upgradePrice = 24,
                isOncePerGame = false,
                category = "信息"
            },
            new StrategyCardData
            {
                cardName = "花色搜索",
                description = "指定1个花色，随机显示牌堆中两张该花色的未翻牌\n升级：显示牌堆中所有该花色的未翻牌",
                price = 16,
                maxLevel = 2,
                upgradePrice = 24,
                isOncePerGame = false,
                category = "信息"
            },

            // ============ 信息-目标检测 ============
            new StrategyCardData
            {
                cardName = "先知",
                description = "接下来5张是否达成目标牌型？（只答是/否）\n升级：接下来8张是否达成？若\"是\"，告知第几张首次达成",
                price = 8,
                maxLevel = 2,
                upgradePrice = 18,
                isOncePerGame = false,
                category = "信息"
            },

            // ============ 改牌-移动 ============
            new StrategyCardData
            {
                cardName = "沉底",
                description = "顶部2张沉底\n升级：顶部3张牌任意选择沉底哪些",
                price = 5,
                maxLevel = 2,
                upgradePrice = 9,
                isOncePerGame = false,
                category = "改牌-移动"
            },
            new StrategyCardData
            {
                cardName = "置顶",
                description = "底部2张置顶\n升级：底部3张任意选择置顶哪些",
                price = 6,
                maxLevel = 2,
                upgradePrice = 10,
                isOncePerGame = false,
                category = "改牌-移动"
            },

            // ============ 改牌-删复 ============
            new StrategyCardData
            {
                cardName = "删除",
                description = "删除1张已翻牌，不改变翻牌计数\n升级：删除最多2张已翻牌",
                price = 10,
                maxLevel = 2,
                upgradePrice = 18,
                isOncePerGame = false,
                category = "改牌-删复"
            },
            new StrategyCardData
            {
                cardName = "复制",
                description = "复制1张已翻牌，洗入剩余牌堆\n升级：复制1张已翻牌并直接置顶",
                price = 14,
                maxLevel = 2,
                upgradePrice = 22,
                isOncePerGame = false,
                category = "改牌-删复"
            },

            // ============ 改修正 ============
            new StrategyCardData
            {
                cardName = "宽限",
                description = "修正窗口延长2张\n升级：修正窗口延长4张",
                price = 6,
                maxLevel = 2,
                upgradePrice = 12,
                isOncePerGame = false,
                category = "改修正"
            },
            new StrategyCardData
            {
                cardName = "再修一次",
                description = "本局额外1次修正机会\n升级：本局额外2次修正机会",
                price = 12,
                maxLevel = 2,
                upgradePrice = 22,
                isOncePerGame = false,
                category = "改修正"
            },
            new StrategyCardData
            {
                cardName = "修正促销",
                description = "本局修正只消耗5筹码\n升级：本局修正不消耗筹码",
                price = 10,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                category = "改修正"
            },

            // ============ 改规则 ============
            new StrategyCardData
            {
                cardName = "近误差红利",
                description = "本局误差为1时额外获得15筹码\n升级：本局误差≤1时额外获得25筹码",
                price = 10,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                category = "改规则"
            },
            new StrategyCardData
            {
                cardName = "早鸟优惠",
                description = "本局翻牌数≤10时达成目标，额外获得18筹码\n升级：翻牌数≤14时达成目标，额外获得24筹码",
                price = 10,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                category = "改规则"
            },
            new StrategyCardData
            {
                cardName = "换目标",
                description = "目标替换为同层级随机2个目标中的1个（自选）\n升级：替换为同层级随机3个目标中的1个",
                price = 14,
                maxLevel = 2,
                upgradePrice = 22,
                isOncePerGame = false,
                category = "改规则"
            },
            new StrategyCardData
            {
                cardName = "零误差红利",
                description = "本局误差为0时额外获得30筹码\n升级：本局误差为0时额外获得50筹码",
                price = 18,
                maxLevel = 2,
                upgradePrice = 30,
                isOncePerGame = false,
                category = "改规则"
            },
            new StrategyCardData
            {
                cardName = "绝处逢生",
                description = "一次性，本局失败改为视为存活，无筹码奖励",
                price = 28,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = true,
                category = "改规则"
            },
            new StrategyCardData
            {
                cardName = "消除特殊",
                description = "一次性，消除当前特殊规则关的特殊规则",
                price = 25,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = true,
                category = "改规则"
            },
            new StrategyCardData
            {
                cardName = "宽容+",
                description = "本层误差容忍度+1\n升级：本层误差容忍度+2",
                price = 24,
                maxLevel = 2,
                upgradePrice = 44,
                isOncePerGame = false,
                category = "改规则"
            },
        };

        AssetDatabase.CreateAsset(database, assetPath);
        Debug.Log($"StrategyCardDatabase 已生成：{assetPath}，共 {database.cards.Length} 张策略牌");
    }
}
#endif