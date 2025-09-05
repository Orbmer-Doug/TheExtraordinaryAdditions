using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace TheExtraordinaryAdditions.Core.Utilities;

// DropHelper's credit in its ENTIRETY goes to Ozzatron in the Calamity Mod
// https://github.com/CalamityTeam/CalamityModPublic/blob/1.4.4/Utilities/DropHelper.cs

#region Fraction Struct
public struct Fraction
{
    internal readonly int numerator;
    internal readonly int denominator;

    public Fraction(int n, int d)
    {
        numerator = n < 0 ? 0 : n;
        denominator = d <= 0 ? 1 : d;
    }

    public static implicit operator float(Fraction f) => f.numerator / (float)f.denominator;
}
#endregion

#region Weighted Item Stack Struct
public struct WeightedItemStack
{
    public const float DefaultWeight = 1f;
    public const float MinisiculeWeight = 1E-6f;

    internal int itemID;
    internal float weight;
    internal int minQuantity;
    internal int maxQuantity;

    internal WeightedItemStack(int id, float w)
    {
        itemID = id;
        weight = w;
        minQuantity = 1;
        maxQuantity = 1;
    }

    internal WeightedItemStack(int id, float w, int quantity)
    {
        itemID = id;
        weight = w;
        minQuantity = quantity;
        maxQuantity = quantity;
    }

    internal WeightedItemStack(int id, float w, int min, int max)
    {
        itemID = id;
        weight = w;
        minQuantity = min;
        maxQuantity = max;
    }

    internal int ChooseQuantity(UnifiedRandom rng) => rng.Next(minQuantity, maxQuantity + 1);

    // Allow for implicitly casting integer item IDs into weighted item stacks.
    // Stack size is assumed to be 1. Weight is assumed to be default.
    public static implicit operator WeightedItemStack(int id)
    {
        return new WeightedItemStack(id, DefaultWeight, 1);
    }
}
#endregion

public static class DropHelper
{
    #region Global Drop Chances
    /// <summary>
    /// Weapons in Normal Mode typically have a 1 in X chance of dropping, where X is this variable.
    /// </summary>
    public const int NormalWeaponDropRateInt = 4;

    /// <summary>
    /// Weapons in Normal Mode typically have this chance to drop, measured out of 1.0.
    /// </summary>
    public const float NormalWeaponDropRateFloat = 0.25f;

    /// <summary>
    /// Weapons in Normal Mode typically have this chance to drop (as a DropHelper Fraction).
    /// </summary>
    public static readonly Fraction NormalWeaponDropRateFraction = new(1, NormalWeaponDropRateInt);

    /// <summary>
    /// Weapons in Expert Mode typically have a 1 in X chance of dropping, where X is this variable.
    /// </summary>
    public const int BagWeaponDropRateInt = 3;

    /// <summary>
    /// Weapons in Expert Mode typically have this chance to drop, measured out of 1.0.
    /// </summary>
    public const float BagWeaponDropRateFloat = 0.3333333f;

    /// <summary>
    /// Weapons in Expert Mode typically have this chance to drop (as a DropHelper Fraction).
    /// </summary>
    public static readonly Fraction BagWeaponDropRateFraction = new(1, BagWeaponDropRateInt);
    #endregion

    #region Block Drops
    private static int[] AllLoadedItemIDs = null;

    /// <summary>
    /// Adds the specified items to TML's blockLoot list. Items on the list cannot spawn in the world via any means.<br />
    /// <b>You should only use this function in the following places:</b><br />
    /// - ModNPC.PreKill and GlobalNPC.PreKill<br />
    /// - ModNPC.OnKill and GlobalNPC.OnKill<br /><br />
    /// This function is intended to block items from dropping from NPCs based on <b>TEMPORARY CONDITIONS.</b><br />
    /// If you want to <b>permanently remove</b> a drop from an NPC, this is not the function you want.<br />
    /// In those cases, use GlobalNPC.ModifyLoot, an if statement for that NPC, and loot.Remove or loot.RemoveWhere.<br />
    /// This will ensure that the drops are removed from the bestiary as well.
    /// </summary>
    /// <param name="itemIDs">The item IDs to prevent from spawning.</param>
    public static void BlockDrops(params int[] itemIDs)
    {
        foreach (int itemID in itemIDs)
            NPCLoader.blockLoot.Add(itemID);
    }

