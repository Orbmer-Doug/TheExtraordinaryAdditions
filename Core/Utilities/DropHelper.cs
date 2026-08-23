using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace TheExtraordinaryAdditions.Core.Utilities;

#region Fraction Struct (thanks Yorai)

public readonly struct Fraction(int n, int d)
{
    internal readonly int Numerator = n < 0 ? 0 : n;
    internal readonly int Denominator = d <= 0 ? 1 : d;

    public static implicit operator float(Fraction f) => f.Numerator / (float) f.Denominator;
}

#endregion

#region Weighted Item Stack Struct

public readonly struct WeightedItemStack
{
    public const float DefaultWeight = 1f;

    internal readonly int ItemID;
    internal readonly float Weight;
    internal readonly int MinQuantity;
    internal readonly int MaxQuantity;

    internal WeightedItemStack(int id, float w)
    {
        ItemID = id;
        Weight = w;
        MinQuantity = 1;
        MaxQuantity = 1;
    }

    internal WeightedItemStack(int id, float w, int quantity)
    {
        ItemID = id;
        Weight = w;
        MinQuantity = MaxQuantity = quantity;
    }

    internal WeightedItemStack(int id, float w, int min, int max)
    {
        ItemID = id;
        Weight = w;
        MinQuantity = min;
        MaxQuantity = max;
    }

    internal WeightedItemStack(int id)
    {
        ItemID = id;
        Weight = DefaultWeight;
        MinQuantity = MaxQuantity = 1;
    }

    internal int ChooseQuantity(UnifiedRandom rng) => rng.Next(MinQuantity, MaxQuantity + 1);
}

#endregion

public static class DropHelper
{
    #region Block Drops

    /// <summary>
    /// Adds the specified items to TML's blockLoot list. Items on the list cannot spawn in the world via any means.<br />
    /// <b>You should only use this function in the following places:</b><br />
    /// - ModNPC.PreKill and GlobalNPC.PreKill<br />
    /// - ModNPC.OnKill and GlobalNPC.OnKill<br /><br />
    /// This function is intended to block items from dropping from NPCs based on temporary conditions<br />
    /// If you want to permanently remove a drop from an NPC, this is not the function you want.<br />
    /// In those cases, use GlobalNPC.ModifyLoot<br />
    /// This will ensure that the drops are removed from the bestiary as well.
    /// </summary>
    /// <param name="itemIDs">The item IDs to prevent from spawning.</param>
    public static void BlockDrops(params int[] itemIDs)
    {
        foreach (int itemID in itemIDs)
            NPCLoader.blockLoot.Add(itemID);
    }

    #endregion

    #region Recursive Drop Rate Mutator

    private static int RecursivelyMutateDropRate(this IItemDropRule rule, int itemID, int newNumerator,
        int newDenominator)
    {
        switch (rule)
        {
            case CommonDrop drop when drop.itemId == itemID:
                drop.chanceNumerator = newNumerator;
                drop.chanceDenominator = newDenominator;
                return 1;
            case ItemDropWithConditionRule conditionalDrop when conditionalDrop.itemId == itemID:
                conditionalDrop.chanceNumerator = newNumerator;
                conditionalDrop.chanceDenominator = newDenominator;
                return 1;
            case DropBasedOnExpertMode expertDrop:
            {
                int normalChanges =
                    expertDrop.ruleForNormalMode.RecursivelyMutateDropRate(itemID, newNumerator, newDenominator);
                int expertChanges =
                    expertDrop.ruleForExpertMode.RecursivelyMutateDropRate(itemID, newNumerator, newDenominator);
                return normalChanges + expertChanges;
            }
            case DropBasedOnMasterMode masterDrop:
            {
                int defaultChanges =
                    masterDrop.ruleForDefault.RecursivelyMutateDropRate(itemID, newNumerator, newDenominator);
                int masterChanges =
                    masterDrop.ruleForMasterMode.RecursivelyMutateDropRate(itemID, newNumerator, newDenominator);
                return defaultChanges + masterChanges;
            }
            default:
                return 0;
        }
    }

    #endregion

    #region Leading Condition Rule Extensions

