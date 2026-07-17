using UnityEngine;
using UnityEditor;
using System.IO;

public class CardDataGenerator
{
    // 在Unity顶部菜单栏添加一个自定义菜单项
    [MenuItem("Tools/Poker Roguelike/生成52张扑克牌数据")]
    public static void GenerateCards()
    {
        // 定义SO存放的路径
        string folderPath = "Assets/ScriptableObjects/Cards";

        // 如果文件夹不存在，自动创建
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        int count = 0;

        // 遍历4种花色
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            // 遍历点数 2 到 14
            for (int rank = 2; rank <= 14; rank++)
            {
                // 生成规范的文件名，例如：Spades_02.asset, Hearts_14.asset
                // ToString("D2") 能让 2 变成 02，排序时更好看
                string fileName = $"{suit}_{rank.ToString("D2")}.asset";
                string assetPath = $"{folderPath}/{fileName}";

                // 检查是否已经存在该文件，防止误操作覆盖
                if (AssetDatabase.LoadAssetAtPath<CardData>(assetPath) == null)
                {
                    // 在内存中创建SO实例
                    CardData card = ScriptableObject.CreateInstance<CardData>();
                    card.suit = suit;
                    card.rank = rank;

                    // 将内存中的SO保存为实体资源文件
                    AssetDatabase.CreateAsset(card, assetPath);
                    count++;
                }
            }
        }

        // 强制保存并刷新资源数据库，让新文件立刻在Project窗口中显示出来
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"生成完毕！成功创建了 {count} 张新牌。路径：{folderPath}");
    }
}