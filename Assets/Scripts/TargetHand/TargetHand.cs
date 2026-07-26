// TargetHand.cs
using System;
using System.Collections.Generic;

[Serializable]
public class TargetHand
{
    public enum Tier
    {
        Tier1, // 预期5-10张
        Tier2, // 预期8-18张
        Tier3, // 预期18-30张
        Tier4  // 预期30-48张
    }

    public string handName;        // 牌型名称
    public Tier tier;              // 所属层级
    public string description;     // 达成条件描述
    public Func<List<Card>, bool> checkCondition; // 检测函数

    public TargetHand(string name, Tier tier, string desc, Func<List<Card>, bool> condition)
    {
        handName = name;
        this.tier = tier;
        description = desc;
        checkCondition = condition;
    }
}