    /// <param name="mainRule">The LeadingConditionRule which should have another drop rule registered as one of its chains</param>
    extension(LeadingConditionRule mainRule)
    {
        /// <summary>
        /// Adds any given drop rule as a chained rule to the given LeadingConditionRule
        /// </summary>
        /// <param name="chainedRule">The drop rule which should occur given this leading condition</param>
        /// <param name="hideLootReport">If this should be hidden in the bestiary</param>
        public IItemDropRule Add(IItemDropRule chainedRule,
            bool hideLootReport = false)
        {
            return mainRule.OnSuccess(chainedRule, hideLootReport);
        }

        /// <summary>
        /// Shorthand to add a simple drop to the given LeadingConditionRule
        /// </summary>
        public IItemDropRule Add(int itemID, int dropRateInt = 1,
            int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
        {
            return mainRule.OnSuccess(ItemDropRule.Common(itemID, dropRateInt, minQuantity, maxQuantity),
                hideLootReport);
        }

        /// <summary>
        /// Shorthand to add a simple drop to the given LeadingConditionRule
        /// </summary>
        public IItemDropRule Add(int itemID, Fraction dropRate,
            int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
        {
            return mainRule.OnSuccess(
                new CommonDrop(itemID, dropRate.Denominator, minQuantity, maxQuantity, dropRate.Numerator),
                hideLootReport);
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule
        /// </summary>
        /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        public IItemDropRule AddIf(Func<bool> lambda, int itemID,
            int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false,
            string desc = null)
        {
            return mainRule.OnSuccess(
                ItemDropRule.ByCondition(If(lambda, true, desc), itemID, dropRateInt, minQuantity, maxQuantity),
                hideLootReport);
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule
        /// </summary>
        /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        public IItemDropRule AddIf(Func<bool> lambda, int itemID,
            Fraction dropRate, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false,
            string desc = null)
        {
            return mainRule.OnSuccess(
                ItemDropRule.ByCondition(If(lambda, true, desc), itemID, dropRate.Denominator, minQuantity, maxQuantity,
                    dropRate.Numerator), hideLootReport);
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule
        /// </summary>
        /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        public IItemDropRule AddIf(Func<DropAttemptInfo, bool> lambda,
            int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false,
            string desc = null)
        {
            return mainRule.OnSuccess(
                ItemDropRule.ByCondition(If(lambda, true, desc), itemID, dropRateInt, minQuantity, maxQuantity),
                hideLootReport);
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule
        /// </summary>
        /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        public IItemDropRule AddIf(Func<DropAttemptInfo, bool> lambda,
            int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false,
            string desc = null)
        {
            return mainRule.OnSuccess(
                ItemDropRule.ByCondition(If(lambda, true, desc), itemID, dropRate.Denominator, minQuantity, maxQuantity,
                    dropRate.Numerator), hideLootReport);
        }

        /// <summary>
        /// Adds any given drop rule as a chained rule to the given LeadingConditionRule
        /// </summary>
        public IItemDropRule AddFail(IItemDropRule chainedRule,
            bool hideLootReport = false)
        {
            return mainRule.OnFailedConditions(chainedRule, hideLootReport);
        }

        /// <summary>
        /// Shorthand to add a simple drop to the given LeadingConditionRule
        /// </summary>
        public IItemDropRule AddFail(int itemID, int dropRateInt = 1,
            int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
        {
            return mainRule.OnFailedConditions(ItemDropRule.Common(itemID, dropRateInt, minQuantity, maxQuantity),
                hideLootReport);
        }

        /// <summary>
        /// Shorthand to add a simple drop to the given LeadingConditionRule using a Fraction drop rate
        /// </summary>
        public IItemDropRule AddFail(int itemID, Fraction dropRate,
            int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
        {
            return mainRule.OnFailedConditions(
                new CommonDrop(itemID, dropRate.Denominator, minQuantity, maxQuantity, dropRate.Numerator),
                hideLootReport);
        }
    }

    #endregion

    #region ILoot Extensions

    /// <param name="loot">The ILoot interface for the loot table</param>
    extension(ILoot loot)
    {
        /// <summary>
        /// Shorthand to add a simple drop to a loot table
        /// </summary>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule Add(int itemID, int dropRateInt = 1, int minQuantity = 1,
            int maxQuantity = 1)
        {
            return loot.Add(ItemDropRule.Common(itemID, dropRateInt, minQuantity, maxQuantity));
        }

        /// <summary>
        /// Shorthand to add a simple drop to a loot table
        /// </summary>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule Add(int itemID, Fraction dropRate, int minQuantity = 1,
            int maxQuantity = 1)
        {
            return loot.Add(new CommonDrop(itemID, dropRate.Denominator, minQuantity, maxQuantity, dropRate.Numerator));
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to a loot table
        /// </summary>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule AddIf(IItemDropRuleCondition cond, int itemID, int dropRateInt = 1,
            int minQuantity = 1, int maxQuantity = 1)
        {
            return loot.Add(ItemDropRule.ByCondition(cond, itemID, dropRateInt, minQuantity, maxQuantity));
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to a loot table using a Fraction drop rate
        /// </summary>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule AddIf(IItemDropRuleCondition cond, int itemID, Fraction dropRate,
            int minQuantity = 1, int maxQuantity = 1)
        {
            return loot.Add(ItemDropRule.ByCondition(cond, itemID, dropRate.Denominator, minQuantity, maxQuantity,
                dropRate.Numerator));
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to a loot table
        /// </summary>
        /// <param name="ui">Whether drops registered with this condition appear in the Bestiary. Defaults to true.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule AddIf(Func<bool> lambda, int itemID, int dropRateInt = 1,
            int minQuantity = 1, int maxQuantity = 1, bool ui = true, string desc = null)
        {
            return loot.Add(ItemDropRule.ByCondition(If(lambda, ui, desc), itemID, dropRateInt, minQuantity,
                maxQuantity));
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to a loot table
        /// </summary>
        /// <param name="ui">Whether drops registered with this condition appear in the Bestiary. Defaults to true.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule AddIf(Func<bool> lambda, int itemID, Fraction dropRate,
            int minQuantity = 1, int maxQuantity = 1, bool ui = true, string desc = null)
        {
            return loot.Add(ItemDropRule.ByCondition(If(lambda, ui, desc), itemID, dropRate.Denominator, minQuantity,
                maxQuantity, dropRate.Numerator));
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to a loot table
        /// </summary>
        /// <param name="ui">Whether drops registered with this condition appear in the Bestiary. Defaults to true.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule AddIf(Func<DropAttemptInfo, bool> lambda, int itemID,
            int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1, bool ui = true, string desc = null)
        {
            return loot.Add(ItemDropRule.ByCondition(If(lambda, ui, desc), itemID, dropRateInt, minQuantity,
                maxQuantity));
        }

        /// <summary>
        /// Shorthand to add an arbitrary conditional drop to a loot table
        /// </summary>
        /// <param name="ui">Whether drops registered with this condition appear in the Bestiary. Defaults to true.</param>
        /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
        /// <returns>The item drop rule registered</returns>
        public IItemDropRule AddIf(Func<DropAttemptInfo, bool> lambda, int itemID,
            Fraction dropRate, int minQuantity = 1, int maxQuantity = 1, bool ui = true, string desc = null)
        {
            return loot.Add(ItemDropRule.ByCondition(If(lambda, ui, desc), itemID, dropRate.Denominator, minQuantity,
                maxQuantity, dropRate.Numerator));
        }

        /// <summary>
        /// Shorthand to add a simple normal-only drop to a loot table
        /// </summary>
        public IItemDropRule AddNormalOnly(int itemID, int dropRateInt = 1, int minQuantity = 1,
            int maxQuantity = 1)
        {
            return loot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), itemID, dropRateInt, minQuantity,
                maxQuantity));
        }

        /// <summary>
        /// Shorthand to add a simple normal-only drop to a loot table
        /// </summary>
        public IItemDropRule AddNormalOnly(int itemID, Fraction dropRate, int minQuantity = 1,
            int maxQuantity = 1)
        {
            return loot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), itemID, dropRate.Denominator,
                minQuantity,
                maxQuantity, dropRate.Numerator));
        }

