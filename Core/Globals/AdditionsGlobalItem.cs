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

namespace TheExtraordinaryAdditions.Core.Globals;

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
        // Reset all events on mod unload.
        SetDefaultsEvent = null;
        ModifyTooltipsEvent = null;
        PreDrawInInventoryEvent = null;
        PreDrawInWorldEvent = null;
        CanUseItemEvent = null;
        UseItemEvent = null;
    }

    public override bool CanUseItem(Item item, Player player)
    {
        switch (item.type)
        {
            case ItemID.HeatRay:
                if (player.HasBuff(LaserResource.OverheatBuff))
                    return false;
                return true;
        }

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
        switch (item.type)
        {
            case ItemID.CursedFlames:
                return false;
        }
        UseItemEvent?.Invoke(item, player);
        return null;
    }

    public override void UpdateEquip(Item item, Player player)
    {
        if (item.type == ItemID.VikingHelmet)
        {
            if (player.HeldItem.axe > 0)
            {
                player.GetDamage<MeleeDamageClass>() += .2f;
                player.GetCritChance<MeleeDamageClass>() += 15f;
                player.GetAttackSpeed<MeleeDamageClass>() += .5f;
            }
        }
    }

    public override string IsArmorSet(Item head, Item body, Item legs)
    {
        return "";
    }

    public override void UpdateArmorSet(Player player, string set)
    {
        Item item = player.HeldItem;
        int t = item.type;
        ModPlayer mod = player.Additions();

        switch (set)
        {
            case "Crimson":
                if (t == ItemID.BloodButcherer || t == ItemID.TendonBow || t == ItemID.TheMeatball || t == ItemID.CrimsonYoyo)
                {
                    player.GetDamage<GenericDamageClass>() += .15f;
                }

                break;
            case "Shadow":
                if (t == ItemID.LightsBane || t == ItemID.DemonBow || t == ItemID.BallOHurt || t == ItemID.CorruptYoyo)
                {
                    player.GetDamage<GenericDamageClass>() += .15f;
                }
                break;
        }
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

        if ((item.type == ItemID.Heart || item.type == ItemID.CandyApple || item.type == ItemID.CandyCane) && modPlayer.frigidTonic)
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

        void NewLine(string name)
        {
            // If there is no tooltip in the first place, there is none to search for
            // is there a way around this????
            tooltips.Add(new TooltipLine(Mod, "Tool", GetText("AdditionsGlobalItem." + name).Value));
        }

        switch (item.type)
        {
            case ItemID.DemonBow:
                NewLine("DemonBow");
                break;
            case ItemID.TendonBow:
                NewLine("TendonBow");
                break;
            case ItemID.PhoenixBlaster:
                NewLine("PhoenixBlaster");
                break;
            case ItemID.InfluxWaver:
                NewLine("InfluxWaver");
                break;
            case ItemID.TheHorsemansBlade:
                NewLine("Horsemen");
                break;
            case ItemID.Kraken:
                NewLine("Kraken");
                break;
            case ItemID.TheEyeOfCthulhu:
                NewLine("TheEyeOfCthulhu");
                break;
            case ItemID.Valor:
                NewLine("Valor");
                break;
            case ItemID.AquaScepter:
                NewLine("AquaScepter");
                break;
            case ItemID.VikingHelmet:
                NewLine("VikingHelmet");
                break;
            case ItemID.HallowedRepeater:
                NewLine("HallowedRepeater");
                break;
            case ItemID.MagnetSphere:
                NewLine("MagnetSphere");
                break;
            case ItemID.CursedFlames:
                NewLine("CursedFlames");
                break;
            case ItemID.GoldenShower:
                NewLine("GoldenShower");
                break;
            case ItemID.BookofSkulls:
                NewLine("BookofSkulls");
                break;
            case ItemID.HeatRay:
                NewLine("HeatRay");
                break;
            case ItemID.BreakerBlade:
                NewLine("BreakerBlade");
                break;
            case ItemID.AmethystStaff:
                NewLine("AmethystStaff");
                break;
            case ItemID.TopazStaff:
                NewLine("TopazStaff");
                break;
            case ItemID.SapphireStaff:
                NewLine("SapphireStaff");
                break;
            case ItemID.EmeraldStaff:
                NewLine("EmeraldStaff");
                break;
            case ItemID.AmberStaff:
                NewLine("AmberStaff");
                break;
            case ItemID.RubyStaff:
                NewLine("RubyStaff");
                break;
            case ItemID.DiamondStaff:
                NewLine("DiamondStaff");
                break;
        }
    }

    public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Player owner = Main.player[player.whoAmI];
        GlobalPlayer mod = player.Additions();

        float adjustedItemScale = player.GetAdjustedItemScale(item);

        void NewProj(int type, int? damag, Vector2? pos = default, Vector2? vel = default, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                int proj = Projectile.NewProjectile(source, pos ?? position, vel ?? velocity, type, damag ?? damage, knockback, Main.myPlayer, ai0, ai1, ai2);
                Projectile p = Main.projectile[proj];
                p.Additions().ExtraAI[0] = extra0;
                p.Additions().ExtraAI[1] = extra1;
                if (proj.BetweenNum(0, Main.maxProjectiles, true))
                    p.netUpdate = true;
            }
        }

        switch (item.type)
        {
            case ItemID.DemonBow:
                NewProj(ModContent.ProjectileType<DarkArrow>(), default, default);
                return false;
            case ItemID.TendonBow:
                NewProj(ModContent.ProjectileType<CrimtaneArrow>(), default, default);
                return false;
            case ItemID.PhoenixBlaster:
                if (player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed)
                {
                    NewProj(ModContent.ProjectileType<PhoenixRound>(), default, default, velocity * .7f);
                    return false;
                }
                return true;
            case ItemID.InfluxWaver:
                NewProj(ModContent.ProjectileType<InfluxWaverSwing>(), default, default, velocity);
                return false;
            case ItemID.TheHorsemansBlade:
                if (player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed)
                {
                    NewProj(ModContent.ProjectileType<HorsemenDive>(), damage * 2, default, velocity);
                    CalUtils.AddCooldown(player, PumpkinDashCooldown.ID, SecondsToFrames(3));
                    return false;
                }

                NewProj(ModContent.ProjectileType<HorsemenSwing>(), default, default, velocity);
                return false;
            case ItemID.AquaScepter:
                NewProj(ModContent.ProjectileType<WaterStream>(), default, default, velocity, 0f, Collision.DrownCollision(player.TopLeft, player.width, player.height) ? 1f : 0f);
                return false;
            case ItemID.InfernoFork:
                NewProj(ModContent.ProjectileType<InfernalFork>(), default);
                return false;
            case ItemID.MagnetSphere:
                NewProj(ModContent.ProjectileType<EnhancedMagnetSphere>(), default);
                return false;
            case ItemID.SpaceGun:
                NewProj(ModContent.ProjectileType<SpaceRay>(), default);
                return false;
            case ItemID.LaserRifle:
                NewProj(ModContent.ProjectileType<LaserBlast>(), default);
                return false;
            case ItemID.BreakerBlade:
                BreakerBladeCrush crush = Main.projectile[Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer)].As<BreakerBladeCrush>();
                bool s = Main.keyState.IsKeyDown(Keys.S);
                if (mod.SafeMouseRight.Current && !s)
                    crush.Beam = true;
                if (!mod.SafeMouseRight.Current && s && !mod.AtMaxLimit)
                    crush.State = BreakerBladeCrush.BladeState.Charging;

                return false;
        }

        bool damageClass = item.CountsAsClass<RangedDamageClass>() || item.CountsAsClass<MeleeDamageClass>()
            || item.CountsAsClass<MagicDamageClass>() || item.CountsAsClass<ThrowingDamageClass>() || item.CountsAsClass<SummonDamageClass>();
        if (mod.aridFlask && damageClass && Main.rand.NextBool(4) && !item.channel && player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SandBlast>(), DamageSoftCap(damage, 80), 1f, player.whoAmI, 0f, 0f, 0f);
        return true;
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

    public override void PostDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        Player player = Main.LocalPlayer;
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

    public override bool AltFunctionUse(Item item, Player player)
    {
        return item.type switch
        {
            ItemID.PhoenixBlaster => true,
            ItemID.CursedFlames => true,
            ItemID.BreakerBlade => true,
            ItemID.TheHorsemansBlade => player.ownedProjectileCounts[ModContent.ProjectileType<HorsemenDive>()] <= 0 && !CalUtils.HasCooldown(player, PumpkinDashCooldown.ID),
            _ => base.AltFunctionUse(item, player),
        };
    }

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.PhoenixBlaster] = true;
    }

    public override void SetDefaults(Item item)
    {
        SetDefaultsEvent?.Invoke(item);

        switch (item.type)
        {
            case ItemID.MagnetSphere:
                item.shootSpeed = 10f;
                item.damage = 86;
                item.knockBack = 3f;
                item.useAnimation = item.useTime = 32;
                break;
            case ItemID.BreakerBlade:
                NoVisual();
                item.shoot = ModContent.ProjectileType<BreakerBladeCrush>();
                item.autoReuse = true;
                break;
            case ItemID.InfluxWaver:
                NoVisual();
                item.shoot = ModContent.ProjectileType<InfluxWaverSwing>();
                item.autoReuse = true;
                break;
            case ItemID.TheHorsemansBlade:
                NoVisual();
                item.shoot = ModContent.ProjectileType<HorsemenSwing>();
                item.autoReuse = true;
                break;
        }

        void NoVisual()
        {
            item.noUseGraphic = true;
            item.UseSound = null;
            item.noMelee = true;
        }
    }

    public override bool CanShoot(Item item, Player player)
    {
        return item.type switch
        {
            ItemID.BreakerBlade | ItemID.TheHorsemansBlade | ItemID.InfluxWaver => player.ownedProjectileCounts[item.shoot] <= 0,
            _ => true,
        };
    }

    public override void UseItemFrame(Item item, Player player)
    {
        if (Main.myPlayer == player.whoAmI)
        {
            switch (item.type)
            {
                case ItemID.PhoenixBlaster:
                    DoShotAnimation(.45f);
                    break;
                case ItemID.SpaceGun:
                    AimArms();
                    break;
                case ItemID.LaserRifle:
                    AimArms();
                    break;
            }
        }

        void DoShotAnimation(float amount)
        {
            player.ChangeDir(Math.Sign((player.Additions().mouseWorld - player.Center).X));
            float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Additions().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.4f)
            {
                rotation += -amount * ((0.4f - animProgress) / 0.4f).Squared() * player.direction;
            }
            player.SetCompositeArmFront(true, 0, rotation);
        }

        void AimArms()
        {
            player.ChangeDir(Math.Sign((player.Additions().mouseWorld - player.Center).X));
            float rotation = (player.Center - player.Additions().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, 0, rotation);
        }
    }

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
    {
        if (Main.myPlayer == player.whoAmI)
        {
            switch (item.type)
            {
                case ItemID.PhoenixBlaster:
                    HoldOut(42f, new(42f, 30f), new(7f, 3f));
                    break;
                case ItemID.SpaceGun:
                    HoldOut(28f, new(36f, 24f), new(4f, 2f));
                    break;
                case ItemID.LaserRifle:
                    HoldOut(38f, new(40f, 24f), new(0f, 0f));
                    break;
            }
        }

        void HoldOut(float dist, Vector2 itemSize, Vector2 itemOrigin, float rot = MathHelper.PiOver2)
        {
            player.ChangeDir(Math.Sign((player.Additions().mouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + rot * player.gravDir;
            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * dist;
            CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
        }
    }

    public override void HoldItem(Item item, Player player)
    {
        switch (item.type)
        {
            case ItemID.PhoenixBlaster:
                break;
            case ItemID.BreakerBlade:
                LimitBreakerUI.CurrentlyViewing = true;
                break;
        }
    }

    public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        switch (item.type)
        {
            case ItemID.PhoenixBlaster:
                position -= Vector2.UnitY * 10f;
                break;
            case ItemID.HallowedRepeater:
                type = ModContent.ProjectileType<DivineArrow>();
                break;
        }
    }
}