    /// <summary>
    /// Blocks every possible item in the game from dropping. This is the extreme version of BlockDrops.<br />
    /// <b>Please read the usage notes on BlockDrops.</b><br />
    /// This function intentionally still allows hearts and mana stars to drop. If you also want to block those, block them separately.
    /// </summary>
    /// <param name="exceptions">The item IDs to still allow to drop.</param>
    public static void BlockEverything(params int[] exceptions)
    {
        // This solution is legitimately brain damaged but it works for now
        // At least it's cached...
        if (AllLoadedItemIDs is null)
        {
            AllLoadedItemIDs = new int[ItemLoader.ItemCount];
            for (int i = 0; i < ItemLoader.ItemCount; ++i)
                AllLoadedItemIDs[i] = i;
        }

        // Apply exceptions
        int[] withSomeExceptions = new int[ItemLoader.ItemCount];
        AllLoadedItemIDs.CopyTo(withSomeExceptions, 0);
        withSomeExceptions[ItemID.Heart] = ItemID.RedPotion;
        withSomeExceptions[ItemID.Star] = ItemID.RedPotion;
        foreach (int itemID in exceptions)
            withSomeExceptions[itemID] = ItemID.RedPotion;

        BlockDrops(withSomeExceptions);
    }
    #endregion

    #region Specific Drop Helpers
    // Code copied from Player.QuickSpawnClonedItem, which was added by TML.
    /// <summary>
    /// Clones the given item and spawns it into the world at the given position. You can also customize stack count as necessary.<br></br>
    /// The default stack count of -1 makes it copy the stack count of the given item.
    /// </summary>
    /// <param name="item">The item to clone and spawn.</param>
    /// <param name="position">Where the item should be spawned.</param>
    /// <param name="stack">The stack count to use. Leave at -1 to use the stack of the <b>item</b> parameter.</param>
    /// <returns>The spawned clone of the item. <b>NEVER</b> equal to the input item.</returns>
    public static Item DropItemClone(IEntitySource src, Item item, Vector2 position, int stack = -1)
    {
        int index = Item.NewItem(src, position, item.type, stack, false, -1, false, false);
        Item theClone = Main.item[index] = item.Clone();
        theClone.whoAmI = index;
        theClone.position = position;
        if (stack != -1)
            theClone.stack = stack;

        // If in multiplayer, broadcast that this item was spawned.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);

        return theClone;
    }