        /// <summary>
        /// Shorthand to add an arbitrary drop rule as a normal-only drop to a loot table
        /// </summary>
        public void AddNormalOnly(IItemDropRule rule)
        {
            LeadingConditionRule normalOnly = loot.DefineNormalOnlyDropSet();
            normalOnly.Add(rule);
        }

        /// <summary>
        /// Registers a LeadingConditionRule for a loot table and returns it so you can add drops to that rule
        /// </summary>
        /// <param name="condition">The condition behind which you want to gate several drop rules</param>
        /// <returns>The LeadingConditionRule which encapsulates the given condition</returns>
        public LeadingConditionRule DefineConditionalDropSet(IItemDropRuleCondition condition)
        {
            LeadingConditionRule rule = new(condition);
            loot.Add(rule);
            return rule;
        }

        public LeadingConditionRule DefineConditionalDropSet(Func<bool> lambda) =>
            loot.DefineConditionalDropSet(If(lambda));

        public LeadingConditionRule DefineConditionalDropSet(Func<DropAttemptInfo, bool> lambda) =>
            loot.DefineConditionalDropSet(If(lambda));

        public LeadingConditionRule DefineNormalOnlyDropSet() =>
            loot.DefineConditionalDropSet(new Conditions.NotExpert());

        /// <summary>
        /// This function does its best to replace all instances of the given item in the given loot table's entries with the specified chance<br />
        /// It tries to affect as many types of drop rule as possible
        /// </summary>
        /// <param name="itemID">The item to drop</param>
        /// <param name="dropRate">The new drop rate to use</param>
        /// <param name="includeGlobalDrops">Whether or not to include global loot rules. Defaults to false. Generally, you should leave this as false</param>
        /// <returns>The number of changes made</returns>
        public int ChangeDropRate(int itemID, Fraction dropRate, bool includeGlobalDrops = false)
        {
            int numChanges = 0;
            List<IItemDropRule> rules = loot.Get(includeGlobalDrops);
            foreach (IItemDropRule rule in rules)
            {
                rule.RecursivelyMutateDropRate(itemID, dropRate.Numerator, dropRate.Denominator);
                numChanges++;
            }

            return numChanges;
        }
    }

