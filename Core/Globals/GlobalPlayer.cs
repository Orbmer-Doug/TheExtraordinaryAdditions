using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Tools;
using TheExtraordinaryAdditions.Content.Items.Weapons.Classless;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI;

namespace TheExtraordinaryAdditions.Core.Globals;

public class GlobalPlayer : ModPlayer
{
    internal readonly ReferencedValueRegistry valueRegistry = new();

    public delegate void PlayerActionDelegate(GlobalPlayer p);

    public static event PlayerActionDelegate ResetEffectsEvent;

    public delegate void SaveLoadDataDelegate(GlobalPlayer p, TagCompound tag);

    public delegate void MaxStatsDelegate(GlobalPlayer p, ref StatModifier health, ref StatModifier mana);

    public static event SaveLoadDataDelegate SaveDataEvent;

    public static event SaveLoadDataDelegate LoadDataEvent;

    public static event PlayerActionDelegate PostUpdateEvent;

    public static event MaxStatsDelegate MaxStatsEvent;

    public override void Load()
    {
        for (int i = 0; i < (int)GetLastEnumValue<AdditionsMinion>(); i++)
            Minion[(AdditionsMinion)i] = false;
    }

    public override void Unload()
    {
        PostUpdateEvent = null;
        LoadDataEvent = null;
        SaveDataEvent = null;
        ResetEffectsEvent = null;
        MaxStatsEvent = null;
    }

    public override void SaveData(TagCompound tag)
    {
        // Apply the save data event.
        SaveDataEvent?.Invoke(this, tag);
    }

    public override void LoadData(TagCompound tag)
    {
        // Apply the load data event.
        LoadDataEvent?.Invoke(this, tag);
    }

    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        // Do nothing by default to stats.
        health = StatModifier.Default;
        mana = StatModifier.Default;