    /// <summary>
    /// Finds the worm segment nearest to an NPC's target by combing the NPC array for the closest NPC that is one of the specified types.<br></br>
    /// Return the specified NPC's index if no matching worm segment was found.
    /// </summary>
    /// <param name="wormHead">The NPC whose target is used for distance comparisons.</param>
    /// <param name="wormSegmentIDs">An array (or multiple parameters) of NPC IDs which are the worm segments to look for.</param>
    /// <returns>An index in the NPC array of the closest worm segment, or the specified NPC's index.</returns>
    public static int FindClosestWormSegment(NPC wormHead, params int[] wormSegmentIDs)
    {
        List<int> idsToCheck = new List<int>(wormSegmentIDs);
        Vector2 playerPos = Main.player[wormHead.target].Center;

        int r = wormHead.whoAmI;
        float minDist = 1E+06f;
        for (int i = 0; i < Main.npc.Length; ++i)
        {
            NPC n = Main.npc[i];
            if (n != null && n.active && idsToCheck.Contains(n.type))
            {
                float dist = (n.Center - playerPos).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    r = i;
                }
            }
        }
        return r;
    }

    /// <summary>
    /// Shorthand for shorthand: Registers an item to drop per-player on the specified condition.<br />
    /// Intended for lore items, but can be used generally for instanced drops.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item ID to drop.</param>
    /// <returns>A LeadingConditionRule which you can attach more PerPlayer or other rules to as you want.</returns>
    public static LeadingConditionRule AddConditionalPerPlayer(this ILoot loot, Func<bool> lambda, int itemID)
    {
        LeadingConditionRule lcr = new(If(lambda));
        lcr.Add(PerPlayer(itemID));
        loot.Add(lcr);
        return lcr;
    }

    /// <summary>
    /// Shorthand for shorthand: Registers an item to drop per-player on the specified condition.<br />
    /// Intended for lore items, but can be used generally for instanced drops.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item ID to drop.</param>
    /// <returns>A LeadingConditionRule which you can attach more PerPlayer or other rules to as you want.</returns>
    public static LeadingConditionRule AddConditionalPerPlayer(this ILoot loot, Func<DropAttemptInfo, bool> lambda, int itemID)
    {
        LeadingConditionRule lcr = new(If(lambda));
        lcr.Add(PerPlayer(itemID));
        loot.Add(lcr);
        return lcr;
    }

    public static DropBasedOnExpertMode NormalVsExpertQuantity(int itemID, int dropRateInt, int minNormal, int maxNormal, int minExpert, int maxExpert)
    {
        IItemDropRule normalRule = ItemDropRule.Common(itemID, dropRateInt, minNormal, maxNormal);
        IItemDropRule expertRule = ItemDropRule.Common(itemID, dropRateInt, minExpert, maxExpert);
        return new DropBasedOnExpertMode(normalRule, expertRule);
    }
    #endregion

    #region Recursive Drop Rate Mutator
    private static int RecursivelyMutateDropRate(this IItemDropRule rule, int itemID, int newNumerator, int newDenominator)
    {
        if (rule is CommonDrop drop && drop.itemId == itemID)
        {
            drop.chanceNumerator = newNumerator;
            drop.chanceDenominator = newDenominator;
            return 1;
        }
        else if (rule is ItemDropWithConditionRule conditionalDrop && conditionalDrop.itemId == itemID)
        {
            conditionalDrop.chanceNumerator = newNumerator;
            conditionalDrop.chanceDenominator = newDenominator;
            return 1;
        }
        else if (rule is DropBasedOnExpertMode expertDrop)
        {
            int normalChanges = RecursivelyMutateDropRate(expertDrop.ruleForNormalMode, itemID, newNumerator, newDenominator);
            int expertChanges = RecursivelyMutateDropRate(expertDrop.ruleForExpertMode, itemID, newNumerator, newDenominator);
            return normalChanges + expertChanges;
        }
        else if (rule is DropBasedOnMasterMode masterDrop)
        {
            int defaultChanges = RecursivelyMutateDropRate(masterDrop.ruleForDefault, itemID, newNumerator, newDenominator);
            int masterChanges = RecursivelyMutateDropRate(masterDrop.ruleForMasterMode, itemID, newNumerator, newDenominator);
            return defaultChanges + masterChanges;
        }
        return 0;
    }
    #endregion

    #region Lambda Drop Rule Condition
    // This class serves as a vanilla drop rule condition that is based on completely arbitrary code.
    // Create these using the function DropHelper.If as needed.
    internal class LambdaDropRuleCondition : IItemDropRuleCondition
    {
        private readonly Func<DropAttemptInfo, bool> conditionLambda;
        private readonly bool visibleInUI;
        private readonly string description;

        internal LambdaDropRuleCondition(Func<DropAttemptInfo, bool> lambda, bool ui = true, string desc = null)
        {
            conditionLambda = lambda;
            visibleInUI = ui;
            description = desc;
        }

        public bool CanDrop(DropAttemptInfo info) => conditionLambda(info);
        public bool CanShowItemDropInUI() => visibleInUI;
        public string GetConditionDescription() => description;
    }

    /// <summary>
    /// Creates a new LambdaDropRuleCondition which executes the code of your choosing to decide whether this item drop should occur.<br />
    /// This version of "If" does <b>NOT</b> use the DropAttemptInfo struct that is available.<br />
    /// This lets you write simpler lambdas that do not need the context, e.g. just checking if a boss is dead.
    /// </summary>
    /// <param name="lambda">Lambda function which evaluates to true or false, deciding whether the item should drop. <code>() => {CodeHere}</code></param>
    /// <returns>The LambdaDropRuleCondition produced.</returns>
    public static IItemDropRuleCondition If(Func<bool> lambda) => new LambdaDropRuleCondition((_) => lambda());

    /// <summary>
    /// Creates a new LambdaDropRuleCondition which executes the code of your choosing to decide whether this item drop should occur.<br />
    /// This version of "If" does <b>NOT</b> use the DropAttemptInfo struct that is available.<br />
    /// This lets you write simpler lambdas that do not need the context, e.g. just checking if a boss is dead.
    /// </summary>
    /// <param name="lambda">Lambda function which evaluates to true or false, deciding whether the item should drop. <code>() => {CodeHere}</code></param>
    /// <param name="ui">Whether drops registered with this condition appear in the Bestiary. Defaults to true.</param>
    /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
    /// <returns>The LambdaDropRuleCondition produced.</returns>
    public static IItemDropRuleCondition If(Func<bool> lambda, bool ui = true, string desc = null)
    {
        bool LambdaInfoWrapper(DropAttemptInfo _) => lambda();
        return new LambdaDropRuleCondition(LambdaInfoWrapper, ui, desc);
    }

    /// <summary>
    /// Creates a new LambdaDropRuleCondition which executes the code of your choosing to decide whether this item drop should occur.<br />
    /// This version of "If" <b>DOES</b> use the DropAttemptInfo struct, and thus the provided lambda requires 1 argument.
    /// </summary>
    /// <param name="lambda">Lambda function which evaluates to true or false, deciding whether the item should drop. <code>(info) => {CodeHere}</code></param>
    /// <returns>The LambdaDropRuleCondition produced.</returns>
    public static IItemDropRuleCondition If(Func<DropAttemptInfo, bool> lambda) => new LambdaDropRuleCondition(lambda);

    /// <summary>
    /// Creates a new LambdaDropRuleCondition which executes the code of your choosing to decide whether this item drop should occur.<br />
    /// This version of "If" <b>DOES</b> use the DropAttemptInfo struct, and thus the provided lambda requires 1 argument.
    /// </summary>
    /// <param name="lambda">Lambda function which evaluates to true or false, deciding whether the item should drop. <code>(info) => {CodeHere}</code></param>
    /// <param name="ui">Whether drops registered with this condition appear in the Bestiary. Defaults to true.</param>
    /// <param name="desc">The description of this condition in the Bestiary. Defaults to null.</param>
    /// <returns>The LambdaDropRuleCondition produced.</returns>
    public static IItemDropRuleCondition If(Func<DropAttemptInfo, bool> lambda, bool ui = true, string desc = null)
    {
        return new LambdaDropRuleCondition(lambda, ui, desc);
    }

    #region Common Boss Stages
    public static IItemDropRuleCondition PostKS(bool ui = true)
    {
        return If(() => NPC.downedSlimeKing, ui, GetTextValue("Condition.Drops.DownedKS"));
    }

    public static IItemDropRuleCondition PostEoC(bool ui = true)
    {
        return If(() => NPC.downedBoss1, ui, GetTextValue("Condition.Drops.DownedEoC"));
    }

    public static IItemDropRuleCondition PostEvil1(bool ui = true)
    {
        return If(() => NPC.downedBoss2, ui, GetTextValue("Condition.Drops.Downed" + (WorldGen.crimson ? "BoC" : "EoW")));
    }

    public static IItemDropRuleCondition PostQB(bool ui = true)
    {
        return If(() => NPC.downedQueenBee, ui, GetTextValue("Condition.Drops.DownedQB"));
    }

    public static IItemDropRuleCondition PostDeer(bool ui = true)
    {
        return If(() => NPC.downedDeerclops, ui, GetTextValue("Condition.Drops.DownedDeer"));
    }

    public static IItemDropRuleCondition PostSkele(bool ui = true)
    {
        return If(() => NPC.downedBoss3, ui, GetTextValue("Condition.Drops.DownedSkele"));
    }

    public static IItemDropRuleCondition Hardmode(bool ui = true)
    {
        return If(() => Main.hardMode, ui, GetTextValue("Condition.Drops.Hardmode"));
    }

    public static IItemDropRuleCondition PostQS(bool ui = true)
    {
        return If(() => NPC.downedQueenSlime, ui, GetTextValue("Condition.Drops.DownedQS"));
    }

    public static IItemDropRuleCondition PostDest(bool ui = true)
    {
        return If(() => NPC.downedMechBoss1, ui, GetTextValue("Condition.Drops.DownedDest"));
    }

    public static IItemDropRuleCondition PostTwins(bool ui = true)
    {
        return If(() => NPC.downedMechBoss2, ui, GetTextValue("Condition.Drops.DownedTwins"));
    }

    public static IItemDropRuleCondition PostSP(bool ui = true)
    {
        return If(() => NPC.downedMechBoss3, ui, GetTextValue("Condition.Drops.DownedSP"));
    }

    public static IItemDropRuleCondition Post1Mech(bool ui = true)
    {
        return If(() => NPC.downedMechBossAny, ui, GetTextValue("Condition.Drops.Downed1Mech"));
    }

    public static IItemDropRuleCondition Post3Mechs(bool ui = true)
    {
        return If(() => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3, ui, GetTextValue("Condition.Drops.Downed3Mechs"));
    }


    public static IItemDropRuleCondition PostPlant(bool ui = true)
    {
        return If(() => NPC.downedPlantBoss, ui, GetTextValue("Condition.Drops.DownedPlant"));
    }

    public static IItemDropRuleCondition PostGolem(bool ui = true)
    {
        return If(() => NPC.downedGolemBoss, ui, GetTextValue("Condition.Drops.DownedGolem"));
    }

    public static IItemDropRuleCondition PostEoL(bool ui = true)
    {
        return If(() => NPC.downedEmpressOfLight, ui, GetTextValue("Condition.Drops.DownedEoL"));
    }

    public static IItemDropRuleCondition PostFish(bool ui = true)
    {
        return If(() => NPC.downedFishron, ui, GetTextValue("Condition.Drops.DownedFish"));
    }

    public static IItemDropRuleCondition PostLC(bool ui = true)
    {
        return If(() => NPC.downedAncientCultist, ui, GetTextValue("Condition.Drops.DownedLC"));
    }

    public static IItemDropRuleCondition PostML(bool ui = true)
    {
        return If(() => NPC.downedMoonlord, ui, GetTextValue("Condition.Drops.DownedML"));
    }
    #endregion
    #endregion

    #region Leading Condition Rule Extensions
    /// <summary>
    /// Adds any given drop rule as a chained rule to the given LeadingConditionRule.
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should have another drop rule registered as one of its chains.</param>
    /// <param name="chainedRule">The drop rule which should occur given this leading condition.</param>
    /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
    /// <returns>The LeadingConditionRule (first parameter).</returns>
    public static IItemDropRule Add(this LeadingConditionRule mainRule, IItemDropRule chainedRule, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(chainedRule, hideLootReport);
    }

    /// <summary>
    /// Shorthand to add a simple drop to the given LeadingConditionRule.
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should drop this item as one of its chains.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
    /// <returns>The LeadingConditionRule (first parameter).</returns>
    public static IItemDropRule Add(this LeadingConditionRule mainRule, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(ItemDropRule.Common(itemID, dropRateInt, minQuantity, maxQuantity), hideLootReport);
    }

    /// <summary>
    /// Shorthand to add a simple drop to the given LeadingConditionRule using a Fraction drop rate.
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should drop this item as one of its chains.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
    /// <returns>The LeadingConditionRule (first parameter).</returns>
    public static IItemDropRule Add(this LeadingConditionRule mainRule, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(new CommonDrop(itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator), hideLootReport);
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule.
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should drop this item as one of its chains.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this LeadingConditionRule mainRule, Func<bool> lambda, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(ItemDropRule.ByCondition(If(lambda), itemID, dropRateInt, minQuantity, maxQuantity), hideLootReport);
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule using a Fraction drop rate.
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should drop this item as one of its chains.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this LeadingConditionRule mainRule, Func<bool> lambda, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(ItemDropRule.ByCondition(If(lambda), itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator), hideLootReport);
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule.<br />
    /// <b>This version requires a lambda which uses DropAttemptInfo.</b>
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should drop this item as one of its chains.</param>
    /// <param name="lambda">A lambda which takes a DropAttemptInfo struct and evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this LeadingConditionRule mainRule, Func<DropAttemptInfo, bool> lambda, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(ItemDropRule.ByCondition(If(lambda), itemID, dropRateInt, minQuantity, maxQuantity), hideLootReport);
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to the given LeadingConditionRule using a Fraction drop rate.<br />
    /// <b>This version requires a lambda which uses DropAttemptInfo.</b>
    /// </summary>
    /// <param name="mainRule">The LeadingConditionRule which should drop this item as one of its chains.</param>
    /// <param name="lambda">A lambda which takes a DropAttemptInfo struct and evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <param name="hideLootReport">Set to true for this drop to not appear in the Bestiary.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this LeadingConditionRule mainRule, Func<DropAttemptInfo, bool> lambda, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1, bool hideLootReport = false)
    {
        return mainRule.OnSuccess(ItemDropRule.ByCondition(If(lambda), itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator), hideLootReport);
    }
    #endregion

    #region ILoot Extensions
    /// <summary>
    /// Shorthand to add a simple drop to a loot table.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule Add(this ILoot loot, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.Common(itemID, dropRateInt, minQuantity, maxQuantity));
    }

    /// <summary>
    /// Shorthand to add a simple drop to a loot table using a Fraction drop rate.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule Add(this ILoot loot, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(new CommonDrop(itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator));
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to a loot table.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="cond">An IItemDropRuleCondition which encapsulates the condition which needs to be checked in real-time.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this ILoot loot, IItemDropRuleCondition cond, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(cond, itemID, dropRateInt, minQuantity, maxQuantity));
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to a loot table using a Fraction drop rate.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="cond">An IItemDropRuleCondition which encapsulates the condition which needs to be checked in real-time.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this ILoot loot, IItemDropRuleCondition cond, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(cond, itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator));
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to a loot table.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this ILoot loot, Func<bool> lambda, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(If(lambda), itemID, dropRateInt, minQuantity, maxQuantity));
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to a loot table using a Fraction drop rate.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this ILoot loot, Func<bool> lambda, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(If(lambda), itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator));
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to a loot table.<br />
    /// <b>This version requires a lambda which uses DropAttemptInfo.</b>
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which takes a DropAttemptInfo struct and evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this ILoot loot, Func<DropAttemptInfo, bool> lambda, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(If(lambda), itemID, dropRateInt, minQuantity, maxQuantity));
    }

    /// <summary>
    /// Shorthand to add an arbitrary conditional drop to a loot table using a Fraction drop rate.<br />
    /// <b>This version requires a lambda which uses DropAttemptInfo.</b>
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which takes a DropAttemptInfo struct and evaluates in real-time to the condition that needs to be checked.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddIf(this ILoot loot, Func<DropAttemptInfo, bool> lambda, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(If(lambda), itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator));
    }

    /// <summary>
    /// Shorthand to add a simple normal-only drop to a loot table.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRateInt">The chance that the item will drop is 1 in this number. For example, 5 gives a 1 in 5 chance.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddNormalOnly(this ILoot loot, int itemID, int dropRateInt = 1, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), itemID, dropRateInt, minQuantity, maxQuantity));
    }

    /// <summary>
    /// Shorthand to add a simple normal-only drop to a loot table.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The chance that the item will drop as a DropHelper Fraction.</param>
    /// <param name="minQuantity">The minimum number of items to drop. Defaults to 1.</param>
    /// <param name="maxQuantity">The maximum number of items to drop. Defaults to 1.</param>
    /// <returns>The item drop rule registered.</returns>
    public static IItemDropRule AddNormalOnly(this ILoot loot, int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return loot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator));
    }

    /// <summary>
    /// Shorthand to add an arbitrary drop rule as a normal-only drop to a loot table.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="rule">The IItemDropRule to add.</param>
    public static void AddNormalOnly(this ILoot loot, IItemDropRule rule)
    {
        LeadingConditionRule normalOnly = loot.DefineNormalOnlyDropSet();
        normalOnly.Add(rule);
    }

    /// <summary>
    /// Registers a LeadingConditionRule for a loot table and returns it so you can add drops to that rule.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="condition">The condition behind which you want to gate several drop rules.</param>
    /// <returns>The LeadingConditionRule which encapsulates the given condition.</returns>
    public static LeadingConditionRule DefineConditionalDropSet(this ILoot loot, IItemDropRuleCondition condition)
    {
        LeadingConditionRule rule = new LeadingConditionRule(condition);
        loot.Add(rule);
        return rule;
    }

    /// <summary>
    /// Shorthand for registering a LeadingConditionRule using DropHelper.If.<br />
    /// This version does <b>NOT</b> use the DropAttemptInfo struct that is available.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <returns>The LeadingConditionRule which encapsulates the given lambda.</returns>
    public static LeadingConditionRule DefineConditionalDropSet(this ILoot loot, Func<bool> lambda) => loot.DefineConditionalDropSet(If(lambda));

    /// <summary>
    /// Shorthand for registering a LeadingConditionRule using DropHelper.If.<br />
    /// This version <b>DOES</b> use the DropAttemptInfo struct, and thus the provided lambda requires 1 argument.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="lambda">A lambda which evaluates in real-time to the condition that needs to be checked.</param>
    /// <returns>The LeadingConditionRule which encapsulates the given lambda.</returns>
    public static LeadingConditionRule DefineConditionalDropSet(this ILoot loot, Func<DropAttemptInfo, bool> lambda) => loot.DefineConditionalDropSet(If(lambda));

    /// <summary>
    /// Shorthand for shorthand: Registers a Normal Mode only LeadingConditionRule for a loot table and returns it to you.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <returns>A Normal Mode only LeadingConditionRule.</returns>
    public static LeadingConditionRule DefineNormalOnlyDropSet(this ILoot loot) => loot.DefineConditionalDropSet(new Conditions.NotExpert());

    /// <summary>
    /// This function does its best to replace all instances of the given item in the given loot table's entries with the specified chance.<br />
    /// It tries to affect as many types of drop rule as possible.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="newNumerator">The new numerator to use.</param>
    /// <param name="newDenominator">The new denominator to use.</param>
    /// <param name="includeGlobalDrops">Whether or not to include global loot rules. Defaults to false. Generally, you should leave this as false.</param>
    /// <returns>The number of changes made.</returns>
    public static int ChangeDropRate(this ILoot loot, int itemID, int newNumerator, int newDenominator, bool includeGlobalDrops = false)
    {
        int numChanges = 0;
        var rules = loot.Get(includeGlobalDrops);
        foreach (IItemDropRule rule in rules)
            rule.RecursivelyMutateDropRate(itemID, newNumerator, newDenominator);
        return numChanges;
    }

    /// <summary>
    /// This function does its best to replace all instances of the given item in the given loot table's entries with the specified chance.<br />
    /// It tries to affect as many types of drop rule as possible.
    /// </summary>
    /// <param name="loot">The ILoot interface for the loot table.</param>
    /// <param name="itemID">The item to drop.</param>
    /// <param name="dropRate">The new drop rate to use, as a DropHelper Fraction.</param>
    /// <param name="includeGlobalDrops">Whether or not to include global loot rules. Defaults to false. Generally, you should leave this as false.</param>
    /// <returns>The number of changes made.</returns>
    public static int ChangeDropRate(this ILoot loot, int itemID, Fraction dropRate, bool includeGlobalDrops = false)
    {
        return loot.ChangeDropRate(itemID, dropRate.numerator, dropRate.denominator, includeGlobalDrops);
    }
    #endregion

    #region Varied Stack Proportion Drop Rule
    public class VariedStackProportionDropRule : IItemDropRule
    {
        public bool UsesLuck;

        public Fraction DropRate;

        public WeightedItemStack[] Stacks;

        public ChangeDropStacksDelegate StacksChanger;

        public delegate void ChangeDropStacksDelegate(NPC npc, int itemID, ref int minQuantity, ref int maxQuantity);

        public List<IItemDropRuleChainAttempt> ChainedRules
        {
            get;
            set;
        }

        public VariedStackProportionDropRule(Fraction dropRate, ChangeDropStacksDelegate stacksChanger, bool luck, params WeightedItemStack[] stacks)
        {
            DropRate = dropRate;
            Stacks = stacks;
            StacksChanger = stacksChanger;
            UsesLuck = luck;
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public VariedStackProportionDropRule(Fraction dropRate, ChangeDropStacksDelegate stacksChanger, bool luck, params int[] itemIDs)
        {
            DropRate = dropRate;
            Stacks = new WeightedItemStack[itemIDs.Length];
            for (int i = 0; i < Stacks.Length; ++i)
                Stacks[i] = itemIDs[i];
            StacksChanger = stacksChanger;
            UsesLuck = luck;
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public bool CanDrop(DropAttemptInfo info) => true;

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            bool droppedAnything = false;
            ItemDropAttemptResult result = default;
            result.State = ItemDropAttemptResultState.Success;

            // Roll for each drop individually.
            foreach (WeightedItemStack stack in Stacks)
            {
                bool rngRoll = UsesLuck ? info.player.RollLuck(DropRate.denominator) < DropRate.numerator : info.rng.NextFloat() < DropRate;
                if (rngRoll)
                {
                    // Calculate the quantities of the items.
                    int minQuantity = stack.minQuantity;
                    int maxQuantity = stack.maxQuantity;
                    StacksChanger(info.npc, stack.itemID, ref minQuantity, ref maxQuantity);

                    // If the stacks changer resulted in invalid quantities, then move onto the next drop.
                    if (minQuantity <= 0 || maxQuantity <= 0 || minQuantity > maxQuantity)
                        continue;

                    CommonCode.DropItem(info, stack.itemID, info.rng.Next(minQuantity, maxQuantity));
                    droppedAnything = true;
                }
            }

            // If everything fails to drop, report the result as a failure.
            if (!droppedAnything)
                result.State = ItemDropAttemptResultState.FailedRandomRoll;

            // Return the result.
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            float dropRateAdjustedForParent = DropRate * ratesInfo.parentDroprateChance;

            // Report the drop rate of each individual item.
            foreach (WeightedItemStack stack in Stacks)
                drops.Add(new DropRateInfo(stack.itemID, stack.minQuantity, stack.maxQuantity, dropRateAdjustedForParent, ratesInfo.conditions));

            Chains.ReportDroprates(ChainedRules, DropRate, drops, ratesInfo);
        }
    }
    #endregion Varied Stack Proportion Drop Rule

    #region "Calamity Style" Drop Rule
    /// <summary>
    /// Also known as the "Calamity Style" drop rule.<br />
    /// Every item in the list has the given chance to drop individually.<br />
    /// If no items drop, then one of them is forced to drop, chosen at random.
    /// </summary>
    public class AllOptionsAtOnceWithPityDropRule : IItemDropRule
    {
        public WeightedItemStack[] stacks;
        public Fraction dropRate;
        public bool usesLuck;
        public List<IItemDropRuleChainAttempt> ChainedRules
        {
            get;
            set;
        }

        public AllOptionsAtOnceWithPityDropRule(Fraction dropRate, bool luck, params WeightedItemStack[] stacks)
        {
            this.dropRate = dropRate;
            this.stacks = stacks;
            usesLuck = luck;
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public AllOptionsAtOnceWithPityDropRule(Fraction dropRate, bool luck, params int[] itemIDs)
        {
            this.dropRate = dropRate;
            stacks = new WeightedItemStack[itemIDs.Length];
            for (int i = 0; i < stacks.Length; ++i)
                stacks[i] = itemIDs[i]; // implicit conversion operator
            usesLuck = luck;
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public bool CanDrop(DropAttemptInfo info) => true;

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            bool droppedAnything = false;

            // Roll for each drop individually.
            foreach (WeightedItemStack stack in stacks)
            {
                bool rngRoll = usesLuck ? info.player.RollLuck(dropRate.denominator) < dropRate.numerator : info.rng.NextFloat() < dropRate;
                droppedAnything |= rngRoll;
                if (rngRoll)
                    CommonCode.DropItem(info, stack.itemID, stack.ChooseQuantity(info.rng));
            }

            // If everything fails to drop, force drop one item from the set.
            if (!droppedAnything)
            {
                WeightedItemStack stack = info.rng.NextFromList(stacks);
                CommonCode.DropItem(info, stack.itemID, stack.ChooseQuantity(info.rng));
            }

            // Calamity style drops cannot fail. You will always get at least one item.
            ItemDropAttemptResult result = default;
            result.State = ItemDropAttemptResultState.Success;
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            int numDrops = stacks.Length;
            float rawDropRate = dropRate;
            // Combinatorics:
            // OPTION 1: [The item drops = Raw Drop Rate]
            // +
            // OPTION 2: [ALL items fail to drop = (1-x)^n] * [This item is chosen as pity = 1/n]
            float dropRateWithPityRoll = rawDropRate + (float)(Math.Pow(1f - rawDropRate, numDrops) * (1f / numDrops));
            float dropRateAdjustedForParent = dropRateWithPityRoll * ratesInfo.parentDroprateChance;

            // Report the drop rate of each individual item. This calculation includes the fact that each individual item can be guaranteed as pity.
            foreach (WeightedItemStack stack in stacks)
                drops.Add(new DropRateInfo(stack.itemID, stack.minQuantity, stack.maxQuantity, dropRateAdjustedForParent, ratesInfo.conditions));

            Chains.ReportDroprates(ChainedRules, rawDropRate, drops, ratesInfo);
        }
    }

    public static IItemDropRule CalamityStyle(Fraction dropRateForEachItem, params WeightedItemStack[] stacks) => CalamityStyle(dropRateForEachItem, true, stacks);
    public static IItemDropRule CalamityStyle(Fraction dropRateForEachItem, bool luck, params WeightedItemStack[] stacks)
    {
        return new AllOptionsAtOnceWithPityDropRule(dropRateForEachItem, luck, stacks);
    }
    public static IItemDropRule CalamityStyle(Fraction dropRateForEachItem, params int[] itemIDs) => CalamityStyle(dropRateForEachItem, true, itemIDs);
    public static IItemDropRule CalamityStyle(Fraction dropRateForEachItem, bool luck, params int[] itemIDs)
    {
        return new AllOptionsAtOnceWithPityDropRule(dropRateForEachItem, luck, itemIDs);
    }
    #endregion

    #region Per Player Drop Rule
    public class PerPlayerDropRule : CommonDrop
    {
        // Default instanced drops are protected for 15 minutes, because they are used for boss bags.
        // You can customize this duration as you see fit. Calamity defaults it to 5 minutes.
        private const int DefaultDropProtectionTime = 18000; // 5 minutes
        private int protectionTime;

        public PerPlayerDropRule(int itemID, int denominator, int minQuantity = 1, int maxQuantity = 1, int numerator = 1, int protectFrames = DefaultDropProtectionTime)
            : base(itemID, denominator, minQuantity, maxQuantity, numerator)
        {
            protectionTime = protectFrames;
        }

        public PerPlayerDropRule(int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
            : base(itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator)
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
        private void TryDropInternal(DropAttemptInfo info, int itemId, int stack)
        {
            if (itemId <= 0 || itemId >= ItemLoader.ItemCount)
                return;

            // If server-side, then the item must be spawned for each client individually.
            if (Main.netMode == NetmodeID.Server)
            {
                NPC npc = info.npc;
                int idx = Item.NewItem(npc.GetSource_Loot(), npc.Center, itemId, stack, true, -1);
                Main.timeItemSlotCannotBeReusedFor[idx] = protectionTime;
                for (int i = 0; i < Main.maxPlayers; ++i)
                    if (Main.player[i].active)
                        NetMessage.SendData(MessageID.InstancedItem, i, -1, null, idx);
                Main.item[idx].active = false;
            }

            // Otherwise just drop the item.
            else
                CommonCode.DropItem(info, itemId, stack);
        }
    }

    public static IItemDropRule PerPlayer(int itemID, int denominator = 1, int minQuantity = 1, int maxQuantity = 1, int numerator = 1)
    {
        return new PerPlayerDropRule(itemID, denominator, minQuantity, maxQuantity, numerator);
    }
    public static IItemDropRule PerPlayer(int itemID, Fraction dropRate, int minQuantity = 1, int maxQuantity = 1)
    {
        return PerPlayer(itemID, dropRate.denominator, minQuantity, maxQuantity, dropRate.numerator);
    }
    #endregion
}