using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Consumable.BossBags;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Items.Equipable.Pets;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Placeable;
using TheExtraordinaryAdditions.Content.Items.Placeable.Base;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;
using TheExtraordinaryAdditions.Content.NPCs.BossesBars;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

[AutoloadBossHead]
public sealed partial class StygainHeart : ModNPC, IBossDowned
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StygainHeart);
    public override string BossHeadTexture => AssetRegistry.GetTexturePath(AdditionsTexture.StygainHeart_Head_Boss);
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 8;
        NPCID.Sets.TrailingMode[NPC.type] = 3;
        NPCID.Sets.TrailCacheLength[NPC.type] = 10;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.UsesNewTargetting[Type] = true;
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Slow & BuffID.Webbed & BuffID.Confused] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            PortraitScale = 0.6f,
            PortraitPositionYOverride = -40
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void FindFrame(int frameHeight)
    {
        if (NPC.frameCounter++ % 10f == 9f)
            NPC.frame.Y += NPC.height;
        if (NPC.frame.Y >= NPC.height * Main.npcFrameCount[Type])
            NPC.frame.Y = 0;
    }

    public override void SetDefaults()
    {
        NPC.npcSlots = 6f;
        NPC.damage = DifficultyBasedValue(160, 250, 300, 370, 360, 400);
        NPC.width = 198;
        NPC.height = 162;
        NPC.defense = 12;
        NPC.SetLifeMaxByMode(100000, 125000, 150000, 175000, 200000);
        NPC.aiStyle = -1;
        AIType = -1;
        NPC.knockBackResist = 0f;
        NPC.canGhostHeal = false;
        NPC.scale = 1f;
        NPC.boss = true;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.HitSound = SoundID.NPCHit18;
        NPC.DeathSound = AssetRegistry.GetSound(AdditionsSound.heartbeat) with { Volume = 5f };
        NPC.value = Item.buyPrice(3, 15, 50, 0) / 5;
        NPC.netAlways = true;
        NPC.BossBar = ModContent.GetInstance<StygainBossbar>();

        if (!Main.dedServ && !Main.gameMenu)
        {
            Music = MusicLoader.GetMusicSlot(Mod, AssetRegistry.GetMusicPath(AdditionsSound.Ladikerfos));
        }
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange((IEnumerable<IBestiaryInfoElement>)(object)new IBestiaryInfoElement[]
        {
            BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.BloodMoon,
            new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
        });
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance);
    }

    public enum StygainAttackType
    {
        SpawnEffects,
        Phase2Drama,
        Charge,
        ChargeWait,
        ShotgunBloodshot,
        PortalSmash,
        Bloodrain,
        Assimilations,
        MoonBarrage,
        BulletTwirl,
        BloodBeacon,
        DieEffects,
    };

    public static StygainAttackType[] Drama =>
    [
        StygainAttackType.Phase2Drama,
    ];

    public static StygainAttackType[] Phase1AttackCycle =>
    [
        StygainAttackType.ShotgunBloodshot,
        StygainAttackType.Charge,
        StygainAttackType.PortalSmash,
        StygainAttackType.Bloodrain,
        StygainAttackType.Assimilations,
        StygainAttackType.Charge,
        StygainAttackType.PortalSmash,
        StygainAttackType.ShotgunBloodshot,
        StygainAttackType.Assimilations,
    ];

    public static StygainAttackType[] Phase2AttackCycle =>
    [
        StygainAttackType.MoonBarrage,
        StygainAttackType.Assimilations,
        StygainAttackType.Charge,
        StygainAttackType.PortalSmash,
        StygainAttackType.ShotgunBloodshot,
        StygainAttackType.Charge,
        StygainAttackType.Bloodrain,
        StygainAttackType.BulletTwirl,
        StygainAttackType.ShotgunBloodshot,
        StygainAttackType.Charge,
        StygainAttackType.ChargeWait,
        StygainAttackType.Assimilations,
        StygainAttackType.MoonBarrage,
        StygainAttackType.BulletTwirl,
        StygainAttackType.PortalSmash,
        StygainAttackType.Assimilations,
        StygainAttackType.ChargeWait,
        StygainAttackType.Charge,
        StygainAttackType.BloodBeacon,
    ];

    public static StygainAttackType[] DieEffect =>
    [
        StygainAttackType.DieEffects,
    ];

    private static NPC myself;
    public static NPC Myself
    {
        get
        {
            if (myself is not null && !myself.active)
                return null;

            return myself;
        }
        internal set => myself = value;
    }

    public static int BloodshotDamage => DifficultyBasedValue(110, 170, 190, 220, 240, 260);
    public static int RadialEyesDamage => DifficultyBasedValue(125, 190, 200, 230, 260, 310);
    public static int BulletTwirlDamage => DifficultyBasedValue(115, 200, 230, 260, 300, 355);
    public static int BloodwavesDamage => DifficultyBasedValue(135, 190, 240, 270, 310, 380);
    public static int BloodBeaconLanceDamage => DifficultyBasedValue(155, 190, 260, 280, 330, 400);
    public static int BloodBeaconDamage => DifficultyBasedValue(350, 400, 430, 480, 560, 1000);
    public static float Phase2LifeRatio => Main.getGoodWorld ? .75f : .5f;

    public const int CurrentStateIndex = 0;
    public const int AttackTimerIndex = 1;
    public const int AttackCycleIndex = 2;
    public const int HasDoneBloodBeaconIndex = 3;
    // Every extra ai above this gets cleared upon choosing the next attack
    public const int HasDoneDramaticBurstIndex = 11;
    public const int HasDonePhase2DramaIndex = 12;
    public const int FogInterpolantIndex = 13;
    public const int StartMakingMassIndex = 14;
    public const int MassTimerIndex = 15;
    public const int MassPositionXIndex = 16;
    public const int MassPositionYIndex = 17;
    public const int MassInitializeIndex = 18;
    public const int MassSpinStartIndex = 19;
    public const int MassSpinDirIndex = 20;

    public StygainAttackType CurrentState
    {
        get => (StygainAttackType)NPC.ai[CurrentStateIndex];
        set => NPC.ai[CurrentStateIndex] = (int)value;
    }
    public int AttackTimer
    {
        get => (int)NPC.ai[AttackTimerIndex];
        set => NPC.ai[AttackTimerIndex] = value;
    }
    public int AttackCycle
    {
        get => (int)NPC.ai[AttackCycleIndex];
        set => NPC.ai[AttackCycleIndex] = value;
    }
    public bool HasDoneBloodBeacon
    {
        get => NPC.ai[HasDoneBloodBeaconIndex] == 1;
        set => NPC.ai[HasDoneBloodBeaconIndex] = value.ToInt();
    }
    public bool HasDoneDramaticBurst
    {
        get => NPC.AdditionsInfo().ExtraAI[HasDoneDramaticBurstIndex] == 1;
        set => NPC.AdditionsInfo().ExtraAI[HasDoneDramaticBurstIndex] = value.ToInt();
    }
    public bool HasDonePhase2Drama
    {
        get => NPC.AdditionsInfo().ExtraAI[HasDonePhase2DramaIndex] == 1;
        set => NPC.AdditionsInfo().ExtraAI[HasDonePhase2DramaIndex] = value.ToInt();
    }
    public ref float FogInterpolant => ref NPC.AdditionsInfo().ExtraAI[FogInterpolantIndex];
    public bool StartMakingMass
    {
        get => NPC.AdditionsInfo().ExtraAI[StartMakingMassIndex] == 1;
        set => NPC.AdditionsInfo().ExtraAI[StartMakingMassIndex] = value.ToInt();
    }
    public int MassTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[MassTimerIndex];
        set => NPC.AdditionsInfo().ExtraAI[MassTimerIndex] = value;
    }
    public Vector2 MassPosition
    {
        get => new Vector2(NPC.AdditionsInfo().ExtraAI[MassPositionXIndex], NPC.AdditionsInfo().ExtraAI[MassPositionYIndex]);
        set
        {
            NPC.AdditionsInfo().ExtraAI[MassPositionXIndex] = value.X;
            NPC.AdditionsInfo().ExtraAI[MassPositionYIndex] = value.Y;
        }
    }
    public bool MassInitialize
    {
        get => NPC.AdditionsInfo().ExtraAI[MassInitializeIndex] == 1;
        set => NPC.AdditionsInfo().ExtraAI[MassInitializeIndex] = value.ToInt();
    }
    public ref float MassSpinStart => ref NPC.AdditionsInfo().ExtraAI[MassSpinStartIndex];
    public int MassSpinDir
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[MassSpinDirIndex];
        set => NPC.AdditionsInfo().ExtraAI[MassSpinDirIndex] = value;
    }

    public bool[] Directions = new bool[8];

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((bool)canDespawn);
        writer.Write((int)despawnTimer);
        writer.Write((bool)NPC.dontTakeDamage);

        for (int i = 0; i < Directions.Length; i++)
            writer.Write((bool)Directions[i]);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        canDespawn = (bool)reader.ReadBoolean();
        despawnTimer = (int)reader.ReadInt32();
        NPC.dontTakeDamage = (bool)reader.ReadBoolean();

        for (int i = 0; i < Directions.Length; i++)
            Directions[i] = (bool)reader.ReadBoolean();
    }

    public override void AI()
    {
        Player target = Main.player[NPC.target];

        Myself = NPC;
        if (Myself == null)
            return;

        Afterimages ??= new(10, () => NPC.Center);

        DetermineTarget(NPC, target);
        float lifeRatio = NPC.life / (float)NPC.lifeMax;
        bool phase2 = lifeRatio < Phase2LifeRatio;

        if (NPC.life == 1000 && !NPC.dontTakeDamage)
        {
            ClearAllProjectiles();
            NPC.dontTakeDamage = true;
            CurrentState = StygainAttackType.DieEffects;
            NPC.netUpdate = true;
        }

        // Give boss effects
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (!p.active || p.dead)
                continue;

            p.GrantBossEffectsBuff();
        }

        NPC.damage = NPC.defDamage;
        NPC.defense = NPC.defDefense;

        switch (CurrentState)
        {
            case StygainAttackType.SpawnEffects:
                DoAttack_SpawnEffects(target);
                break;
            case StygainAttackType.Charge:
                DoAttack_Charge(target, phase2);
                break;
            case StygainAttackType.ChargeWait:
                Do_Attack_ChargeWait(target, phase2);
                break;
            case StygainAttackType.ShotgunBloodshot:
                DoAttack_ShotgunBloodshot(target, phase2);
                break;
            case StygainAttackType.PortalSmash:
                DoAttack_PortalSmash(target);
                break;
            case StygainAttackType.Bloodrain:
                DoAttack_Bloodrain(target, phase2);
                break;
            case StygainAttackType.Assimilations:
                DoAttack_Assimilations(target, phase2);
                break;
            case StygainAttackType.Phase2Drama:
                DoBehavior_Phase2Drama(target);
                break;
            case StygainAttackType.MoonBarrage:
                DoAttack_MoonBarrage(target, phase2);
                break;
            case StygainAttackType.BulletTwirl:
                DoAttack_DartCyclone(target);
                break;
            case StygainAttackType.BloodBeacon:
                DoAttack_BloodBeacon(target);
                break;
            case StygainAttackType.DieEffects:
                DoBehavior_DeathEffects(target);
                break;
        }

        Afterimages?.UpdateFancyAfterimages(new(NPC.Center, NPC.scale * Vector2.One, NPC.Opacity, NPC.rotation,
            NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 255, 0, 0f, NPC.frame, false));

        if (StartMakingMass)
            SummonMass();

        if (HasDonePhase2Drama)
        {
            FogInterpolant = MathHelper.Clamp(FogInterpolant + 0.02f, 0f, .67f);
        }

        AttackTimer++;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        // Has knockback negating effects in global player, just to disorient them
        if (hurtInfo.Damage > 30)
        {
            target.AddBuff(BuffID.Darkness, 210, true, false);
            target.AddBuff(BuffID.Bleeding, 210, true, false);
        }

        if (target.HasBuff(ModContent.BuffType<HemorrhageTransfer>()))
        {
            NPC.NewNPCProj(target.Center, Vector2.Zero, ModContent.ProjectileType<BloodletRelay>(), 0, 0f, hurtInfo.Damage * .25f);
        }

        for (int i = 0; i <= 3; i++)
        {
            ParticleRegistry.SpawnBloodParticle(target.Center, NPC.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.25f) * Main.rand.NextFloat(.8f, 1.8f), Main.rand.Next(30, 40), Main.rand.NextFloat(.5f, .8f), Color.DarkRed);
        }
    }

    public static Dictionary<StygainAttackType[], Func<NPC, bool>> SubphaseTable => new()
    {
        [Phase1AttackCycle] = (npc) => (npc.life / (float)npc.lifeMax) > Phase2LifeRatio,
        [Drama] = (npc) => (npc.life / (float)npc.lifeMax) < Phase2LifeRatio && !npc.As<StygainHeart>().HasDonePhase2Drama,
        [Phase2AttackCycle] = (npc) => (npc.life / (float)npc.lifeMax) < Phase2LifeRatio && npc.life > 1000,
        [DieEffect] = (npc) => npc.life.BetweenNum(1, 1000, true),
    };

    public void SelectNextAttack()
    {
        StygainAttackType[] patternToUse = SubphaseTable.First(table => table.Value(NPC)).Key;
        StygainAttackType nextAttackType = patternToUse[AttackCycle % patternToUse.Length];

        // Attack Cycle Index
        AttackCycle++;

        // Re-target players
        NPC.TargetClosest();

        CurrentState = nextAttackType;
        AttackTimer = 0;

        // Misc slots
        for (int i = 0; i < 10; i++)
            NPC.AdditionsInfo().ExtraAI[i] = 0f;

        // Make a mass after every 4 attacks
        if (Utility.CountNPCs(ModContent.NPCType<CoalescentMass>()) < 3 && AttackCycle % 4 == 3)
        {
            StartMakingMass = true;
        }

        NPC.netUpdate = true;
        if (NPC.netSpam > 10)
            NPC.netSpam = 10;
    }

    private static int despawnTimer = 120;
    private static bool canDespawn;
    public static void DetermineTarget(NPC npc, Player target)
    {
        Vector2 vectorCenter = npc.Center;
        if (!target.active || target.dead || Vector2.Distance(target.Center, vectorCenter) > 5600f || Main.dayTime)
        {
            npc.TargetClosest(false);
            target = Main.player[npc.target];
            if ((!target.active || target.dead || Main.dayTime || Vector2.Distance(target.Center, vectorCenter) > 5600f) && despawnTimer > 0)
            {
                despawnTimer--;
            }
        }
        else
        {
            despawnTimer = 120;
        }
        canDespawn = despawnTimer <= 0;
        if (canDespawn)
        {
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - .3f, -50f, 30f);
            if (npc.timeLeft > 60)
                npc.timeLeft = 60;
            npc.Opacity = InverseLerp(0f, 60f, npc.timeLeft);

            if (npc.ai[0] != -1f)
            {
                npc.ai[0] = -1f;
                npc.ai[1] = 0f;
                npc.ai[2] = 0f;
                npc.netUpdate = true;
            }
            return;
        }
    }

    public override bool CheckActive()
    {
        return canDespawn;
    }

    public override bool CheckDead()
    {
        if (CurrentState == StygainAttackType.DieEffects)
            return true;

        NPC.life = 1000;
        NPC.dontTakeDamage = true;
        NPC.netUpdate = true;
        return false;
    }

    public FancyAfterimages Afterimages;
    public override bool PreDraw(SpriteBatch sb, Vector2 screenPos, Color drawColor)
    {
        if (NPC.IsABestiaryIconDummy)
            return true;

        Texture2D texture = NPC.ThisNPCTexture();
        Vector2 drawPosition = NPC.Center;
        SpriteEffects direction = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Vector2 origin = NPC.frame.Size() * .5f;

        bool die = CurrentState == StygainAttackType.DieEffects;
        float deathFade = 1f - InverseLerp(0f, 60f, AttackTimer);

        // Make a menacing rotating aura
        Color backglow = Color.DarkRed;
        Vector2 offsets = new Vector2(0f, NPC.gfxOffY) - Main.screenPosition;
        Vector2 drawStartOuter = offsets + NPC.Center;
        Vector2 spinPoint = -Vector2.UnitY * 12f;
        float timer = AttackTimer % 216000f / 60f;
        float rotation = MathHelper.TwoPi * timer / 3;

        if (die)
        {
            backglow *= deathFade;
            spinPoint *= deathFade;
        }

        for (int i = 0; i < 12; i++)
        {
            Vector2 spinStart = drawStartOuter + Utils.RotatedBy(spinPoint, (double)(rotation - (float)Math.PI * i / 6f), default);
            Color glowAlpha = NPC.GetAlpha(backglow * NPC.Opacity);
            glowAlpha.A = (byte)(72 * deathFade);
            sb.Draw(texture, spinStart, NPC.frame, glowAlpha * .5f, NPC.rotation, origin, NPC.scale, direction, 0f);
        }

        // Create afterimages when necessary
        Afterimages?.DrawFancyAfterimages(texture, [Color.Black, Color.DarkRed, Color.Crimson, Color.Red], Animators.MakePoly(3f).InFunction(InverseLerp(10f, 16f, NPC.velocity.Length())));

        // Create a pulse anytime after blood beacon
        if (HasDoneBloodBeacon)
        {
            float pulse = (float)Math.Cos(MathHelper.PiOver2 * Main.GlobalTimeWrappedHourly * 2f) + (float)Math.Cos(Math.E * Main.GlobalTimeWrappedHourly * 1.7);
            pulse = pulse * 0.25f + 0.5f;
            pulse = (float)Math.Pow(pulse, 3.0);
            Color drawCol = Color.Lerp(Color.DarkRed, Color.Crimson, pulse);
            drawCol *= MathHelper.Lerp(0.45f, 0.77f, Convert01To010(pulse));

            float time = Main.GlobalTimeWrappedHourly * 10f % 10f / 10f;
            float scale = NPC.scale + time * 2f;
            sb.DrawBetter(texture, drawPosition, NPC.frame, drawCol * MathHelper.Lerp(0.7f, 0f, time), NPC.rotation, origin, scale, direction);
        }

        // Draw the base boss
        if (die)
        {
            sb.EnterShaderRegion();
            ManagedShader fade = AssetRegistry.GetShader("StygainDisintegration");
            fade.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.noise), 1);
            fade.TrySetParameter("opacity", 1.1f - InverseLerp(0f, deathAnimTime, AttackTimer));
            fade.Render();
        }

        Main.spriteBatch.DrawBetter(texture, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, direction);

        if (die)
            sb.ExitShaderRegion();

        return false;
    }

    public override void BossHeadRotation(ref float rotation) => rotation = NPC.rotation;
    public override void BossHeadSpriteEffects(ref SpriteEffects spriteEffects) => spriteEffects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

    public override void OnKill()
    {
        AdditionsNetcode.SyncWorld();
    }

    public override void BossLoot(ref int potionType)
    {
        potionType = ItemID.GreaterHealingPotion;
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<TreasureBagStygainHeart>()));
        npcLoot.Add(ModContent.ItemType<StygainHeartTrophy>(), 1);
        npcLoot.Add(ModContent.ItemType<StygianEyeball>(), 1);
        npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<CrimsonCalamari>()));

        LeadingConditionRule normalOnly = npcLoot.DefineNormalOnlyDropSet();
        int[] weapons =
        [
            ModContent.ItemType<Sangue>(),
            ModContent.ItemType<HemoglobbedCapsule>(),
            ModContent.ItemType<LanceOfSanguineSteels>(),
            ModContent.ItemType<Exsanguination>(),
        ];
        normalOnly.Add(DropHelper.CalamityStyle(DropHelper.NormalWeaponDropRateFraction, weapons));

        int[] armor =
        [
            ModContent.ItemType<RedMistHelmet>(),
            ModContent.ItemType<NothingThereHelmet>(),
            ModContent.ItemType<MimicryChestplate>(),
            ModContent.ItemType<MimicryLeggings>()
        ];
        normalOnly.Add(DropHelper.CalamityStyle(DropHelper.NormalWeaponDropRateFraction, armor));
    }
}

public sealed class StygainGlobalPlayer : ModPlayer
{
    public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
    {
        if (Utility.CountNPCs(ModContent.NPCType<CoalescentMass>()) > 0 && npc.type == ModContent.NPCType<StygainHeart>())
        {
            modifiers.Knockback *= 2.7f;
            modifiers.KnockbackImmunityEffectiveness *= 0f;
        }
    }
}