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
        // 确保目录存在
        EnsureDirectoryExists("Assets/Configs");

        // 生成游戏配置
        GenerateGameConfig();

        // 生成策略牌数据库
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

        // 检查是否已存在
        if (AssetDatabase.LoadAssetAtPath<GameConfigSO>(assetPath) != null)
        {
            Debug.LogWarning($"GameConfig 已存在于 {assetPath}，跳过生成。如需重新生成请先删除。");
            return;
        }

        GameConfigSO config = ScriptableObject.CreateInstance<GameConfigSO>();

        // 设置默认值
        config.initialChips = 200;
        config.maxCorrectionsPerRound = 1;
        config.correctionCost = 20;
        config.shopSlotCount = 3;
        config.refreshCost = 20;
        config.maxCarryCards = 4;
        config.layer1ReturnCardCount = 3;
        config.layer2CorrectionWindowOffset = 3;

        config.errorTable = new GameConfigSO.ErrorEntry[]
        {
            new GameConfigSO.ErrorEntry { error = 0, early = 60, late = 60 },
            new GameConfigSO.ErrorEntry { error = 1, early = 30, late = 20 },
            new GameConfigSO.ErrorEntry { error = 2, early = -20, late = -40 },
            new GameConfigSO.ErrorEntry { error = 3, early = -40, late = -60 },
            new GameConfigSO.ErrorEntry { error = 4, early = -60, late = -80 },
            new GameConfigSO.ErrorEntry { error = 5, early = -80, late = -100 },
        };

        AssetDatabase.CreateAsset(config, assetPath);
        Debug.Log($"GameConfig 已生成：{assetPath}");
    }

    private static void GenerateStrategyCardDatabase()
    {
        string assetPath = "Assets/Configs/StrategyCardDatabase.asset";

        // 检查是否已存在
        if (AssetDatabase.LoadAssetAtPath<StrategyCardDataSO>(assetPath) != null)
        {
            Debug.LogWarning($"StrategyCardDatabase 已存在于 {assetPath}，跳过生成。如需重新生成请先删除。");
            return;
        }

        StrategyCardDataSO database = ScriptableObject.CreateInstance<StrategyCardDataSO>();

        database.cards = new StrategyCardData[]
        {
            // ============ 观察类 ============
            new StrategyCardData
            {
                cardName = "偷看顶牌",
                description = "查看牌堆顶部2张",
                price = 25,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                isObservable = true
            },
            new StrategyCardData
            {
                cardName = "偷看中间",
                description = "查看牌堆中间5张",
                price = 8,
                maxLevel = 2,
                upgradePrice = 7,
                isOncePerGame = false,
                isObservable = true
            },
            new StrategyCardData
            {
                cardName = "偷看底牌",
                description = "查看牌堆底部8张",
                price = 10,
                maxLevel = 2,
                upgradePrice = 8,
                isOncePerGame = false,
                isObservable = true
            },
            new StrategyCardData
            {
                cardName = "定点找牌",
                description = "查看2张指定点数牌的精确位置",
                price = 30,
                maxLevel = 2,
                upgradePrice = 25,
                isOncePerGame = false,
                isObservable = true
            },
            new StrategyCardData
            {
                cardName = "提前验货",
                description = "查看接下来5张能否凑齐目标",
                price = 16,
                maxLevel = 2,
                upgradePrice = 12,
                isOncePerGame = false,
                isObservable = true
            },

            // ============ 改牌类 ============
            new StrategyCardData
            {
                cardName = "删顶牌",
                description = "删除牌堆顶部5张",
                price = 35,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "底牌搬家",
                description = "从底部取5张插入顶部",
                price = 30,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "切牌",
                description = "对半分并交换位置",
                price = 25,
                maxLevel = 2,
                upgradePrice = 15,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "翻转发牌",
                description = "剩余牌堆顺序完全翻转",
                price = 25,
                maxLevel = 2,
                upgradePrice = 20,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "换花色",
                description = "指定两种花色全部替换（含颜色）",
                price = 35,
                maxLevel = 2,
                upgradePrice = 25,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "重洗牌堆",
                description = "彻底洗牌剩余牌堆",
                price = 40,
                maxLevel = 2,
                upgradePrice = 25,
                isOncePerGame = false,
                isObservable = false
            },

            // ============ 改目标类 ============
            new StrategyCardData
            {
                cardName = "双重目标",
                description = "同时追踪2个随机目标，完成任一即可达成（但不强制）",
                price = 20,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "换目标",
                description = "刷新为同难度层另一个随机牌型",
                price = 10,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },

            // ============ 改结算类 ============
            new StrategyCardData
            {
                cardName = "半赔半赚",
                description = "结算倍率×0.5",
                price = 15,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "双倍输赢",
                description = "结算倍率×2.0",
                price = 25,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "容错两次",
                description = "±2误差不扣不加",
                price = 40,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "亏损封顶",
                description = "本局损失上限-40",
                price = 40,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "免死一次",
                description = "致命伤害时筹码强制保留1点；购买后永不出现",
                price = 100,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = true,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "全押",
                description = "可在修正前激活；扣则归零，赚则×3；购买后永不出现",
                price = 60,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = true,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "保留猜测",
                description = "修正前猜测被保留，结算选误差更小的",
                price = 55,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },

            // ============ 改修正类 ============
            new StrategyCardData
            {
                cardName = "二次修正",
                description = "本局修正次数变为2次",
                price = 65,
                maxLevel = 1,
                upgradePrice = 0,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "打折修正",
                description = "修正消耗-10（即-10）",
                price = 20,
                maxLevel = 2,
                upgradePrice = 25,
                isOncePerGame = false,
                isObservable = false
            },
            new StrategyCardData
            {
                cardName = "超时修正",
                description = "翻过N后仍可修正，消耗-40",
                price = 18,
                maxLevel = 2,
                upgradePrice = 25,
                isOncePerGame = false,
                isObservable = false
            },
        };

        AssetDatabase.CreateAsset(database, assetPath);
        Debug.Log($"StrategyCardDatabase 已生成：{assetPath}，共 {database.cards.Length} 张策略牌");
    }
}
#endif