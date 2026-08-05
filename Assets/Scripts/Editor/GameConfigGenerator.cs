#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class GameConfigGenerator : EditorWindow
{
    [MenuItem("Tools/地下赌场/生成所有配置文件")]
    public static void GenerateAllConfigs()
    {
        EnsureDirectoryExists("Assets/Configs");
        GenerateGameConfig();
        GenerateStrategyCardDatabase();
        AssetDatabase.Refresh();
        Debug.Log("所有配置文件生成完毕！");
        EditorUtility.DisplayDialog("生成完成", "配置已生成在 Assets/Configs/", "确定");
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
        if (AssetDatabase.LoadAssetAtPath<GameConfigSO>(assetPath) != null) return;

        GameConfigSO config = ScriptableObject.CreateInstance<GameConfigSO>();
        config.initialChips = 30;
        config.surviveReward = 10;
        config.maxCorrectionsPerRound = 1;
        config.correctionCost = 10;
        config.shopSlotCount = 2;
        config.refreshCost = 10;
        config.maxCarryCards = 4;
        config.totalLayers = 5;
        config.stagesPerLayer = 3;
        config.errorToleranceByLayer = new int[] { 5, 4, 3, 2, 0 };
        config.zeroErrorBonus = 20;
        config.oneErrorBonus = 5;
        config.layer2CorrectionWindowOffset = 3;
        config.layer3MaxDrawLimit = 25;
        config.layer4FadedBaseCost = 10;

        AssetDatabase.CreateAsset(config, assetPath);
    }

    private static void GenerateStrategyCardDatabase()
    {
        string assetPath = "Assets/Configs/StrategyCardDatabase.asset";
        if (AssetDatabase.LoadAssetAtPath<StrategyCardDataSO>(assetPath) != null) return;

        StrategyCardDataSO database = ScriptableObject.CreateInstance<StrategyCardDataSO>();
        database.cards = new StrategyCardData[]
        {
            // ===== 第一层 =====
            new StrategyCardData { cardName = "探顶", description = "看牌堆顶部3张\n升级：看顶部5张，可选1张沉底", price = 10, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 1, category = "信息" },
            new StrategyCardData { cardName = "探底", description = "看牌堆底部3张，可选1张置顶\n升级：看底部5张，可选2张置顶", price = 6, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 1, category = "信息" },
            new StrategyCardData { cardName = "探牌", description = "随机显示3张未翻牌\n升级：随机显示5张", price = 8, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 1, category = "信息" },
            new StrategyCardData { cardName = "宽限", description = "修正窗口延长2张\n升级：延长4张", price = 6, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 1, category = "改修正" },
            new StrategyCardData { cardName = "再修一次", description = "每局修正次数+1\n升级：修正次数+2", price = 12, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 1, category = "改修正" },
            new StrategyCardData { cardName = "早鸟优惠", description = "翻牌≤10达成，额外+12\n升级：额外+20", price = 10, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 1, category = "改规则" },
            new StrategyCardData { cardName = "近误差红利", description = "误差=1时额外+8\n升级：额外+15", price = 10, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 1, category = "改规则" },
            new StrategyCardData { cardName = "偏差大师", description = "误差≥3且存活，额外+15。永久累计+5/次\n升级：额外+25，累计+8/次", price = 16, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 1, category = "成长型" },

            // ===== 第二层 =====
            new StrategyCardData { cardName = "点数搜索", description = "指定点数，随机显示2张\n升级：显示所有该点数未翻牌", price = 16, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "信息" },
            new StrategyCardData { cardName = "花色搜索", description = "指定花色，随机显示2张\n升级：显示所有该花色未翻牌", price = 16, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "信息" },
            new StrategyCardData { cardName = "沉底", description = "顶部2张沉底\n升级：顶部3张任意选择沉底哪些", price = 5, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改牌-移动" },
            new StrategyCardData { cardName = "交换", description = "顶部2张和底部2张互换\n升级：顶部3张和底部3张互换", price = 10, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改牌-移动" },
            new StrategyCardData { cardName = "洗牌", description = "剩余牌堆重新洗牌\n升级：洗牌后看顶部2张", price = 8, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改牌-移动" },
            new StrategyCardData { cardName = "预览", description = "每局下注前自动翻开顶部3张，若直接中目标则0误差获胜\n升级：翻5张", price = 14, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 2, category = "改牌-翻牌操控" },
            new StrategyCardData { cardName = "快进", description = "连续翻3张，选1张保留，其余沉底\n升级：翻5张选1张", price = 8, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改牌-翻牌操控" },
            new StrategyCardData { cardName = "删除", description = "删除1张已翻牌，不改变翻牌计数\n升级：删除最多2张", price = 10, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改牌-删复" },
            new StrategyCardData { cardName = "复制", description = "复制1张已翻牌，洗入剩余牌堆\n升级：复制1张并直接置顶", price = 14, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改牌-删复" },
            new StrategyCardData { cardName = "换目标", description = "替换为同层随机2个目标之一，自选\n升级：替换为3个目标之一", price = 14, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 2, category = "改目标" },
            new StrategyCardData { cardName = "零误差红利", description = "误差=0额外+20\n升级：额外+35", price = 18, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 2, category = "改规则" },
            new StrategyCardData { cardName = "宽容", description = "本层误差容忍度+1\n升级：容忍度+2", price = 24, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 2, category = "改规则" },
            new StrategyCardData { cardName = "稳扎稳打", description = "误差≤2，额外+8。连续触发每局叠加+4，中断重置\n升级：额外+12，叠加+6", price = 20, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 2, category = "成长型" },
            new StrategyCardData { cardName = "修正艺术家", description = "使用修正且误差=0，额外+20。永久累计+5/次\n升级：额外+30，累计+8/次", price = 22, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 2, category = "成长型" },
            new StrategyCardData { cardName = "速攻", description = "15张内达成且误差≤1，额外+20。永久累计+5/次\n升级：额外+30，累计+8/次", price = 22, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 2, category = "成长型" },

            // ===== 第三层 =====
            new StrategyCardData { cardName = "回收", description = "将1张已翻牌洗回剩余牌堆，翻牌数-1\n升级：洗回最多2张，翻牌数-2", price = 8, maxLevel = 2, type = StrategyCardData.CardType.主动, unlockLayer = 3, category = "改牌-删复" },
            new StrategyCardData { cardName = "修正促销", description = "修正消耗降为0\n升级：消耗为0且使用后额外+3筹码", price = 10, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 3, category = "改修正" },
            new StrategyCardData { cardName = "消除特殊", description = "一次性，消除当前全局特殊规则", price = 25, maxLevel = 1, isOncePerGame = true, type = StrategyCardData.CardType.一次性, unlockLayer = 3, category = "改规则" },
            new StrategyCardData { cardName = "孤注一掷", description = "一次性，容忍度=0，误差=0时收入x5", price = 60, maxLevel = 1, isOncePerGame = true, type = StrategyCardData.CardType.一次性, unlockLayer = 3, category = "改规则" },
            new StrategyCardData { cardName = "完美风暴", description = "误差=0时收入x1.5\n升级：收入x2", price = 50, maxLevel = 2, type = StrategyCardData.CardType.被动, unlockLayer = 3, category = "成长型" },

            // ===== 第四层 =====
            new StrategyCardData { cardName = "不死鸟", description = "一次性，失败改为存活，无奖励", price = 150, maxLevel = 1, isOncePerGame = true, type = StrategyCardData.CardType.一次性, unlockLayer = 4, category = "传说" },
            new StrategyCardData { cardName = "十次修正", description = "修正免费、可用10次、且过了猜数也能修正", price = 250, maxLevel = 1, type = StrategyCardData.CardType.被动, unlockLayer = 4, category = "传说" },
            new StrategyCardData { cardName = "命运之轮", description = "本局误差容忍度+15", price = 300, maxLevel = 1, type = StrategyCardData.CardType.被动, unlockLayer = 4, category = "传说" },
            new StrategyCardData { cardName = "天启", description = "每局下注前自动看顶部44张", price = 400, maxLevel = 1, type = StrategyCardData.CardType.被动, unlockLayer = 4, category = "传说" },
        };

        AssetDatabase.CreateAsset(database, assetPath);
        Debug.Log($"StrategyCardDatabase 已生成，共 {database.cards.Length} 张");
    }
}
#endif