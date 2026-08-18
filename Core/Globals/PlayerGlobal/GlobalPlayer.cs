using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI;

namespace TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

public sealed class GlobalPlayer : ModPlayer
{
    public delegate void PlayerActionDelegate(GlobalPlayer p);

    public static event PlayerActionDelegate ResetEffectsEvent;

    public static event PlayerActionDelegate PostUpdateEvent;

    public delegate void MaxStatsDelegate(GlobalPlayer p, ref StatModifier health, ref StatModifier mana);

    public static event MaxStatsDelegate MaxStatsEvent;

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

    public float BreakerLimit;
    public const int MaxLimit = 100;
    public int LimitTimer;
    public float CurrentLimit => InverseLerp(0f, MaxLimit, BreakerLimit);
    public bool AtMaxLimit => CurrentLimit == 1f;
    public bool PlayedLimitSound;
    public static readonly int MaxTimeWithLimit = SecondsToFrames(15);

    public override void UpdateDead()
    {
        PlayedLimitSound = false;
        BreakerLimit = 0f;
        LimitTimer = 0;
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
    }

    public static bool HasDamageClass(Player player)
    {
        Item item = player.HeldItem;
        return item.CountsAsClass<RangedDamageClass>() || item.CountsAsClass<MeleeDamageClass>()
                                                       || item.CountsAsClass<MagicDamageClass>() ||
                                                       item.CountsAsClass<ThrowingDamageClass>()
                                                       || item.CountsAsClass<SummonDamageClass>();
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
        if (npc.type is NPCID.DemonEye or NPCID.BigMimicJungle or NPCID.BrainofCthulhu or NPCID.Snail
                or NPCID.SolarCrawltipedeHead or NPCID.WyvernHead or NPCID.Clown or NPCID.GiantTortoise
                or NPCID.DuneSplicerHead or NPCID.CaveBat or NPCID.JungleBat or NPCID.Medusa or NPCID.MossHornet
                or NPCID.LavaSlime or NPCID.Harpy or NPCID.Gastropod && Main.rand.NextBool(80) && Main.zenithWorld)
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

    public override void PostUpdateBuffs()
    {
        if (LimitBreakerUI.CurrentlyViewing)
        {
            if (AtMaxLimit)
            {
                Player.moveSpeed += .5f;
                Player.fallStart = (int) (Player.position.Y / 60f /*16f*/);
                Player.maxFallSpeed = 20f /*10f*/;
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
                    AssetRegistry.GennedSounds.BreakerCapped.Play(Player.Center);
                    PlayedLimitSound = true;
                }

                BreakerLimit = MaxLimit;
            }
            else
                PlayedLimitSound = false;
        }
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore,
        ref PlayerDeathReason damageSource)
    {
        if (Main.rand.NextBool(60))
            damageSource = PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                "Mods.TheExtraordinaryAdditions.Status.Death.Silly" + Main.rand.Next(1, 3), Player.name));
        return true;
    }
}
