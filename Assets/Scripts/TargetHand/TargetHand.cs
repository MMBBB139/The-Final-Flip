using System;
using System.Collections.Generic;

[Serializable]
public class TargetHand
{
    public enum Tier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4
    }

    public string handName;
    public Tier tier;
    public string description;
    public Func<List<Card>, bool> checkCondition;

    public TargetHand(string name, Tier tier, string desc, Func<List<Card>, bool> condition)
    {
        handName = name;
        this.tier = tier;
        description = desc;
        checkCondition = condition;
    }
}