        // Apply the stat modification event.
        MaxStatsEvent?.Invoke(this, ref health, ref mana);
    }

    #region Mouse
    public readonly record struct MouseButtonState(bool JustPressed, bool Current, bool JustReleased);

    // Any state the mouse is in
    public MouseButtonState MouseLeft;
    public MouseButtonState MouseRight;
    public MouseButtonState MouseMiddle;

    // Checks for if the mouse is in the world
    public MouseButtonState SafeMouseLeft;
    public MouseButtonState SafeMouseRight;
    public MouseButtonState SafeMouseMiddle;

    /// <summary>
    /// Captures the last 15 positions of the players cursor
    /// </summary>
    public Vector2[] oldMouseWorld = new Vector2[15];

    public Vector2 mouseWorld;
    public Vector2 mouseScreen;

    /// <summary>
    /// The larger this number is the more "fast" the mouse is going
    /// </summary>
    public float oldMouseWorldDistance;

    public bool CanUseMouseButton => !Main.mapFullscreen
        && !Player.mouseInterface && !PlayerInput.WritingText && Main.hasFocus;

    /// <summary>
    /// Syncs the position and right click of the mouse of this player to the server
    /// </summary>
    public bool SyncMouse;

    #endregion Mouse

    #region Buffs
    public enum AdditionsMinion : int
    {
        LaserDrones,
        Loki,
        SuperLoki,
        Avragen,
        peter,
    }
    internal Dictionary<AdditionsMinion, bool> Minion = [];

    public bool CrimsonBlessing;
    public bool HealingArtifact;
    public float healingPotBonus = 1f;
    public bool EternalRested;
    public bool aridFlask;
    public bool frigidTonic;
    public bool DentedBySpoon;
    public bool ashy = false;
    public bool overheat = false;
    public bool BigOxygen = false;
    #endregion Buffs

    #region Equipable
    public bool Peter;
    public bool SpecteriteArmor;
    public bool TremorArmor;
    public bool AshersTie;
    public bool TungstenTie;
    public bool ancientBoon;
    public bool NothingThere;
    public bool AbsoluteArmor;
    public bool RedMist;
    public bool LightSpiritBand;
    public bool FulminicEye;
    public bool FungalSatchel;
    public bool flameInsignia;
    public bool EclipsedOne;
    public bool Nitrogen;
    public bool Cryogenic;
    public bool Auroric;
    #endregion Equipable

    #region Counters
    /// <summary>
    /// Acts as the <see cref="Main.GameUpdateCount"/> for a player without any arbitrary resets
    /// </summary>
    public uint GlobalTimer;
    public int CircuitOverload;
    public int IcyShardWait;
    public int LooseSawbladeCounter;
    public int TieCooldown;
    public int GarciaOverload;
    public float TesselesticHeat;
    public int TremorWait;
    public int NothingThereWait;
    public int BrewingStormsCounter;
    public int EclipsedDuoCounter;
    public int RipperCounter;
    public int MeteorCounter;
    public int LightningRodCount;
    public int CryogenicCounter;
    public int RedMistCounter;
    public int NothingThereCounter;
    public int AbsoluteCounter;
    public int FinalStrikeCounter;
    #endregion Counters

    #region Vanilla
    public float BreakerLimit;
    public const int MaxLimit = 100;
    public int LimitTimer;
    public float CurrentLimit => InverseLerp(0f, MaxLimit, BreakerLimit);
    public bool AtMaxLimit => CurrentLimit == 1f;
    public bool PlayedLimitSound = false;
    public static readonly int MaxTimeWithLimit = SecondsToFrames(15);
    #endregion Vanilla

    #region Misc
    public bool LungingDown;
    public bool teleport;

    public bool crossLightning = false;
    public bool crossIce = false;
    public bool crossFire = false;
    public bool crossWave = false;
    public bool overload;

    public int DummyMaxLife;
    public int DummyDefense;
    public float DummyScale;
    public float DummyRotation;
    public bool DummyGravity;
    public float DummyMoveSpeed;
    public bool DummyBoss;

    #endregion Misc

    public override void OnEnterWorld()
    {
        DummyScale = 1f;
        DummyMoveSpeed = 0f;
        DummyMaxLife = GodDummy.MaxLifeAmount;
        DummyBoss = DummyGravity = false;
        DummyRotation = 0f;
        DummyDefense = 0;
    }

    public override void UpdateDead()
    {
        LungingDown = false;
        flameInsignia = false;
        BigOxygen = false;

        PlayedLimitSound = false;
        BreakerLimit = 0f;
        LimitTimer = 0;

        healingPotBonus = 1f;
        CircuitOverload = 0;
        BrewingStormsCounter = 0;
        EclipsedDuoCounter = 0;
        RipperCounter = 0;
        MeteorCounter = 0;
        AbsoluteCounter = 0;
        LightningRodCount = 0;
        LooseSawbladeCounter = 0;
        CryogenicCounter = 0;
        GarciaOverload = 0;
        RedMistCounter = 0;
        NothingThereCounter = 0;
        NothingThereWait = 0;
        TremorWait = 0;
        FinalStrikeCounter = 0;
        TesselesticHeat = 0f;
    }

    public override void ResetEffects()
    {
        // Apply the reset effects event.
        ResetEffectsEvent?.Invoke(this);

        int percentMaxLifeIncrease = 0;
        if (HealingArtifact)
        {
            percentMaxLifeIncrease += 5;
        }
        if (NothingThere)
        {
            percentMaxLifeIncrease += 10;
        }
        Player.statLifeMax2 += Player.statLifeMax / 5 / 20 * percentMaxLifeIncrease;

        #region SetFalse
        for (int i = 0; i < (int)GetLastEnumValue<AdditionsMinion>(); i++)
            Minion[(AdditionsMinion)i] = false;

        teleport = false;
        DentedBySpoon = false;
        overload = false;
        flameInsignia = false;
        overheat = false;
        ashy = false;
        AshersTie = false;
        TungstenTie = false;
        aridFlask = false;
        SpecteriteArmor = false;
        TremorArmor = false;
        frigidTonic = false;
        healingPotBonus = 1f;
        HealingArtifact = false;
        ancientBoon = false;
        LightSpiritBand = false;
        Peter = false;
        CrimsonBlessing = false;
        NothingThere = false;
        RedMist = false;
        BigOxygen = false;
        EclipsedOne = false;
        Auroric = false;
        EternalRested = false;
        AbsoluteArmor = false;
        Nitrogen = false;
        Cryogenic = false;
        FulminicEye = false;
        FungalSatchel = false;
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

        if (Main.CurrentPlayer.whoAmI == Player.whoAmI)
        {
            TriggersPack trigger = PlayerInput.Triggers;
            MouseLeft = new(trigger.JustPressed.MouseLeft, trigger.Current.MouseLeft, trigger.JustReleased.MouseLeft);
            MouseRight = new(trigger.JustPressed.MouseRight, trigger.Current.MouseRight, trigger.JustReleased.MouseRight);
            MouseMiddle = new(trigger.JustPressed.MouseMiddle, trigger.Current.MouseMiddle, trigger.JustReleased.MouseMiddle);

            SafeMouseLeft = new(
                trigger.JustPressed.MouseLeft && CanUseMouseButton,
                trigger.Current.MouseLeft && CanUseMouseButton,
                trigger.JustReleased.MouseLeft && CanUseMouseButton);

            SafeMouseRight = new(
                trigger.JustPressed.MouseRight && CanUseMouseButton,
                trigger.Current.MouseRight && CanUseMouseButton,
                trigger.JustReleased.MouseRight && CanUseMouseButton);

            SafeMouseMiddle = new(
                trigger.JustPressed.MouseMiddle && CanUseMouseButton,
                trigger.Current.MouseMiddle && CanUseMouseButton,
                trigger.JustReleased.MouseMiddle && CanUseMouseButton);

            mouseScreen = new Vector2(PlayerInput.MouseX, PlayerInput.MouseY);
            Vector2 transform = Vector2.Transform(mouseScreen, Matrix.Invert(Main.GameViewMatrix?.ZoomMatrix ?? Matrix.Identity));
            mouseWorld = transform + Main.screenPosition + (Main.screenPosition - Main.screenLastPosition);
            if (Player.gravDir == -1f)
                mouseWorld.Y = Main.screenPosition.Y + (Main.screenPosition - Main.screenLastPosition).Y + Main.screenHeight - transform.Y;

            oldMouseWorld.CreateTrail(mouseWorld);
            oldMouseWorldDistance = Vector2.Distance(oldMouseWorld[0], oldMouseWorld[^1]) / oldMouseWorld.Length;
        }
    }

    public int actualMaxLife;
    public override void PostUpdateMiscEffects()
    {
        actualMaxLife = Player.statLifeMax2;

        Item item = Player.HeldItem;
        bool damageClass = item.CountsAsClass<RangedDamageClass>() || item.CountsAsClass<MeleeDamageClass>()
            || item.CountsAsClass<MagicDamageClass>() || item.CountsAsClass<ThrowingDamageClass>() || item.CountsAsClass<SummonDamageClass>();

        if ((Nitrogen || Cryogenic) && SafeMouseLeft.Current && damageClass && item.damage > 0)
        {
            IcyShardWait++;
        }

        MiscEffects();
        BuffEffects();
    }

    /// <summary>
    /// Handling assortments of things
    /// </summary>
    public void MiscEffects()
    {
        Item item = Player.HeldItem;
        if ((Nitrogen || Cryogenic) && IcyShardWait >= 20)
        {
            Vector2 pos = Player.RotatedRelativePoint(Player.MountedCenter)
                + PolarVector(10f * Player.direction, Player.fullRotation + MathHelper.Pi)
                + PolarVector(4f * Player.direction * Player.gravDir, Player.fullRotation + MathHelper.PiOver2);
            Vector2 vel = Main.rand.NextVector2CircularEdge(5f, 5f);
            if (Main.myPlayer == Player.whoAmI)
                Player.NewPlayerProj(pos, vel, ModContent.ProjectileType<IcyShards>(), DamageSoftCap(item.damage, 150), 1f, Main.myPlayer, 0f, 0f, 0f);
            IcyShardWait = 0;
        }

        if (FulminicEye)
        {
            NPC n = NPCTargeting.GetClosestNPC(new(Player.Center, 400, true));
            if (n.CanHomeInto() && GlobalTimer % SecondsToFrames(1.25f) == 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, Player.Center);
                Vector2 vel = Player.SafeDirectionTo(n.Center) * 10f;
                if (Main.myPlayer == Player.whoAmI)
                    Player.NewPlayerProj(Player.Center, n.Center.ToRectangle(6, 6).RandomRectangle(), ModContent.ProjectileType<FulminicSpark>(), 30, 1f, Player.whoAmI);

                for (int i = 0; i < 12; i++)
                    ParticleRegistry.SpawnSparkParticle(Player.Center, vel.RotatedByRandom(.22f) * Main.rand.NextFloat(.4f, .8f), 20, Main.rand.NextFloat(.3f, .5f), Color.Purple);
            }
        }

        if (LungingDown)
        {
            Player.maxFallSpeed = 480f;
            Player.noFallDmg = true;
        }

        if (flameInsignia == true)
        {
            const int radius = 180;
            const int rays = 25;
            for (int j = 0; j < rays; j++)
            {
                Vector2 pos = Player.Center + ((MathHelper.TwoPi * j / rays + RandomRotation()).ToRotationVector2() * radius);
                if (Collision.CanHitLine(pos, 1, 1, Player.Center, 1, 1))
                {
                    float angularVelocity = Main.rand.NextFloat(0.045f, 0.09f);
                    float scale = Main.rand.NextFloat(.4f, .7f);
                    Vector2 vel = Player.velocity + (Player.velocity * -.2f);
                    Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
                    ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, Main.rand.Next(20, 24), scale, fireColor, 1f, true, angularVelocity);
                    ParticleRegistry.SpawnGlowParticle(pos, vel + Vector2.UnitY.RotatedByRandom(.25f) * -Main.rand.NextFloat(1f, 4f), Main.rand.Next(20, 30), 50f * scale, fireColor, 1f);
                }
            }

            if (Player.miscCounter % 20f == 19f)
            {
                List<NPC> targets = NPCTargeting.GetNPCsClosestToFarthest(new(Player.Center, radius, true));
                if (targets.Count != 0)
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        NPC target = targets[i];

                        if (!target.CanHomeInto())
                            continue;

                        if (i < 10)
                        {
                            bool active = target.active && target != null;
                            if (active && target.WithinRange(Player.Center, radius) && target.CanBeChasedBy(Player) && target.friendly == false)
                            {
                                int dmg = (int)Player.GetTotalDamage(Player.GetBestClass()).ApplyTo(25);
                                float kb = 0f;
                                int type = ModContent.ProjectileType<InsigniaBlaze>();
                                if (Main.myPlayer == Player.whoAmI)
                                    Player.NewPlayerProj(target.Center, Vector2.Zero, type, dmg, kb, Player.whoAmI, target.whoAmI);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handling typical buff effects
    /// </summary>
    public void BuffEffects()
    {
        if (HealingArtifact)
        {
            healingPotBonus += 0.5f;
        }
    }

    public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
    {
        healValue = (int)(healValue * healingPotBonus);
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

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        OnHitWithAnything(item, target, hit, damageDone);
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        OnHitWithAnything(proj, target, hit, damageDone);
    }

    private void OnHitWithAnything(Entity entity, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (FungalSatchel && hit.Crit && Main.rand.NextBool() && Player.CountOwnerProjectiles(ModContent.ProjectileType<HealingFungus>()) <= 1)
        {
            Vector2 pos = target.RandAreaInEntity();
            Vector2 vel = Utility.GetHomingVelocity(pos, Player.Center, Player.velocity, 6f);
            if (Main.myPlayer == Player.whoAmI)
                Player.NewPlayerProj(pos, vel, ModContent.ProjectileType<HealingFungus>(), 0, 0f, Player.whoAmI);
        }
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        if (ancientBoon)
        {
            if (Main.myPlayer == Player.whoAmI)
            {
                if (info.Damage > 0)
                {
                    float rand = RandomRotation();
                    int dir = Main.rand.NextBool().ToDirectionInt();
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 vel = PolarVector(6f, MathHelper.TwoPi * InverseLerp(0f, 8f, i) + rand);
                        AncientRetaliation proj = Main.projectile[Player.NewPlayerProj(Player.MountedCenter, vel,
                            ModContent.ProjectileType<AncientRetaliation>(), (int)Player.GetTotalDamage<GenericDamageClass>().ApplyTo(800f), 1f, Player.whoAmI)].As<AncientRetaliation>();
                        proj.Direction = dir;
                    }
                }
            }
        }

        if (Player != null && LimitBreakerUI.CurrentlyViewing && BreakerLimit < MaxLimit)
            BreakerLimit += info.Damage * .1f;
    }

    public override void UpdateLifeRegen()
    {
        if (AbsoluteArmor)
        {
            Player.lifeRegenCount += 4;

            while (Player.lifeRegenCount >= 120)
            {
                Player.lifeRegenCount -= 120;

                if (Player.statLife < Player.statLifeMax2)
                {
                    Player.statLife += 2;
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 pos = Player.Center;
                            float lightSpawnOffset = Main.rand.NextFloat(120f, 120f);
                            Vector2 lightSpawnPosition = pos + Main.rand.NextVector2Unit() * lightSpawnOffset;
                            Vector2 lightSpawnVelocity = (pos - lightSpawnPosition) * 0.0411f;
                            float particleScale = Main.rand.NextFloat(.4f, 1.1f);
                            ParticleRegistry.SpawnSparkParticle(lightSpawnPosition, lightSpawnVelocity, 40, particleScale, Color.AntiqueWhite, false, false, pos);
                        }
                    }
                }

                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;
            }
        }
    }

    public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
    {
        // Red mists negation
        if (RedMist)
        {
            modifiers.Knockback *= 0.6f;
            modifiers.KnockbackImmunityEffectiveness *= .15f;
        }

        // funy
        if ((npc.type == NPCID.DemonEye || npc.type == NPCID.BigMimicJungle ||
            npc.type == NPCID.BrainofCthulhu || npc.type == NPCID.Snail ||
            npc.type == NPCID.SolarCrawltipedeHead || npc.type == NPCID.WyvernHead ||
            npc.type == NPCID.Clown || npc.type == NPCID.GiantTortoise ||
            npc.type == NPCID.DuneSplicerHead || npc.type == NPCID.CaveBat ||
            npc.type == NPCID.JungleBat || npc.type == NPCID.Medusa ||
            npc.type == NPCID.MossHornet || npc.type == NPCID.LavaSlime ||
            npc.type == NPCID.Harpy || npc.type == NPCID.Gastropod) && Main.rand.NextBool(80))
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
        //if (mediumCoreDeath)
        {
            itemsByMod["Terraria"].Clear();

            List<Item> items = [new(ItemID.CopperBroadsword), new(ItemID.CopperPickaxe), new(ItemID.CopperAxe),
                new(ItemID.Torch, 15), new(ItemID.RopeCoil, 2), new(ItemID.Cobweb, 6), new(ItemID.BottledWater, 8), new(ItemID.Apple, 2)];

            for (int i = 0; i < items.Count - 1; i++)
            {
                itemsByMod["Terraria"].Add(items[i]);
            }
        }
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

    public static readonly int TimeForCryogenic = SecondsToFrames(10);

    public override void PostUpdate()
    {
        // Apply the post-update event.
        PostUpdateEvent?.Invoke(this);

        GlobalTimer++;

        if (RedMist && RedMistCounter > 0)
            RedMistCounter--;

        if (AbsoluteCounter > 0)
            AbsoluteCounter--;

        if (NothingThere)
        {
            if (NothingThereCounter > 0)
                NothingThereCounter--;
            if (NothingThereWait > 0)
                NothingThereWait--;
        }

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

        if (TremorArmor)
        {
            if (TremorWait > 0)
            {
                Vector2 top = Player.Center + Vector2.UnitY * -10f;
                if (TremorWait % 2f == 1f)
                {
                    float size = 20f + Player.Size.Length();
                    Vector2 stonePos = top + Main.rand.NextVector2CircularLimited(size, size, .5f, 1f);
                    Vector2 stonevel = stonePos.SafeDirectionTo(top) * Main.rand.NextFloat(2f, 4f);
                    Dust.NewDustPerfect(stonePos, DustID.Stone, stonevel, Main.rand.Next(60, 110), default, Main.rand.NextFloat(1f, 1.4f)).noGravity = true;
                }
                Lighting.AddLight(top, Color.Gray.ToVector3() * InverseLerp(0f, SecondsToFrames(5), TremorWait));
                TremorWait--;
            }
        }

        if (Cryogenic)
            CryogenicCounter++;
        else
            CryogenicCounter = 0;
        if (CryogenicCounter > TimeForCryogenic)
        {
            AdditionsSound.ColdHitMassive.Play(Player.Center, .7f, 0f, .1f);
            if (Main.myPlayer == Player.whoAmI)
                Player.NewPlayerProj(Player.Center, Vector2.Zero, ModContent.ProjectileType<CryogenicBlast>(), (int)Player.GetTotalDamage<GenericDamageClass>().ApplyTo(4000), 4f, Player.whoAmI);
            CryogenicCounter = 0;
        }

        // Handle visuals for the crossdisc
        bool active = Player.active && !Player.dead;
        if (active && Player.HeldItem.ModItem is CrossDisc)
        {
            if (crossIce)
            {

            }
            if (crossFire)
            {

            }
            if (crossLightning)
            {

            }
            if (crossWave)
            {

            }
        }

        if (active && Player.velocity.Length() != 0 && !Player.mount.Active)
        {
            Vector2 randPos = Player.RotatedRelativePoint(Player.MountedCenter) + PolarVector(Player.height / 2 * Player.gravDir, Player.fullRotation + MathHelper.PiOver2) + PolarVector(Main.rand.NextFloat(-Player.width / 2, Player.width / 2), Player.fullRotation);

            if (SpecteriteArmor)
            {
                Vector2 vel = -Player.velocity.RotatedByRandom(.18f) * Main.rand.NextFloat(.3f, .5f);
                ParticleRegistry.SpawnMistParticle(randPos, vel, Main.rand.NextFloat(.5f, .8f), new(85, 89, 225), new(8, 35, 97), Main.rand.NextByte(98, 182), .05f);
            }

            if (EclipsedOne)
            {
                Vector2 vel = -Player.velocity.RotatedByRandom(.12f) * Main.rand.NextFloat(.1f, .4f);
                ParticleRegistry.SpawnDustParticle(randPos, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), Color.LightCyan);
            }
        }
    }
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {

    }
    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
    {   
        if (Main.rand.NextBool(60))
            damageSource = PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.TheExtraordinaryAdditions.Status.Death.Silly" + Main.rand.Next(1, 3), Player.name));

        // The ties defiance
        if (TungstenTie && !Player.HasBuff<TheTiesCooldown>() && AshersTie == false)
        {
            AdditionsSound.etherealNuhUh.Play(Player.Center, 1.4f, -.2f);
            for (int l = 0; l < 12; l++)
            {
                ParticleRegistry.SpawnPulseRingParticle(Player.Center, Vector2.Zero, 20, RandomRotation(), new(.5f, 1f), 0f, .15f, Color.DarkGray);
                ParticleRegistry.SpawnSparkParticle(Player.Center, Main.rand.NextVector2CircularLimited(12, 12f, .4f, 1f), 40, Main.rand.NextFloat(.4f, .6f), Color.Gray);
            }
            ParticleRegistry.SpawnThunderParticle(Player.Center, 40, 1.5f, new(1f), 0f, Color.WhiteSmoke);

            Player.Heal(100);
            if (Player.statLife > Player.statLifeMax2)
                Player.statLife = Player.statLifeMax2;

            Player.AddBuff(ModContent.BuffType<TheTiesCooldown>(), SecondsToFrames(80));
            return false;
        }
        if (AshersTie && !Player.HasBuff<TheTiesCooldown>() && TungstenTie == false)
        {
            AdditionsSound.etherealNuhUh.Play(Player.Center);
            for (int l = 0; l < 50; l++)
            {
                Vector2 vel = Main.rand.NextVector2CircularLimited(10f, 10f, .7f, 1f) * Main.rand.NextFloat(.6f, 1f);
                ParticleRegistry.SpawnGlowParticle(Player.Center, vel, 30, Main.rand.NextFloat(.5f, .8f), Color.FloralWhite);
            }
            ParticleRegistry.SpawnThunderParticle(Player.Center, 34, 1f, new(1f), 0f, Color.WhiteSmoke);

            Player.Heal(100);
            if (Player.statLife > Player.statLifeMax2)
                Player.statLife = Player.statLifeMax2;

            Player.AddBuff(ModContent.BuffType<TheTiesCooldown>(), SecondsToFrames(90));
            return false;
        }

        return true;
    }
    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
    {
        Vector2 randHitbox = Player.RandAreaInEntity();
        bool noShadow = drawInfo.shadow == 0f;

        if (CryogenicCounter.BetweenNum(0, TimeForCryogenic) && Cryogenic)
        {
            if (Main.rand.NextBool(5) && noShadow)
            {
                for (int t = 0; t < 2; t++)
                {
                    Vector2 randPos = Main.rand.NextVector2CircularEdge(150f, 150f);
                    Vector2 pos = Player.Center + randPos;
                    Vector2 vel = Player.DirectionFrom(Player.Center + Player.velocity + randPos) * Main.rand.NextFloat(7f, 9f);
                    ParticleRegistry.SpawnSparkParticle(pos, vel, 30, InverseLerp(0f, TimeForCryogenic, CryogenicCounter), Color.DarkSlateBlue, false, false, Player.Center);
                }
            }
        }
        if (CrimsonBlessing)
        {
            if (Main.rand.NextBool(6) && noShadow)
            {
                for (int t = 0; t < 10; t++)
                {
                    Vector2 randPos = Main.rand.NextVector2CircularEdge(150f, 150f);
                    Dust.NewDustPerfect(Player.Center + randPos, DustID.Blood, (Vector2?)(Player.DirectionFrom(Player.Center + Player.velocity + randPos) * Main.rand.NextFloat(7f, 9f)), 0, default, 2f).noGravity = true;
                }
            }
        }

        if (ashy)
        {
            if (Main.rand.NextBool(4) && noShadow)
            {
                int dust12 = Dust.NewDust(drawInfo.Position - new Vector2(2f), Player.width + 4, Player.height + 4, DustID.Ash, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default, 3f);
                Main.dust[dust12].noGravity = true;
                Dust obj14 = Main.dust[dust12];
                obj14.velocity *= 1f;
                Main.dust[dust12].velocity.Y -= 0.5f;
                drawInfo.DustCache.Add(dust12);
            }
            g *= 0.3f;
            r *= 0.52f;
            b *= 0.2f;
        }

        if (overheat && !Player.dead)
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

        if (DentedBySpoon)
        {
            g *= 0.75f;
            r *= 0.0f;
            b *= 0.75f;
        }

        if (overload)
        {
            g *= 0.0f;
            r *= 0.75f;
            b *= 0.0f;
        }
    }

    public override void PostUpdateEquips()
    {
        BaseIdleHoldoutProjectile.CheckForEveryHoldout(Player);
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (AdditionsKeybinds.TeleportHotKey.Current && teleport && Main.myPlayer == Player.whoAmI && !Player.CCed && !Player.chaosState)
        {
            Vector2 teleportLocation = default;
            teleportLocation.X = Main.mouseX + Main.screenPosition.X;
            if (Player.gravDir == 1f)
            {
                teleportLocation.Y = Main.mouseY + Main.screenPosition.Y - Player.height;
            }
            else
            {
                teleportLocation.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY;
            }
            teleportLocation.X -= Player.width / 2;
            if (teleportLocation.X > 50f && teleportLocation.X < Main.maxTilesX * 16 - 50 && teleportLocation.Y > 50f && teleportLocation.Y < Main.maxTilesY * 16 - 50 && !Collision.SolidCollision(teleportLocation, Player.width, Player.height))
            {
                Player.Teleport(teleportLocation, 4, 0);
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, teleportLocation.X, teleportLocation.Y, 1, 0, 0);
                Player.AddBuff(88, 1200);
            }
        }
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        Vector2 pos = new(drawInfo.Center.X - Main.screenPosition.X, drawInfo.Center.Y - Main.screenPosition.Y);

        if (EternalRested)
        {
            Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.GlowSoft);
            Vector2 origin = glow.Size() * .5f;
            float size = .5f + (MathF.Cos(Main.GlobalTimeWrappedHourly * 4f) * .2f + .2f);
            drawInfo.DrawDataCache.Add(new DrawData(glow, pos, null, Color.White with { A = 0 }, 0f, origin, size, 0, 0f));
        }
    }
}