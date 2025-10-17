using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

public class AdditionsGlobalItem : GlobalItem
{
    // Declare custom events and their respective backing delegates.
    public delegate void ItemActionDelegate(Item item);

    public static event ItemActionDelegate SetDefaultsEvent;

    public delegate void ModifyTooltipsDelegate(Item item, List<TooltipLine> tooltips);

    public static event ModifyTooltipsDelegate ModifyTooltipsEvent;

    public delegate bool PreDrawInInventoryDelegate(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale);

    public static event PreDrawInInventoryDelegate PreDrawInInventoryEvent;

    public delegate bool PreDrawInWorldDelegate(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI);

    public static event PreDrawInWorldDelegate PreDrawInWorldEvent;

    public delegate bool CanItemDoActionWithPlayerDelegate(Item item, Player player);

    public static event CanItemDoActionWithPlayerDelegate CanUseItemEvent;

    public delegate void ItemPlayerActionDelegate(Item item, Player player);

    public static event ItemPlayerActionDelegate UseItemEvent;

    public override void Unload()
    {
        SetDefaultsEvent = null;
        ModifyTooltipsEvent = null;
        PreDrawInInventoryEvent = null;
        PreDrawInWorldEvent = null;
        CanUseItemEvent = null;
        UseItemEvent = null;
    }

    public override bool CanUseItem(Item item, Player player)
    {
        // Use default behavior if the event has no subscribers.
        if (CanUseItemEvent is null)
            return true;

        bool result = true;
        foreach (Delegate d in CanUseItemEvent.GetInvocationList())
            result &= ((CanItemDoActionWithPlayerDelegate)d).Invoke(item, player);

        return result;
    }

    public override bool? UseItem(Item item, Player player)
    {
        UseItemEvent?.Invoke(item, player);
        return null;
    }

    #region Prices
    public static readonly int RarityWhiteBuyPrice = Item.buyPrice(0, 0, 10, 0);

    public static readonly int RarityBlueBuyPrice = Item.buyPrice(0, 0, 70, 0);

    public static readonly int RarityGreenBuyPrice = Item.buyPrice(0, 2, 0, 0);

    public static readonly int RarityOrangeBuyPrice = Item.buyPrice(0, 4, 0, 0);

    public static readonly int RarityLightRedBuyPrice = Item.buyPrice(0, 6, 0, 0);

    public static readonly int RarityPinkBuyPrice = Item.buyPrice(0, 7, 50, 0);

    public static readonly int RarityLightPurpleBuyPrice = Item.buyPrice(0, 8, 75, 0);

    public static readonly int RarityLimeBuyPrice = Item.buyPrice(0, 10, 0, 0);

    public static readonly int RarityYellowBuyPrice = Item.buyPrice(0, 15, 50, 0);

    public static readonly int RarityCyanBuyPrice = Item.buyPrice(0, 19, 50, 0);

    public static readonly int RarityRedBuyPrice = Item.buyPrice(0, 23, 50, 0);

    public static readonly int RarityPurpleBuyPrice = Item.buyPrice(0, 26, 50, 0);

    public static readonly int UniqueRarityPrice = Item.buyPrice(0, 38, 75, 0);

    public static readonly int LaserRarityPrice = Item.buyPrice(0, 40, 0, 0);

    public static readonly int LegendaryRarityPrice = Item.buyPrice(0, 75, 0, 0);

    public static int GetBuyPrice(int rarity)
    {
        switch (rarity)
        {
            case 0:
                return RarityWhiteBuyPrice;
            case 1:
                return RarityBlueBuyPrice;
            case 2:
                return RarityGreenBuyPrice;
            case 3:
                return RarityOrangeBuyPrice;
            case 4:
                return RarityLightRedBuyPrice;
            case 5:
                return RarityPinkBuyPrice;
            case 6:
                return RarityLightPurpleBuyPrice;
            case 7:
                return RarityLimeBuyPrice;
            case 8:
                return RarityYellowBuyPrice;
            case 9:
                return RarityCyanBuyPrice;
            case 10:
                return RarityRedBuyPrice;
            case 11:
                return RarityPurpleBuyPrice;
            default:
                if (rarity == ModContent.RarityType<LaserClassRarity>())
                {
                    return LaserRarityPrice;
                }
                if (rarity == ModContent.RarityType<UniqueRarity>())
                {
                    return UniqueRarityPrice;
                }
                if (rarity == ModContent.RarityType<LegendaryRarity>())
                {
                    return LegendaryRarityPrice;
                }
                return 0;
        }
    }
    #endregion Prices

    public override bool OnPickup(Item item, Player player)
    {
        GlobalPlayer modPlayer = player.GetModPlayer<GlobalPlayer>();

        if ((item.type == ItemID.Heart || item.type == ItemID.CandyApple || item.type == ItemID.CandyCane)
            && modPlayer.FrigidTonic)
        {
            player.statLife += 5;
            if (Main.myPlayer == player.whoAmI)
                player.HealEffect(5, true);
        }
        return true;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        ModifyTooltipsEvent?.Invoke(item, tooltips);
    }

    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Player player = Main.LocalPlayer;

        // Use default behavior if the event has no subscribers.
        if (PreDrawInWorldEvent is null)
            return true;

        bool result = true;
        foreach (Delegate d in PreDrawInWorldEvent.GetInvocationList())
            result &= ((PreDrawInWorldDelegate)d).Invoke(item, spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);

        return result;
    }

    public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        // Use default behavior if the event has no subscribers.
        if (PreDrawInInventoryEvent is null)
            return true;

        bool result = true;
        foreach (Delegate d in PreDrawInInventoryEvent.GetInvocationList())
            result &= ((PreDrawInInventoryDelegate)d).Invoke(item, spriteBatch, position, frame, drawColor, itemColor, origin, scale);

        return result;
    }

    public override void SetDefaults(Item item)
    {
        SetDefaultsEvent?.Invoke(item);
    }
}