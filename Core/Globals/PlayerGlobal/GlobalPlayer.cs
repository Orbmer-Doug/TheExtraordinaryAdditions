using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI;

namespace TheExtraordinaryAdditions.Core.Globals;

public sealed partial class GlobalPlayer : ModPlayer
{
    public delegate void PlayerActionDelegate(GlobalPlayer p);

    public static event PlayerActionDelegate ResetEffectsEvent;

    public static event PlayerActionDelegate PostUpdateEvent;

    public delegate void MaxStatsDelegate(GlobalPlayer p, ref StatModifier health, ref StatModifier mana);

    public static event MaxStatsDelegate MaxStatsEvent;

    public override void Load()
    {
        ResetMinion();
        ResetBuffs();
    }

    public override void Unload()
    {
        PostUpdateEvent = null;
        ResetEffectsEvent = null;
        MaxStatsEvent = null;
    }

    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        health = StatModifier.Default;
        mana = StatModifier.Default;
        MaxStatsEvent?.Invoke(this, ref health, ref mana);
    }

    #region Vanilla
    public float BreakerLimit;
    public const int MaxLimit = 100;
    public int LimitTimer;
    public float CurrentLimit => InverseLerp(0f, MaxLimit, BreakerLimit);
    public bool AtMaxLimit => CurrentLimit == 1f;
    public bool PlayedLimitSound = false;
    public static readonly int MaxTimeWithLimit = SecondsToFrames(15);
    #endregion Vanilla

    public override void UpdateDead()
    {
        LungingDown = false;

        PlayedLimitSound = false;
        BreakerLimit = 0f;
        LimitTimer = 0;

        HealingPotBonus = 1f;
    }

    public override void ResetEffects()
    {
        ResetEffectsEvent?.Invoke(this);

        int percentMaxLifeIncrease = 0;
        if (Player.GetModPlayer<RejuvenationArtifactPlayer>().Equipped)
            percentMaxLifeIncrease += 5;
        if (Player.GetModPlayer<NothingTherePlayer>().Equipped)
            percentMaxLifeIncrease += 10;
        Player.statLifeMax2 += Player.statLifeMax / 5 / 20 * percentMaxLifeIncrease;

        #region SetFalse
        ResetMinion();
        ResetBuffs();

        Teleport = false;
        HealingPotBonus = 1f;

        #endregion SetFalse
    }

    public override void PreUpdate()
    {
        if (Player.whoAmI == Main.myPlayer)
        {
            if (Player.HeldItem.type != ItemID.BreakerBlade)
                LimitBreakerUI.CurrentlyViewing = false;

            if (Player.heldProj != -1)
            {
                Projectile proj = Main.projectile[Player.heldProj] ?? null;
                if (proj == null || proj.type != ModContent.ProjectileType<TesselesticMeltdownProj>())
                    TesselesticHeatUI.CurrentlyViewing = false;
            }
        }

        UpdateMouse();
    }

    public static bool HasDamageClass(Player player)
    {
        Item item = player.HeldItem;
        return item.CountsAsClass<RangedDamageClass>() || item.CountsAsClass<MeleeDamageClass>()
            || item.CountsAsClass<MagicDamageClass>() || item.CountsAsClass<ThrowingDamageClass>()
            || item.CountsAsClass<SummonDamageClass>() || item.CountsAsClass<CalamityMod.RogueDamageClass>()
            || item.CountsAsClass<CalamityMod.TrueMeleeDamageClass>()
            || item.CountsAsClass<CalamityMod.TrueMeleeNoSpeedDamageClass>();
    }

    public override void PostUpdateMiscEffects()
    {
        Item item = Player.HeldItem;
        bool damageClass = HasDamageClass(Player);

        if (LungingDown)
        {
            Player.maxFallSpeed = 480f;
            Player.noFallDmg = true;
        }

        if (Player.GetModPlayer<RejuvenationArtifactPlayer>().Equipped)
        {
            HealingPotBonus += 0.5f;
        }
    }

    public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
    {
        healValue = (int)(healValue * HealingPotBonus);
    }

    /// <summary>
    /// Finds a accessory with a specified item id
    /// </summary>
    /// <param name="itemID"></param>
    /// <returns>The accessory found</returns>
    public Item FindAccessory(int itemID)
    {
        for (int i = 0; i < 10; i++)
        {
            if (Player.armor[i].type == itemID)
                return Player.armor[i];
        }

        return new Item();
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        if (Player != null && LimitBreakerUI.CurrentlyViewing && BreakerLimit < MaxLimit)
            BreakerLimit += info.Damage * .1f;
    }

    public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
    {
        // funy
        if ((npc.type == NPCID.DemonEye || npc.type == NPCID.BigMimicJungle ||
            npc.type == NPCID.BrainofCthulhu || npc.type == NPCID.Snail ||
            npc.type == NPCID.SolarCrawltipedeHead || npc.type == NPCID.WyvernHead ||
            npc.type == NPCID.Clown || npc.type == NPCID.GiantTortoise ||
            npc.type == NPCID.DuneSplicerHead || npc.type == NPCID.CaveBat ||
            npc.type == NPCID.JungleBat || npc.type == NPCID.Medusa ||
            npc.type == NPCID.MossHornet || npc.type == NPCID.LavaSlime ||
            npc.type == NPCID.Harpy || npc.type == NPCID.Gastropod) && Main.rand.NextBool(80) && Main.zenithWorld)
        {
            modifiers.Knockback *= 25f;
            modifiers.KnockbackImmunityEffectiveness *= 0f;
        }
    }

    public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
    {
        if (proj.type == ModContent.ProjectileType<StickBoom>())
        {
            modifiers.Knockback *= 2f;
            modifiers.KnockbackImmunityEffectiveness *= 0f;
            modifiers.FinalDamage *= .01f;
        }
    }

    public override void ModifyStartingInventory(IReadOnlyDictionary<string, List<Item>> itemsByMod, bool mediumCoreDeath)
    {
        itemsByMod["Terraria"].Clear();

        List<Item> items = [new(ItemID.CopperBroadsword), new(ItemID.CopperPickaxe), new(ItemID.CopperAxe),
                new(ItemID.Torch, 15), new(ItemID.RopeCoil, 2), new(ItemID.Cobweb, 6), new(ItemID.BottledWater, 8), new(ItemID.Apple, 2)];

        for (int i = 0; i < items.Count - 1; i++)
            itemsByMod["Terraria"].Add(items[i]);
    }

    public override void PostUpdateBuffs()
    {
        if (LimitBreakerUI.CurrentlyViewing)
        {
            if (AtMaxLimit)
            {
                Player.moveSpeed += .5f;
                Player.fallStart = (int)(Player.position.Y / 60f/*16f*/);
                Player.maxFallSpeed = 20f/*10f*/;
            }
        }
    }

    public override void PostUpdate()
    {
        PostUpdateEvent?.Invoke(this);

        if (AtMaxLimit)
        {
            if (LimitTimer > MaxTimeWithLimit)
            {
                BreakerLimit = 0;
                LimitTimer = 0;
            }

            LimitTimer++;
        }

        if (LimitBreakerUI.CurrentlyViewing)
        {
            if (BreakerLimit > MaxLimit)
            {
                if (!PlayedLimitSound)
                {
                    AdditionsSound.BreakerCapped.Play(Player.Center);
                    PlayedLimitSound = true;
                }
                BreakerLimit = MaxLimit;
            }
            else
                PlayedLimitSound = false;
        }


        GlobalTimer++;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
    {
        if (Main.rand.NextBool(60))
            damageSource = PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.TheExtraordinaryAdditions.Status.Death.Silly" + Main.rand.Next(1, 3), Player.name));
        return true;
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
    {
        Vector2 randHitbox = Player.RandAreaInEntity();
        bool noShadow = drawInfo.shadow == 0f;

        if (Buffs[AdditionsBuff.Overheat] && !Player.dead)
        {
            if (Main.rand.NextBool(3) && noShadow)
            {
                Vector2 vel = Vector2.UnitY.RotatedByRandom(.25f) * -Main.rand.NextFloat(4f, 10f);
                float scale = Main.rand.NextFloat(.3f, .8f);
                int life = Main.rand.Next(12, 20);
                Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
                ParticleRegistry.SpawnHeavySmokeParticle(randHitbox, vel, life, scale, color, .9f, true, .09f);

                Dust.NewDustPerfect(randHitbox, DustID.SteampunkSteam, vel * .7f, 0, default, scale * 1.4f);
            }
            g *= 0.3f;
            r *= 0.52f;
            b *= 0.2f;
        }

        if (Buffs[AdditionsBuff.DentedBySpoon])
        {
            g *= 0.75f;
            r *= 0.0f;
            b *= 0.75f;
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (AdditionsKeybinds.TeleportHotKey.Current && Teleport && Main.myPlayer == Player.whoAmI && !Player.CCed && !Player.chaosState)
        {
            Vector2 teleportLocation = default;
            teleportLocation.X = Main.mouseX + Main.screenPosition.X;
            if (Player.gravDir == 1f)
                teleportLocation.Y = Main.mouseY + Main.screenPosition.Y - Player.height;
            else
                teleportLocation.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY;
            teleportLocation.X -= Player.width / 2;
            if (teleportLocation.X > 50f && teleportLocation.X < Main.maxTilesX * 16 - 50 && teleportLocation.Y > 50f
                && teleportLocation.Y < Main.maxTilesY * 16 - 50 && !Collision.SolidCollision(teleportLocation, Player.width, Player.height))
            {
                Player.Teleport(teleportLocation, TeleportationStyleID.Portal, 0);
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, teleportLocation.X, teleportLocation.Y, 1, 0, 0);
                Player.AddBuff(BuffID.ChaosState, SecondsToFrames(60));
            }
        }
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        Vector2 pos = new(drawInfo.Center.X - Main.screenPosition.X, drawInfo.Center.Y - Main.screenPosition.Y);

        if (Buffs[AdditionsBuff.EternalRested])
        {
            Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.GlowSoft);
            Vector2 origin = glow.Size() * .5f;
            float size = .5f + (MathF.Cos(Main.GlobalTimeWrappedHourly * 4f) * .2f + .2f);
            drawInfo.DrawDataCache.Add(new DrawData(glow, pos, null, Color.White with { A = 0 }, 0f, origin, size, 0, 0f));
        }
    }
}