    #endregion

    #region Global Drop Chances

    /// <summary>
    /// Weapons in Normal Mode typically have a 1 in X chance of dropping, where X is this variable
    /// </summary>
    public const int NormalWeaponDropRateInt = 4;

    /// <summary>
    /// Weapons in Normal Mode typically have this chance to drop, measured out of 1.0
    /// </summary>
    public const float NormalWeaponDropRateFloat = 0.25f;

    /// <summary>
    /// Weapons in Normal Mode typically have this chance to drop
    /// </summary>
    public static readonly Fraction NormalWeaponDropRateFraction = new(1, NormalWeaponDropRateInt);

    /// <summary>
    /// Weapons in Expert Mode typically have a 1 in X chance of dropping, where X is this variable
    /// </summary>
    public const int BagWeaponDropRateInt = 3;

    /// <summary>
    /// Weapons in Expert Mode typically have this chance to drop, measured out of 1.0
    /// </summary>
    public const float BagWeaponDropRateFloat = 0.3333333f;

    /// <summary>
    /// Weapons in Expert Mode typically have this chance to drop
    /// </summary>
    public static readonly Fraction BagWeaponDropRateFraction = new(1, BagWeaponDropRateInt);

    #endregion

    #region Specific Drop Helpers

    // Code copied from Player.QuickSpawnClonedItem, which was added by TML
    /// <summary>
    /// Clones the given item and spawns it into the world at the given position. You can also customize stack count as necessary.<br />
    /// The default stack count of -1 makes it copy the stack count of the given item.
    /// </summary>
    /// <param name="item">The item to clone and spawn</param>
    /// <param name="position">Where the item should be spawned</param>
    /// <param name="stack">The stack count to use. Leave at -1 to use the stack of the <b>item</b> parameter</param>
    /// <returns>The spawned clone of the item. Not equal to the input item.</returns>
    public static Item DropItemClone(IEntitySource src, Item item, Vector2 position, int stack = -1)
    {
        int index = Item.NewItem(src, position, item.type, stack, false, -1);
        Item theClone = Main.item[index] = item.Clone();
        theClone.whoAmI = index;
        theClone.position = position;
        if (stack != -1)
            theClone.stack = stack;

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);

