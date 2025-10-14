using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.UI;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

public class VanillaChanges : GlobalItem
{
    public override bool AppliesToEntity(Item item, bool lateInstantiation)
    {
        if (!AdditionsConfigServer.Instance.UseCustomAI)
            return false;
        return lateInstantiation && item.type < ItemID.Count && item.ModItem == null;
    }

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.PhoenixBlaster] = true;
    }

    public override void SetDefaults(Item item)
    {
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

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
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

    public override bool CanShoot(Item item, Player player)
    {
        return item.type switch
        {
            ItemID.BreakerBlade | ItemID.TheHorsemansBlade | ItemID.InfluxWaver => player.ownedProjectileCounts[item.shoot] <= 0,
            _ => true,
        };
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
                p.AdditionsInfo().ExtraAI[0] = extra0;
                p.AdditionsInfo().ExtraAI[1] = extra1;
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
                if (mod.SafeMouseRight.Current)
                    crush.Beam = true;
                if (!mod.SafeMouseRight.Current && s && !mod.AtMaxLimit)
                    crush.State = BreakerBladeCrush.BladeState.Charging;

                return false;
        }

        bool damageClass = item.CountsAsClass<RangedDamageClass>() || item.CountsAsClass<MeleeDamageClass>()
            || item.CountsAsClass<MagicDamageClass>() || item.CountsAsClass<ThrowingDamageClass>() || item.CountsAsClass<SummonDamageClass>();
        if (mod.Buffs[GlobalPlayer.AdditionsBuff.AridFlask] && damageClass && Main.rand.NextBool(4) && !item.channel && player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SandBlast>(), DamageSoftCap(damage, 80), 1f, player.whoAmI, 0f, 0f, 0f);
        return true;
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

    public override bool CanUseItem(Item item, Player player)
    {
        switch (item.type)
        {
            case ItemID.HeatRay:
                if (player.HasBuff(LaserResource.OverheatBuff))
                    return false;
                return true;
        }

        return base.CanUseItem(item, player);
    }

    public override bool? UseItem(Item item, Player player)
    {
        return base.UseItem(item, player);
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