        return theClone;
    }

    public static int FindClosestWormSegment(NPC wormHead, int[] wormSegmentIDs)
    {
        List<int> idsToCheck = [.. wormSegmentIDs];
        Vector2 playerPos = Main.player[wormHead.target].Center;

        int r = wormHead.whoAmI;
        float minDist = float.MinValue;
        foreach (NPC n in Main.ActiveNPCs)
            if (idsToCheck.Contains(n.type))
            {
                float dist = (n.Center - playerPos).LengthSquared();
                if (dist < minDist)
                {
                    minDist = dist;
                    r = n.whoAmI;
                }
            }

        return r;
    }

    public static DropBasedOnExpertMode NormalVsExpertQuantity(int itemID, int dropRateInt, int minNormal,
        int maxNormal, int minExpert, int maxExpert)
    {
        IItemDropRule normalRule = ItemDropRule.Common(itemID, dropRateInt, minNormal, maxNormal);
        IItemDropRule expertRule = ItemDropRule.Common(itemID, dropRateInt, minExpert, maxExpert);
        return new DropBasedOnExpertMode(normalRule, expertRule);
    }

    #endregion

    #region Lambda Drop Rule Condition

    internal readonly struct DropRuleCondition : IItemDropRuleCondition
    {
        private readonly Func<DropAttemptInfo, bool> condition;
        private readonly string description;
        private readonly bool visibleInUI;

        internal DropRuleCondition(Func<DropAttemptInfo, bool> lambda, bool ui = true, string desc = null)
        {
            condition = lambda;
            visibleInUI = ui;
            description = desc;
        }

        public bool CanDrop(DropAttemptInfo info) => condition(info);
        public bool CanShowItemDropInUI() => visibleInUI;
        public string GetConditionDescription() => description;
    }

    internal readonly struct DropRuleCondition2 : IItemDropRuleCondition
    {
        private readonly Func<DropAttemptInfo, bool> condition;
        private readonly string description;
        private readonly Func<bool> visibleInUI;

        internal DropRuleCondition2(Func<DropAttemptInfo, bool> lambda, Func<bool> ui, string desc = null)
        {
            condition = lambda;
            visibleInUI = ui;
            description = desc;
        }

        public bool CanDrop(DropAttemptInfo info) => condition(info);
        public bool CanShowItemDropInUI() => visibleInUI();
        public string GetConditionDescription() => description;
    }

    internal readonly struct DropRuleCondition3 : IItemDropRuleCondition
    {
        private readonly Func<DropAttemptInfo, bool> condition;
        private readonly Func<string> description;
        private readonly Func<bool> visibleInUI;

        internal DropRuleCondition3(Func<DropAttemptInfo, bool> lambda, Func<bool> ui, Func<string> desc)
        {
            condition = lambda;
            visibleInUI = ui;
            description = desc;
        }

        public bool CanDrop(DropAttemptInfo info) => condition(info);
        public bool CanShowItemDropInUI() => visibleInUI();
        public string GetConditionDescription() => description();
    }

    public static IItemDropRuleCondition If(Func<bool> lambda) => new DropRuleCondition(_ => lambda());

    public static IItemDropRuleCondition If(Func<bool> lambda, bool inBestiary = true, string bestiaryDesc = null)
    {
        return new DropRuleCondition(_ => lambda(), inBestiary, bestiaryDesc);
    }

    public static IItemDropRuleCondition If(Func<bool> lambda, Func<bool> inBestiary, string bestiaryDesc = null)
    {
        return new DropRuleCondition2(_ => lambda(), inBestiary, bestiaryDesc);
    }

    public static IItemDropRuleCondition If(Func<bool> lambda, Func<bool> inBestiary, Func<string> bestiaryDesc)
    {
        return new DropRuleCondition3(_ => lambda(), inBestiary, bestiaryDesc);
    }

    public static IItemDropRuleCondition If(Func<DropAttemptInfo, bool> lambda) => new DropRuleCondition(lambda);

    public static IItemDropRuleCondition If(Func<DropAttemptInfo, bool> lambda, bool inBestiary = true,
        string bestiaryDesc = null)
    {
        return new DropRuleCondition(lambda, inBestiary, bestiaryDesc);
    }

    public static IItemDropRuleCondition If(Func<DropAttemptInfo, bool> lambda, Func<bool> inBestiary,
        string bestiaryDesc = null)
    {
        return new DropRuleCondition2(lambda, inBestiary, bestiaryDesc);
    }

    public static IItemDropRuleCondition If(Func<DropAttemptInfo, bool> lambda, Func<bool> inBestiary,
        Func<string> bestiaryDesc)
    {
        return new DropRuleCondition3(lambda, inBestiary, bestiaryDesc);
    }

    #endregion

    #region Boss Defeat Conditionals

    public static IItemDropRuleCondition PostSlime(bool ui = true) =>
        Condition.DownedKingSlime.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostEye(bool ui = true) =>
        Condition.DownedEyeOfCthulhu.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostEvil(bool ui = true) =>
        Condition.DownedEowOrBoc.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostBee(bool ui = true) =>
        Condition.DownedQueenBee.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostDeer(bool ui = true) =>
        Condition.DownedDeerclops.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostSkele(bool ui = true) =>
        Condition.DownedSkeletron.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition Hardmode(bool ui = true) =>
        Condition.Hardmode.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostQueenSlime(bool ui = true) =>
        Condition.DownedQueenSlime.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostDest(bool ui = true) =>
        Condition.DownedDestroyer.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostTwins(bool ui = true) =>
        Condition.DownedTwins.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostPrime(bool ui = true) =>
        Condition.DownedSkeletronPrime.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostAnyMech(bool ui = true) =>
        Condition.DownedMechBossAny.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostAllMechs(bool ui = true) =>
        Condition.DownedMechBossAll.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostPlant(bool ui = true) =>
        Condition.DownedPlantera.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostFatty(bool ui = true) =>
        Condition.DownedGolem.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostEmpress(bool ui = true) =>
        Condition.DownedEmpressOfLight.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostFish(bool ui = true) =>
        Condition.DownedDukeFishron.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostLunatic(bool ui = true) =>
        Condition.DownedCultist.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    public static IItemDropRuleCondition PostMoonLord(bool ui = true) =>
        Condition.DownedMoonLord.ToDropCondition(ui ? ShowItemDropInUI.Always : ShowItemDropInUI.Never);

    #endregion

    #region Pity Style Drop Rule

    /// <summary>
    /// Every item in the list has the given chance to drop individually<br />
    /// If no items drop, then one of them is forced to drop, chosen at random
    /// </summary>
    public readonly struct AllOptionsAtOnceWithPityDropRule : IItemDropRule
    {
        public readonly WeightedItemStack[] Stacks;
        public readonly bool UsesLuck;
        public readonly Fraction DropRate;

        public AllOptionsAtOnceWithPityDropRule(Fraction dropRate, bool luck, WeightedItemStack[] stacks)
        {
            DropRate = dropRate;
            Stacks = stacks;
            UsesLuck = luck;
            ChainedRules = [];
        }

        public AllOptionsAtOnceWithPityDropRule(Fraction dropRate, bool luck, int[] itemIDs)
        {
            DropRate = dropRate;
            Stacks = new WeightedItemStack[itemIDs.Length];
            for (int i = 0; i < Stacks.Length; ++i)
                Stacks[i] = new WeightedItemStack(itemIDs[i]);
            UsesLuck = luck;
            ChainedRules = [];
        }

        public List<IItemDropRuleChainAttempt> ChainedRules { get; }

        public bool CanDrop(DropAttemptInfo info) => true;

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            bool droppedAnything = false;

            // Roll for each drop individually
            foreach (WeightedItemStack stack in Stacks)
            {
                bool rngRoll = UsesLuck
                    ? info.player.RollLuck(DropRate.Denominator) < DropRate.Numerator
                    : info.rng.NextFloat() < DropRate;
                droppedAnything |= rngRoll;
                if (rngRoll)
                    CommonCode.DropItem(info, stack.ItemID, stack.ChooseQuantity(info.rng));
            }

            // If everything fails to drop, force drop one item from the set
            if (!droppedAnything)
            {
                WeightedItemStack stack = info.rng.NextFromList(Stacks);
                CommonCode.DropItem(info, stack.ItemID, stack.ChooseQuantity(info.rng));
            }

            ItemDropAttemptResult result = default;
            result.State = ItemDropAttemptResultState.Success;
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            int numDrops = Stacks.Length;
            float rawDropRate = DropRate;
            // Combinatorics:
            // OPTION 1: [The item drops = Raw Drop Rate]
            // +
            // OPTION 2: [ALL items fail to drop = (1-x)^n] * [This item is chosen as pity = 1/n]
            float dropRateWithPityRoll = rawDropRate + (float) (Math.Pow(1f - rawDropRate, numDrops) * (1f / numDrops));
            float dropRateAdjustedForParent = dropRateWithPityRoll * ratesInfo.parentDroprateChance;

            // this calculation includes the fact that each individual item can be guaranteed as pity
            foreach (WeightedItemStack stack in Stacks)
                drops.Add(new DropRateInfo(stack.ItemID, stack.MinQuantity, stack.MaxQuantity,
                    dropRateAdjustedForParent, ratesInfo.conditions));

            Chains.ReportDroprates(ChainedRules, rawDropRate, drops, ratesInfo);
        }
    }

    public static IItemDropRule PityStyle(Fraction dropRateForEachItem, WeightedItemStack[] stacks) =>
        PityStyle(dropRateForEachItem, true, stacks);

    public static IItemDropRule PityStyle(Fraction dropRateForEachItem, bool luck, WeightedItemStack[] stacks)
    {
        return new AllOptionsAtOnceWithPityDropRule(dropRateForEachItem, luck, stacks);
    }

    public static IItemDropRule PityStyle(Fraction dropRateForEachItem, int[] itemIDs) =>
        PityStyle(dropRateForEachItem, true, itemIDs);

    public static IItemDropRule PityStyle(Fraction dropRateForEachItem, bool luck, int[] itemIDs)
    {
        return new AllOptionsAtOnceWithPityDropRule(dropRateForEachItem, luck, itemIDs);
    }

    #endregion

    #region Per Player Drop Rule

    public sealed class PerPlayerDropRule : CommonDrop
    {
        private const int DefaultDropProtectionTime = 18000; // 5 minutes
        private readonly int protectionTime;

        public PerPlayerDropRule(int itemID, int denominator, int minQuantity = 1, int maxQuantity = 1,
            int numerator = 1, int protectFrames = DefaultDropProtectionTime)
            : base(itemID, denominator, minQuantity, maxQuantity, numerator)
        {
            protectionTime = protectFrames;
        }

        public PerPlayerDropRule(int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
            : base(itemID, dropRate.Denominator, minQuantity, maxQuantity, dropRate.Numerator)
        {
            protectionTime = DefaultDropProtectionTime;
        }

        // Overriding CanDrop is unnecessary. This drop rule has no condition.
        // If you want to use a condition with PerPlayerDropRule, use DropHelper.If

        public override ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            ItemDropAttemptResult result = default;
            if (info.rng.Next(chanceDenominator) < chanceNumerator)
            {
                int stack = info.rng.Next(amountDroppedMinimum, amountDroppedMaximum + 1);
                TryDropInternal(info, itemId, stack);
                result.State = ItemDropAttemptResultState.Success;
                return result;
            }

            result.State = ItemDropAttemptResultState.FailedRandomRoll;
            return result;
        }

        // The contents of this method are more or less copied from CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0
        private void TryDropInternal(DropAttemptInfo info, int internalItemId, int stack)
        {
            if (internalItemId <= 0 || internalItemId >= ItemLoader.ItemCount)
                return;

            // If server-side, then the item must be spawned for each client individually
            if (Main.dedServ)
            {
                NPC npc = info.npc;
                int idx = Item.NewItem(npc.GetSource_Loot(), npc.Center, internalItemId, stack, true, -1);
                if (idx < Main.maxItems)
                {
                    Main.timeItemSlotCannotBeReusedFor[idx] = protectionTime;
                    foreach (Player player in Main.ActivePlayers)
                        NetMessage.SendData(MessageID.InstancedItem, player.whoAmI, -1, null, idx);
                    Main.item[idx].active = false;
                }
            }

            // Otherwise just drop the item
            else
            {
                CommonCode.DropItem(info, internalItemId, stack);
            }
        }
    }

    public static IItemDropRule PerPlayer(int itemID, int denominator = 1, int minQuantity = 1, int maxQuantity = 1,
        int numerator = 1)
    {
        return new PerPlayerDropRule(itemID, denominator, minQuantity, maxQuantity, numerator);
    }

    public static IItemDropRule PerPlayer(int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return PerPlayer(itemID, dropRate.Denominator, minQuantity, maxQuantity, dropRate.Numerator);
    }

    #endregion
}
