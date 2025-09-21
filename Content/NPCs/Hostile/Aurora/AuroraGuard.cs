using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Placeable.Banners;
using TheExtraordinaryAdditions.Content.Items.Placeable.Base;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.AuroraTurret;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

public partial class AuroraGuard : ModNPC, IBossDowned
{
    #region Defaults/Variables
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AuroraTurretHead);

    public override void SetStaticDefaults()
    {
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Slow & BuffID.Webbed] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            CustomTexturePath = AssetRegistry.GetTexturePath(AdditionsTexture.AuroraGuardBestiary),
            PortraitScale = 0.6f,
            PortraitPositionYOverride = 0f,
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
    }

    public override void SetDefaults()
    {
        NPC.width = 50;
        NPC.height = 32;
        NPC.defense = 40;
        NPC.lifeMax = 14000;

        NPC.aiStyle = AIType = -1;
        NPC.knockBackResist = 0f;
        NPC.value = Item.buyPrice(0, 5, 0, 0);
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.scale = 1f;

        NPC.npcSlots = 3;
        NPC.rarity = 4;
        NPC.netAlways = true;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<AuroraTurretBanner>();
        Music = -1;
        SceneEffectPriority = SceneEffectPriority.Environment;
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
        {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,
                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
        });
    }

    public enum AttackState
    {
        /// <summary>
        /// chillin (literally)
        /// </summary>
        Idle,

        /// <summary>
        /// angry
        /// </summary>
        Awaken,

        /// <summary>
        /// Scuttles to the player while machine gunning icicles
        /// </summary>
        Skittering,

        /// <summary>
        /// Slows to a stop whilst charging a large blast from the cannon
        /// </summary>
        ChargeAndBlast,

        /// <summary>
        /// Summons big glaciers which erupt out the ground
        /// </summary>
        IceSkewers,

        /// <summary>
        /// Death animation
        /// </summary>
        GoKablooey,

        /// <summary>
        /// Leave animation
        /// </summary>
        FadeAway,
    }

    /// <summary>
    /// Simply so that the sound position can be updated
    /// </summary>
    public SlotId DeathSoundSlot;

    #region Balancing
    public static readonly int IcicleDamage = DifficultyBasedValue(90, 130, 150, 200, 230, 250);
    public static readonly int HeavyBlastDamage = DifficultyBasedValue(90, 130, 150, 200, 230, 250);
    public static readonly int SkewerDamage = DifficultyBasedValue(90, 130, 150, 200, 230, 250);

    public static readonly int MaxStress = DifficultyBasedValue(100, 120, 140, 160, 180, 200);
    public static readonly int TimeOverwhelmed = DifficultyBasedValue(SecondsToFrames(4f), SecondsToFrames(3.5f), SecondsToFrames(3.25f), SecondsToFrames(3f), SecondsToFrames(2.75f), SecondsToFrames(2.5f));

    public static readonly int ShootTime = SecondsToFrames(5f);
    public static readonly int ShootWait = DifficultyBasedValue(10, 9, 7, 6, 5, 4);
    public static readonly float ShootSpeed = DifficultyBasedValue(8f, 10f, 12f, 14f, 15f, 16f);

    public static readonly int TimeForBigShot = DifficultyBasedValue(SecondsToFrames(3.1f), SecondsToFrames(2.9f), SecondsToFrames(2.74f), SecondsToFrames(2.67f), SecondsToFrames(2.6f), SecondsToFrames(2.4f));
    public static readonly int TimeToRise = DifficultyBasedValue(SecondsToFrames(2.1f), SecondsToFrames(1.9f), SecondsToFrames(1.8f), SecondsToFrames(1.67f), SecondsToFrames(1.5f), SecondsToFrames(1.4f));
    public static readonly int SkewerWait = DifficultyBasedValue(70, 60, 50, 40, 30, 20);
    #endregion

    public static readonly int FirstBreak = SecondsToFrames(0f);
    public static readonly int SecondBreak = SecondsToFrames(.850f);
    public static readonly int FinalBreak = SecondsToFrames(1.7f);
    public static readonly int Scream = SecondsToFrames(3.2f);
    public static readonly int AwakenTime = SecondsToFrames(5f);

    public static readonly int KablooeyMarker = SecondsToFrames(7.462f);

    public static readonly Color SlateBlue = new(112, 128, 144);
    public static readonly Color MauveBright = new(147, 143, 173);
    public static readonly Color Lavender = new(173, 151, 189);
    public static readonly Color PastelViolet = new(186, 135, 209);
    public static readonly Color BrightViolet = new(204, 102, 255);

    public static readonly Color Icey = new(164, 235, 255);
    public static readonly Color LightCornflower = new(123, 191, 248);
    public static readonly Color DarkSlateBlue = new(44, 102, 181);
    public static readonly Color DeepBlue = new(14, 32, 168);

    public int AttackTimer
    {
        get => (int)NPC.ai[0];
        set => NPC.ai[0] = value;
    }
    public AttackState CurrentState
    {
        get => (AttackState)NPC.ai[1];
        set => NPC.ai[1] = (int)value;
    }
    public int GlacierIndex
    {
        get => (int)NPC.ai[2];
        set => NPC.ai[2] = value;
    }
    public ref float SpeedMultiplier => ref NPC.AdditionsInfo().ExtraAI[0];
    public ref float BarrelHeat => ref NPC.AdditionsInfo().ExtraAI[1];
    public ref float HeadRotation => ref NPC.AdditionsInfo().ExtraAI[2];
    public ref float Recoil => ref NPC.AdditionsInfo().ExtraAI[3];
    public ref float BodyRotation => ref NPC.AdditionsInfo().ExtraAI[4];
    public SpriteEffects HeadFlip
    {
        get => (SpriteEffects)NPC.AdditionsInfo().ExtraAI[5];
        set => NPC.AdditionsInfo().ExtraAI[5] = (int)value;
    }
    public SpriteEffects BodyFlip
    {
        get => (SpriteEffects)NPC.AdditionsInfo().ExtraAI[6];
        set => NPC.AdditionsInfo().ExtraAI[6] = (int)value;
    }
    public ref float VerticalVisualOffset => ref NPC.AdditionsInfo().ExtraAI[7];
    public Vector2 GlacierPosition
    {
        get => new Vector2(NPC.AdditionsInfo().ExtraAI[8], NPC.AdditionsInfo().ExtraAI[9]);
        set
        {
            NPC.AdditionsInfo().ExtraAI[8] = value.X;
            NPC.AdditionsInfo().ExtraAI[9] = value.Y;
        }
    }
    public Vector2 StruggleDir
    {
        get => new Vector2(NPC.AdditionsInfo().ExtraAI[10], NPC.AdditionsInfo().ExtraAI[11]);
        set
        {
            NPC.AdditionsInfo().ExtraAI[10] = value.X;
            NPC.AdditionsInfo().ExtraAI[11] = value.Y;
        }
    }
    public ref float StruggleDist => ref NPC.AdditionsInfo().ExtraAI[12];
    public Vector2 StrugglePos
    {
        get => new Vector2(NPC.AdditionsInfo().ExtraAI[13], NPC.AdditionsInfo().ExtraAI[14]);
        set
        {
            NPC.AdditionsInfo().ExtraAI[13] = value.X;
            NPC.AdditionsInfo().ExtraAI[14] = value.Y;
        }
    }
    public int StruggleSign
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[15];
        set => NPC.AdditionsInfo().ExtraAI[15] = value;
    }
    public ref float BreakCompletion => ref NPC.AdditionsInfo().ExtraAI[16];
    public ref float KineticForce => ref NPC.AdditionsInfo().ExtraAI[17];
    public Movements Movement
    {
        get => (Movements)NPC.AdditionsInfo().ExtraAI[18];
        set => NPC.AdditionsInfo().ExtraAI[18] = (int)value;
    }

    public Vector2 HeadCenter => VisualCenter + PolarVector(-28f, BodyRotation + PiOver2) + PolarVector(Recoil, HeadRotation - Pi);
    public RotatedRectangle HeadRect => new(74f, HeadCenter + PolarVector(63f, HeadRotation - Pi), HeadCenter + PolarVector(63f, HeadRotation));
    public Vector2 GunPos => HeadCenter + PolarVector(22f, HeadRotation);
    public Vector2 VisualCenter => NPC.Center + (Vector2.UnitY * VerticalVisualOffset) - Vector2.UnitY * 10f;
    public static int CollisionBoxWidth => 50;
    public int CollisionBoxHeight => NPC.height + CollisionBoxYOffset;
    public int CollisionBoxYOffset => 80;
    public Vector2 CollisionBoxOrigin => NPC.Top - Vector2.UnitX * (CollisionBoxWidth / 2);

    public Rectangle CollisionBox
    {
        get
        {
            Vector2 collisionBoxOrigin = NPC.Bottom - Vector2.UnitX * (CollisionBoxWidth / 2) - Vector2.UnitY * CollisionBoxHeight;
            return new Rectangle((int)collisionBoxOrigin.X, (int)collisionBoxOrigin.Y, CollisionBoxWidth, CollisionBoxHeight);
        }
    }

    public float FloorHeight => NPC.Bottom.Y + CollisionBoxYOffset;
    public Vector2 FloorPosition => NPC.Bottom + Vector2.UnitY * CollisionBoxYOffset;
    public static readonly List<AStarNeighbour> TurretStride = AStarNeighbour.BigOmniStride(6, 8);
    public List<AuroraGuardLeg> Legs;
    public List<Vector2> IdealStepPositions;
    public List<Vector2> PreviousIdealStepPositions;
    public Player Target => Main.player[NPC.target];

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((bool)NPC.dontTakeDamage);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.dontTakeDamage = (bool)reader.ReadBoolean();
    }

    public override void Load()
    {
        On_Main.UpdateAudio_DecideOnNewMusic += NotBossButHaveMusic;
    }

    public override void Unload()
    {
        On_Main.UpdateAudio_DecideOnNewMusic -= NotBossButHaveMusic;
    }

    private static void NotBossButHaveMusic(On_Main.orig_UpdateAudio_DecideOnNewMusic orig, Main self)
    {
        orig(self);
        if (Utility.FindNPC(out NPC npc, ModContent.NPCType<AuroraGuard>()))
        {
            if (npc.ai[1] != (int)AttackState.Idle)
                Main.newMusic = MusicLoader.GetMusicSlot(AdditionsMain.Instance, AssetRegistry.GetMusicPath(AdditionsSound.FrigidGale));
        }
    }

    #endregion

    #region AI
    public override void OnSpawn(IEntitySource source)
    {
        GlacierIndex = -1;
        if (!Target.active || Target.dead)
        {
            NPC.TargetClosest(false);
        }

        if (!Main.dedServ)
            InitializeLimbs();
        NPC.netUpdate = true;
    }

    public override void AI()
    {
        PlayerTargeting.SearchForTarget(NPC, NPC.GetTargetData());

        SpeedMultiplier = 1f;
        HijackIntoDeathAnim();

        switch (CurrentState)
        {
            case AttackState.Idle:
                DoBehavior_Idle();
                break;
            case AttackState.Awaken:
                DoBehavior_Awaken();
                break;
            case AttackState.Skittering:
                DoBehavior_Skittering();
                break;
            case AttackState.ChargeAndBlast:
                DoBehavior_ChargeAndBlast();
                break;
            case AttackState.IceSkewers:
                DoBehavior_IceSkewers();
                break;
            case AttackState.GoKablooey:
                DoBehavior_GoKablooey();
                break;
            case AttackState.FadeAway:
                break;
        }

        AttackTimer++;
        if (CurrentState != AttackState.Idle)
        {
            UpdateMovement();
        }

        if (!Main.dedServ)
        {
            SimulateLimbs();
            UpdateVisuals();
        }
    }

    public void HijackIntoDeathAnim()
    {
        if (NPC.life <= 1 && CurrentState != AttackState.GoKablooey)
        {
            NPC.dontTakeDamage = true;
            AttackTimer = 0;
            CurrentState = AttackState.GoKablooey;
            NPC.netUpdate = true;
        }
    }

    public void SelectNextAttack(bool condition, AttackState to)
    {
        if (condition)
        {
            NPC.TargetClosest(false);
            AttackTimer = 0;
            CurrentState = to;
            this.Sync();
        }
    }

    public void DoBehavior_Idle()
    {
        if (AttackTimer == 0)
        {
            if (this.RunServer())
                HeadRotation = RandomRotation();
            NPC.Center = FindNearestSurface(NPC.Center, true, Main.bottomWorld, 100, true).Value;
            GlacierPosition = FindNearestSurface(NPC.Center, true, 1000f, 1).Value - Vector2.UnitY * 80f;
            NPC.dontTakeDamage = true;
            NPC.netUpdate = true;
        }

        if (this.RunServer())
        {
            int type = ModContent.NPCType<EncasingGlacier>();
            if (GlacierIndex == -1)
            {
                GlacierIndex = NPC.NewNPCBetter(NPC.Center, Vector2.Zero, type, 0, NPC.whoAmI, 0f, 0f, 0f, -1);
                NPC.netUpdate = true;
            }
        }

        SelectNextAttack(GlacierIndex >= 0 && GlacierIndex < Main.maxNPCs && Main.npc[GlacierIndex].ai[3] == 1, AttackState.Awaken);
    }

    public Rectangle GlacierHitbox => GlacierPosition.ToRectangle(132, 180);

    public const float MaxRadius = PiOver4 - .3f;

    private void SpawnBreaks(float power = 1f, int amt = 3)
    {
        for (int i = 0; i < amt; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                Vector2 pos = GlacierHitbox.RandomRectangle();
                Vector2 vel = -pos.SafeDirectionTo(GlacierHitbox.Center()) * Main.rand.NextFloat(4f, 6f);
                ParticleRegistry.SpawnBloomLineParticle(pos, vel, Main.rand.Next(10, 14), Main.rand.NextFloat(.3f, .5f), Color.LightSkyBlue);
            }

            for (int k = 0; k < 4; k++)
                ParticleRegistry.SpawnDustParticle(GlacierHitbox.RandomRectangle(), Vector2.UnitY, Main.rand.Next(30, 60), Main.rand.NextFloat(.4f, .7f), Icey, .1f, true, true);
            
            if (!Main.dedServ)
            {
                Gore.NewGorePerfect(NPC.GetSource_FromAI(), GlacierHitbox.RandomRectangle(), StruggleDir.RotatedByRandom(.4f) * Main.rand.NextFloat(3f, 5f) * power,
                    Mod.Find<ModGore>($"GlacierBreak{Main.rand.Next(1, 5)}").Type);
            }
        }
    }

    public void DoBehavior_Awaken()
    {
        if (AttackTimer == 1)
        {
            NPC.Center = FindNearestSurface(NPC.Center, true, 2000f, 100, true).Value;
            GlacierPosition = FindNearestSurface(NPC.Center, true, 1000f, 10).Value - Vector2.UnitY * 80f;
            AdditionsSound.AuroraRise.Play(NPC.Center, 1f, 0f, 0f, 1, null, PauseBehavior.PauseWithGame);

            if (this.RunServer())
            {
                StruggleSign = Main.rand.NextFromList(-1, 1);
                StruggleDir = StruggleSign == -1 ? -Vector2.UnitY.RotatedBy(-MaxRadius).RotatedByRandom(MaxRadius) : -Vector2.UnitY.RotatedBy(MaxRadius).RotatedByRandom(MaxRadius);
                StruggleDist = Main.rand.NextFloat(20f, 30f);
            }
            StrugglePos = NPC.Center + StruggleDir * StruggleDist;

            ScreenShakeSystem.New(new(.1f, .3f, 1500f), NPC.Center);
            SpawnBreaks();
            BreakCompletion = .4f;
            NPC.netUpdate = true;
        }
        else if (AttackTimer < SecondBreak)
        {
            NPC.Center = Vector2.Lerp(NPC.Center, StrugglePos, MakePoly(3f).OutFunction(InverseLerp(FirstBreak, SecondBreak, AttackTimer)));
        }
        else if (AttackTimer == SecondBreak)
        {
            StruggleSign = -StruggleSign;

            if (this.RunServer())
            {
                StruggleDir = StruggleSign == -1 ? -Vector2.UnitY.RotatedBy(-MaxRadius).RotatedByRandom(MaxRadius) : -Vector2.UnitY.RotatedBy(MaxRadius).RotatedByRandom(MaxRadius);
                StruggleDist = Main.rand.NextFloat(20f, 30f);
            }

            StrugglePos = NPC.Center + StruggleDir * StruggleDist;
            ScreenShakeSystem.New(new(.4f, .5f, 1500f), NPC.Center);
            SpawnBreaks(1.3f, 4);
            BreakCompletion = .8f;
            NPC.netUpdate = true;
        }
        else if (AttackTimer < FinalBreak)
        {
            NPC.Center = Vector2.Lerp(NPC.Center, StrugglePos, MakePoly(6f).OutFunction(InverseLerp(SecondBreak, FinalBreak, AttackTimer)));
        }
        else if (AttackTimer == FinalBreak)
        {
            StruggleSign = -StruggleSign;

            if (this.RunServer())
            {
                StruggleDir = StruggleSign == -1 ? -Vector2.UnitY.RotatedBy(-MaxRadius).RotatedByRandom(MaxRadius) : -Vector2.UnitY.RotatedBy(MaxRadius).RotatedByRandom(MaxRadius);
                StruggleDist = Main.rand.NextFloat(50f, 60f);
            }

            StrugglePos = NPC.Center + StruggleDir * StruggleDist;
            ScreenShakeSystem.New(new(.7f, .6f, 1500f), NPC.Center);
            SpawnBreaks(4f, 12);
            for (int j = 0; j < 22; j++)
            {
                Vector2 pos = GlacierHitbox.RandomRectangle();
                Vector2 vel = -pos.SafeDirectionTo(GlacierHitbox.Center()) * Main.rand.NextFloat(4f, 6f);
                ParticleRegistry.SpawnBloomLineParticle(pos, vel * 2.4f, Main.rand.Next(30, 44), Main.rand.NextFloat(.3f, .5f), Color.LightSkyBlue);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel * Main.rand.NextFloat(.9f, 1.7f), Main.rand.Next(120, 190), Main.rand.NextFloat(.5f, .6f), SlateBlue, LightCornflower, null, 1.4f);
            }
            BreakCompletion = 1f;
            NPC.netUpdate = true;
        }
        else if (AttackTimer < Scream)
        {
            NPC.Center = Vector2.Lerp(NPC.Center, StrugglePos, MakePoly(9f).OutFunction(InverseLerp(SecondBreak, FinalBreak, AttackTimer)));
        }
        else if (AttackTimer < AwakenTime)
        {
            if (AttackTimer % 8 == 7)
            {
                ParticleRegistry.SpawnPulseRingParticle(HeadCenter, Vector2.Zero, 20, 0f, Vector2.One, 0f, 4000f, SlateBlue);
            }

            NPC.Center += Main.rand.NextVector2Circular(6f, 6f);
            HeadRotation = HeadRotation.SmoothAngleLerp(HeadCenter.SafeDirectionTo(Target.Center).ToRotation(), .7f, .4f);
            ParticleRegistry.SpawnBlurParticle(NPC.Center, 10, InverseLerp(Scream, Scream + 15f, AttackTimer) * .2f, 6000f);
            ScreenShakeSystem.New(new(.1f, .1f, 2500f), NPC.Center);

            if (NPC.dontTakeDamage)
            {
                NPC.dontTakeDamage = false;
                NPC.netUpdate = true;
            }
        }

        if (AttackTimer < Scream)
            HeadRotation = HeadRotation.SmoothAngleLerp(StruggleDir.ToRotation(), .6f, .2f);
        else
            HeadRotation = HeadRotation.SmoothAngleLerp(HeadCenter.SafeDirectionTo(Target.Center).ToRotation(), .5f, .3f);

        SelectNextAttack(AttackTimer >= AwakenTime, AttackState.Skittering);
    }

    public void DoBehavior_Skittering()
    {
        if (Movement != Movements.Chasing)
        {
            Movement = Movements.Chasing;
            this.Sync();
        }
        HeadRotation = HeadRotation.SmoothAngleLerp(HeadCenter.SafeDirectionTo(Target.Center + Target.velocity * 5f).ToRotation(), .4f, .15f);

        if (AttackTimer % ShootWait == ShootWait - 1)
        {
            Vector2 vel = HeadRotation.ToRotationVector2() * ShootSpeed;
            if (this.RunServer())
                NPC.NewNPCProj(GunPos, vel, ModContent.ProjectileType<GlacialShell>(), IcicleDamage, 0f);
            AdditionsSound.GunLoop.Play(GunPos, Main.rand.NextFloat(.8f, 1f), 0f, .1f, 20);

            for (int i = 0; i < 10; i++)
                ParticleRegistry.SpawnGlowParticle(GunPos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.3f, .8f), Main.rand.Next(20, 30), Main.rand.NextFloat(22f, 31f), Color.SkyBlue);

            BarrelHeat = .7f;
            Recoil = 3;
            NPC.netUpdate = true;
        }

        SelectNextAttack(AttackTimer >= ShootTime, AttackState.ChargeAndBlast);
    }

    public void DoBehavior_ChargeAndBlast()
    {
        if (Movement != Movements.Standing)
        {
            Movement = Movements.Standing;
            this.Sync();
        }

        Vector2 homeIn = Utility.GetHomingVelocity(NPC.position, Target.position, Target.velocity, 800f);
        float fallOff = InverseLerp(TimeForBigShot, TimeForBigShot - 15f, AttackTimer);
        HeadRotation = HeadRotation.SmoothAngleLerp(homeIn.ToRotation(), .2f, .21f * fallOff);
        float interpolant = InverseLerp(0f, TimeForBigShot - 50, AttackTimer);

        // Charge effects
        int wait = (int)Lerp(7, 1, interpolant);
        if (AttackTimer % wait == (wait - 1) && AttackTimer < TimeForBigShot)
        {
            Vector2 pos = GunPos + Main.rand.NextVector2CircularEdge(100f, 100f);
            Vector2 vel = RandomVelocity(2f, 1f, 8f);
            int life = Main.rand.Next(90, 160);
            float scale = Main.rand.NextFloat(.5f, .8f);
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, Color.Cyan, Color.DeepSkyBlue, GunPos, 1.5f, 8, false);
        }

        // big shot
        if (AttackTimer == TimeForBigShot)
        {
            AdditionsSound.BraveSpecial1C.Play(GunPos, 1.5f, -.2f, .1f, 0);

            Vector2 velocity = HeadRotation.ToRotationVector2() * 20f;
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = velocity.RotatedByRandom(.7f) * Main.rand.NextFloat(.1f, .87f);
                ParticleRegistry.SpawnBloomPixelParticle(GunPos, vel, Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, 1.2f), Color.SkyBlue, Color.LightSkyBlue, null, 1.4f, 5);
            }
            ScreenShakeSystem.New(new(1f, .5f, 2000f), GunPos);

            if (this.RunServer())
                NPC.NewNPCProj(GunPos, velocity, ModContent.ProjectileType<HeavyFrostBlast>(), HeavyBlastDamage, 10f);
            Recoil = 12;
            BarrelHeat = 1.5f;
            NPC.netUpdate = true;
        }

        SelectNextAttack(AttackTimer >= TimeForBigShot + 50, AttackState.IceSkewers);
    }

    public void DoBehavior_IceSkewers()
    {
        if (Movement != Movements.Chasing)
        {
            Movement = Movements.Chasing;
            this.Sync();
        }
        SpeedMultiplier = .4f;
        HeadRotation = HeadRotation.SmoothAngleLerp(HeadCenter.AngleTo(Target.Center), .7f, .07f);

        if (AttackTimer % SkewerWait == (SkewerWait - 1))
        {
            Vector2 potential = Target.Center + (Vector2.UnitX * (Clamp(Target.velocity.X * 15f, -40f, 40f) + Main.rand.NextFloat(-4f, 4f)));
            Vector2? ground = FindNearestSurface(potential, true, 2000f, 50, true);
            if (ground.HasValue)
            {
                AdditionsSound.ColdHitMedium.Play(GunPos, 1f, -.1f, .1f, 10);
                for (int i = 0; i < 30; i++)
                    ParticleRegistry.SpawnSquishyPixelParticle(HeadRect.RandomPoint(), -Vector2.UnitY * Main.rand.NextFloat(2f, 7f), Main.rand.Next(60, 80), Main.rand.NextFloat(1.4f, 1.8f), SlateBlue, Icey, 4);
                if (this.RunServer())
                    NPC.NewNPCProj(potential, Vector2.Zero, ModContent.ProjectileType<GlacialSpike>(), SkewerDamage, 0f);
            }
        }

        SelectNextAttack(AttackTimer >= SecondsToFrames(8f), AttackState.Skittering);
    }

    public void DoBehavior_GoKablooey()
    {
        if (Movement != Movements.Chasing)
        {
            Movement = Movements.Chasing;
            this.Sync();
        }
        SpeedMultiplier = MakePoly(2f).OutFunction.Evaluate(AttackTimer, 0f, KablooeyMarker, .1f, 1.4f);
        HeadRotation = HeadRotation.SmoothAngleLerp(HeadCenter.AngleTo(Target.Center), .2f, .8f);
        Main.musicFade[Main.curMusic] = InverseLerp(KablooeyMarker, 0f, AttackTimer);

        if (SoundEngine.TryGetActiveSound(DeathSoundSlot, out var t) && t.IsPlaying)
            t.Position = NPC.Center;
        else
            DeathSoundSlot = AdditionsSound.AuroraKABLOOEY.Play(NPC.Center, 1f, 0f, 0f, 1, null, PauseBehavior.PauseWithGame);

        if (AttackTimer == KablooeyMarker)
        {
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<OverheatedBlast>(), HeavyBlastDamage * 2, 5f);
            ParticleRegistry.SpawnFlash(NPC.Center, 50, .5f, 900f);
            ScreenShakeSystem.New(new(.9f, 2f, 2000f), NPC.Center);

            if (!Main.dedServ)
            {
                for (int i = 0; i < 30; i++)
                {
                    Gore.NewGorePerfect(NPC.GetSource_FromAI(), HeadRect.RandomPoint(), Main.rand.NextVector2Circular(20f, 20f), Mod.Find<ModGore>($"GlacierBreak{Main.rand.Next(1, 5)}").Type);
                }
            }

            foreach (Player player in Main.ActivePlayers)
            {
                if (player != null)
                {
                    // Hard fall-off
                    float pushForce = 1.2f * MakePoly(3f).OutFunction(InverseLerp(1100f, 0f, player.Distance(NPC.Center)));

                    player.velocity.Y = -pushForce * 7f;
                    player.velocity.X += (player.Center.X - NPC.Center.X).NonZeroSign() * pushForce * 7f;
                }
            }

            NPC.Kill();
        }
    }

    public static readonly Dictionary<AdditionsSound, float> weights = new()
    {
        { AdditionsSound.AuroraTink1, 1f },
        { AdditionsSound.AuroraTink2, .6f },
        { AdditionsSound.AuroraTink3, .3f },
    };

    public override void HitEffect(NPC.HitInfo hit)
    {
        weights.Play(NPC.Center, .58f, 0f, .1f);
    }

    public override bool CheckDead()
    {
        if (CurrentState == AttackState.GoKablooey)
            return true;

        // Keep life at 1
        NPC.life = 1;
        NPC.netUpdate = true;
        return false;
    }
    #endregion

    #region Movement/Pathfinding/Inverse Kinematics
    public enum Movements
    {
        Standing,

        Chasing = 10,

        Jumping = 20,

        Falling = 30,
    }

    public const float riseSpeed = -0.4f;
    public const float minRiseSpeed = -8f;
    public const float gravity = 0.4f;
    public const float jumpVelocity = 13f;
    public void UpdateMovement()
    {
        NPC.noTileCollide = true;
        NPC.noGravity = true;

        switch (Movement)
        {
            case Movements.Standing:
                DoStanding();
                break;

            case Movements.Chasing:
                DoWalking();
                break;

            case Movements.Jumping:
                break;

            case Movements.Falling:
                break;
        }
    }

    public void DoStanding()
    {
        // Quickly come to a stop
        NPC.velocity *= .9f;

        GravityState(out bool onFloor, out bool insideSolids, out bool _);
        bool alwaysRise = false;
        float heightAbovePlayerToRiseTo = Target.Bottom.Y - 4;

        float distanceToPlayer = Math.Abs(NPC.Center.X - Target.Center.X);

        // Fall down if above player
        if (distanceToPlayer < 380f && FloorHeight < Target.Bottom.Y - 30 && !alwaysRise)
            NPC.velocity.Y = Clamp(NPC.velocity.Y + gravity, 0.001f, 16f);

        else if (onFloor)
        {
            NPC.velocity.Y = 0f;
            JumpTime = 0;
        }

        // Rise up if the whole body is in solid and below the player
        else if (insideSolids)
        {
            if (FloorHeight > heightAbovePlayerToRiseTo)
            {
                NPC.velocity.Y = Clamp(NPC.velocity.Y + riseSpeed, minRiseSpeed, 0f);
            }
            else
            {
                NPC.velocity.Y = 0;
            }
        }

        // Fall down
        else
        {
            NPC.velocity.Y = Clamp(NPC.velocity.Y + gravity, -jumpVelocity, 16f);
        }

        DoFalling();
    }

    public int JumpTime;
    public void DoWalking()
    {
        GravityState(out bool onFloor, out bool insideSolids, out bool acceptTopSurfaces);
        bool alwaysRise = false;
        float heightAbovePlayerToRiseTo = Target.Bottom.Y - 4;
        bool straightforwardPath = IsThereAStraightforwardPath(30, 444f, out bool falling);
        bool onlyVerticalMovement = false;

        float distanceToPlayer = Math.Abs(NPC.Center.X - Target.Center.X);
        float distanceToTarget = NPC.Distance(Target.Center);

        if (distanceToTarget >= 2100)
            SpeedMultiplier = 2f;
        else
        {
            if (!straightforwardPath)
            {
            }
            else
                Movement = Movements.Chasing;
        }

        // Walk towards the player
        if (!onlyVerticalMovement)
        {
            NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();
            float walkSpeed = (3f + InverseLerp(300f, 600f, distanceToPlayer) * 4f) * InverseLerp(0f, 60f, distanceToPlayer) * SpeedMultiplier;
            NPC.velocity.X = Lerp(NPC.velocity.X, walkSpeed * NPC.direction, 1 / 20f);
        }

        // Fall down if above player
        if (distanceToPlayer < 380f && FloorHeight < Target.Bottom.Y - 30 && !alwaysRise)
            NPC.velocity.Y = Clamp(NPC.velocity.Y + gravity, 0.001f, 16f);

        else if (onFloor)
        {
            NPC.velocity.Y = 0f;
            JumpTime = 0;
        }

        // Rise up if the whole body is in solid and below the player
        else if (insideSolids)
        {
            if (FloorHeight > heightAbovePlayerToRiseTo)
            {
                NPC.velocity.Y = Clamp(NPC.velocity.Y + riseSpeed, minRiseSpeed, 0f);
            }
            else
            {
                NPC.velocity.Y = 0;
            }
        }

        // Fall down
        else
        {
            NPC.velocity.Y = Clamp(NPC.velocity.Y + gravity, -jumpVelocity, 16f);
        }

        DoFalling();

        // Checking on the ground is too restrictive
        if (!Main.dedServ)
        {
            if (ShouldJump() && JumpTime <= 0)
            {
                NPC.velocity.Y -= jumpVelocity;
                {
                    foreach (AuroraGuardLeg safe in Legs)
                    {
                        Vector2 vel = safe.LegKnee.SafeDirectionTo(safe.LegTipGraphic);
                        ParticleRegistry.SpawnPulseRingParticle(safe.LegTipGraphic, vel * 2f, 30, vel.ToRotation(), new Vector2(.5f, 1f), 0f, 80f, Color.SkyBlue);
                        for (int i = 0; i < 3; i++)
                            ParticleRegistry.SpawnDustParticle(safe.LegTipGraphic, -vel.RotatedByRandom(.4f) * Main.rand.NextFloat(3f, 7f), Main.rand.Next(20, 34), Main.rand.NextFloat(.5f, .9f), Color.Cyan, .1f, true, true);
                    }
                }
            }
        }
        JumpTime++;

    }

    public void DoFalling()
    {
        if (NPC.velocity.Y <= 2)
        {
            // Landing
            if (NPC.velocity.Y <= 0 && KineticForce > 30f)
            {
                float force = InverseLerp(30, 50, KineticForce) * 0.5f + 0.5f;

                ScreenShakeSystem.New(new(force, force * .1f), NPC.Center);
                AdditionsSound.MediumExplosion.Play(NPC.Center, 1.1f, -.1f);

                if (!Main.dedServ)
                {
                    foreach (AuroraGuardLeg safe in Legs)
                        ParticleRegistry.SpawnShockwaveParticle(safe.LegTip, 20, .8f, 20f, 10f, .3f);
                }

                foreach (Player player in Main.ActivePlayers)
                {
                    if (player != null && !player.dead && player.velocity.Y == 0)
                    {
                        float pushForce = GaussianFalloff2D(NPC.Center, player.Center, force, 300f);
                        player.velocity.Y = -pushForce * 7f;
                        player.velocity.X += (player.Center.X - NPC.Center.X).NonZeroSign() * pushForce * 7f;
                    }
                }
            }

            KineticForce = 0;
        }
        else
            KineticForce += 1;
    }

    public bool ShouldJump()
    {
        AuroraGuardLeg safe = Legs.OrderBy(c => c.LegTip.Distance(Target.Center)).FirstOrDefault();
        if (IsThereAChasm(safe, widthThreshold: 16, out Point start, out Point end))
        {
            if (safe.LegTip.Distance(start.ToWorldCoordinates()) < 100f)
                return true;
        }

        // Nothing to be found
        return false;
    }

    public bool IsThereAStraightforwardPath(int freeFallDistanceCheck, float extraLeeway, out bool freeFall)
    {
        freeFall = !GroundCheck(NPC.Top.ToTileCoordinates(), CollisionBoxWidth / 32, freeFallDistanceCheck, out Point ground);

        if (freeFall)
            return false;

        int gravityState = GravityState(out _, out _, out _);

        // If we are currently inside tiles and below the player, go up to find a valid tile
        if (gravityState <= 0)
        {
            float verticalDistanceToPlayer = Math.Max(64, FloorHeight - (Target.Bottom.Y - 4));
            bool foundGroundAbove = false;

            for (int i = 0; i < verticalDistanceToPlayer / 16f; i++)
            {
                Tile t = Main.tile[ground + new Point(0, -i)];
                if (!t.HasUnactuatedTile || t.IsHalfBlock || !Main.tileSolid[t.TileType] || TileID.Sets.Platforms[t.TileType])
                {
                    ground += new Point(0, -i);
                    foundGroundAbove = true;
                    break;
                }
            }

            if (!foundGroundAbove)
                return false;
        }


        // Make sure the ground we found is navigable
        int maxIterations = 50;
        ground -= new Point(0, 1);
        ground = AStarPathfinding.OffsetUntilNavigable(ground, new Point(0, 1), TurretCrawlPathfind, ref maxIterations);
        if (maxIterations < 0)
            return false;

        maxIterations = 34;

        // Try to find navigable ground below the player
        Point pathfindingEnd = AStarPathfinding.OffsetUntilNavigable(Target.Center.ToTileCoordinates(), new Point(0, 1), TurretCrawlPathfind, ref maxIterations);

        // If theres no floor under the player then give up
        if (maxIterations < 0)
            return false;

        // If we managed to find a good starting point and a good ending point, we then proceed to simulate pathfinding between the two
        // If we can find a path to the target whose length is shorter than a straight line from start to end + some varying leeway
        // Then we know there exists an "easy straightforward path"
        return AStarPathfinding.IsThereAPath(ground, pathfindingEnd, TurretStride, TurretCrawlPathfind, extraLeeway);
    }

    public static bool GroundCheck(Point origin, int checkHalfWidth, int maxDistance, out Point groundPos)
    {
        for (int i = 0; i < maxDistance; i++)
        {
            for (int y = 0; y <= checkHalfWidth; y = (y < 1 ? -y + 1 : -y))
            {
                Tile t = ParanoidTileRetrieval(origin.X + y, origin.Y + i);
                if (!t.HasUnactuatedTile)
                    continue;

                if (Main.tileSolid[t.TileType] || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0))
                {
                    groundPos = origin + new Point(y, i);
                    return true;
                }
            }
        }

        groundPos = Point.Zero;
        return false;
    }

    public int GravityState(out bool touchingFloor, out bool insideSolids, out bool acceptTopSurfaces)
    {
        Rectangle targetHitbox = NPC.targetRect;
        acceptTopSurfaces = FloorHeight >= (float)targetHitbox.Bottom - 6; // Accept platforms if not above the players 

        insideSolids = SolidCollisionFix(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight, acceptTopSurfaces);
        bool upperBodyInSolids = SolidCollisionFix(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight - 4, acceptTopSurfaces);
        touchingFloor = insideSolids && !upperBodyInSolids;

        //Dust.QuickBox(CollisionBoxOrigin, CollisionBoxOrigin + new Vector2(CollisionBoxWidth, CollisionBoxHeight), 30, Color.Red, null);

        if (touchingFloor)
            return 0;

        // Rise up if were below the players floor height
        if (upperBodyInSolids && FloorHeight > Target.Bottom.Y - 4)
            return -1;

        return 1;
    }

    public static bool TurretCrawlPathfind(Point p, Point? from, out bool universallyUnnavigable)
    {
        universallyUnnavigable = true;

        Tile t = Main.tile[p];
        bool solidTile = Main.tileSolid[t.TileType];
        bool platform = TileID.Sets.Platforms[t.TileType];

        // Cant navigate inside solid tiles
        if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
            return false;

        universallyUnnavigable = false;

        // Can navigate on half tiles and platforms just fine
        if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
            return true;

        for (int i = -1; i <= 1; i++)
            for (int j = 0; j <= 1; j++)
            {
                // Only cardinal directions here
                if (j * i != 0 || (j == 0 && i == 0))
                    continue;

                // If a neighboring tile is solid we can go on it
                Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                    return true;
            }

        // Can fall straight down just fine
        if (from != null && p.X == from.Value.X && p.Y == from.Value.Y + 1)
            return true;

        return false;
    }

    public void SimulateLimbs()
    {
        if (Legs == null)
            InitializeLimbs();

        float highestReleaseScore = float.MinValue;
        AuroraGuardLeg highestReleaseLeg = null;
        int attachedLegs = 0;

        foreach (AuroraGuardLeg limb in Legs)
        {
            limb.Update();
            if (limb.LatchedOn)
                attachedLegs++;

            if (limb.ReleaseScore() > highestReleaseScore && limb.StepTimer <= 0)
            {
                highestReleaseLeg = limb;
                highestReleaseScore = limb.ReleaseScore();
            }

        }

        if (NPC.velocity.Length() > 1f && attachedLegs > 3 && highestReleaseLeg != null)
        {
            highestReleaseLeg.ReleaseGrip();
        }
    }

    public void InitializeLimbs()
    {
        Legs = [];
        for (int i = 0; i < 4; i++)
        {
            float baseRotation = Lerp(PiOver4 * 1.5f, -PiOver4 * 1.5f, i / 3f) + PiOver2;
            AuroraGuardLeg leg = new(this, i < 1 || i > 2, i < 2, baseRotation);
            Legs.Add(leg);
        }

        for (int i = 0; i < 4; i++)
        {
            int set = i < 2 ? 0 : 2;
            int otherSisterOffset = i % 2 == 0 ? 1 : 0;
            int pairedleg = i == 3 ? 0 : (i == 0 ? 3 : (i == 1 ? 2 : 1));

            Legs[i].PairedLeg = Legs[pairedleg];
            Legs[i].SisterLeg = Legs[set + otherSisterOffset];
        }
    }
    #endregion

    #region Updaters
    public void UpdateVisuals()
    {
        if (CurrentState != AttackState.Idle)
        {
            VerticalVisualOffset = 0f;
            VerticalVisualOffset += 9f * Sin01(Main.GlobalTimeWrappedHourly * 3);
        }

        BodyRotation = NPC.velocity.X * 0.04f;
        Vector2 averageLeftLegs = Vector2.Lerp(Legs[0].LegTip, Legs[1].LegTip, 0.5f);
        Vector2 averageRightLegs = Vector2.Lerp(Legs[2].LegTip, Legs[3].LegTip, 0.5f);

        if (averageLeftLegs.X < averageRightLegs.X)
        {
            BodyRotation += averageLeftLegs.AngleTo(averageRightLegs) * 0.25f;
        }

        HeadFlip = float.IsNegative(MathF.Cos(HeadRotation)) ? SpriteEffects.FlipVertically : SpriteEffects.None;
        BodyFlip = NPC.velocity.X.NonZeroSign().ToSpriteDirection();

        if (Recoil > 0)
            Recoil--;

        if (BarrelHeat > 0f)
        {
            BarrelHeat -= .01f;
            Lighting.AddLight(GunPos, new Vector3(0.75f, 0.85f, 1.4f) * BarrelHeat);
        }
    }
    #endregion

    #region Drawing
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D head = AssetRegistry.GetTexture(AdditionsTexture.AuroraTurretHead);
        Texture2D turretBase = AssetRegistry.GetTexture(AdditionsTexture.AuroraTurretBase);

        if (Legs == null)
            return false;

        foreach (AuroraGuardLeg leg in Legs)
        {
            if (!leg.FrontPair)
                leg.Draw(spriteBatch, screenPos, drawColor);
        }

        spriteBatch.Draw(turretBase, VisualCenter - screenPos, null, drawColor, BodyRotation, turretBase.Size() / 2, NPC.scale, BodyFlip, 0);
        spriteBatch.Draw(head, HeadCenter - screenPos, null, drawColor, HeadRotation, head.Size() / 2, NPC.scale, HeadFlip, 0);
        if (CurrentState != AttackState.Idle)
        {
            DrawSight();
            if (BarrelHeat > 0f)
                DrawBarrelHeat();
        }

        foreach (AuroraGuardLeg leg in Legs)
        {
            if (leg.FrontPair)
                leg.Draw(spriteBatch, screenPos, drawColor);
        }

        if (CurrentState == AttackState.GoKablooey)
            DrawDeath();

        return false;
    }

    public void DrawSight()
    {
        Texture2D texture = AssetRegistry.InvisTex;

        Vector2 sightPos = HeadCenter + PolarVector(22f * (HeadFlip == SpriteEffects.FlipVertically ? -1 : 1), HeadRotation) + PolarVector(8f * (HeadFlip == SpriteEffects.FlipVertically ? 1 : -1), HeadRotation - PiOver2);

        float sightsSize = 500f;
        foreach (Player player in Main.ActivePlayers)
        {
            if (player != null && player.Hitbox.LineCollision(sightPos, sightPos + PolarVector(500f, HeadRotation), 10f))
                sightsSize = Clamp(sightPos.Distance(player.Center) * 2.4f, 10f, 500f);
        }

        float sightsResolution = 2f;
        Color color = Color.DeepSkyBlue;

        ManagedShader scope = ShaderRegistry.PixelatedSightLine;
        scope.TrySetParameter("noiseOffset", Main.GameUpdateCount * -0.003f);
        scope.TrySetParameter("mainOpacity", 1f);
        scope.TrySetParameter("resolution", new Vector2(sightsResolution * sightsSize));
        scope.TrySetParameter("rotation", -HeadRotation);
        scope.TrySetParameter("width", 0.0025f);
        scope.TrySetParameter("lightStrength", 3f);
        scope.TrySetParameter("color", color.ToVector3());
        scope.TrySetParameter("darkerColor", Color.Black.ToVector3());
        scope.TrySetParameter("bloomSize", 0.29f);
        scope.TrySetParameter("bloomMaxOpacity", 0.4f);
        scope.TrySetParameter("bloomFadeStrength", 7f);

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, scope.Effect);

        Main.EntitySpriteDraw(texture, sightPos - Main.screenPosition, null, Color.White, 0f, texture.Size() * .5f, sightsSize, 0, 0f);

        Main.spriteBatch.ExitShaderRegion();
    }

    public void DrawBarrelHeat()
    {
        void heat()
        {
            Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.AuroraTurretBarrelGlow);
            Texture2D ball = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Main.spriteBatch.DrawBetter(glow, HeadCenter + PolarVector(2f, HeadRotation) + PolarVector(1f, HeadRotation - PiOver2), null, Icey * 5f * BarrelHeat, HeadRotation, glow.Size() / 2f, 1f);
            Main.spriteBatch.DrawBetterRect(ball, ToTarget(GunPos, new(18f)), null, Icey * 3f * BarrelHeat, 0f, ball.Size() / 2f);
        }

        PixelationSystem.QueueTextureRenderAction(heat, PixelationLayer.OverNPCs, BlendState.Additive);
    }

    public void DrawDeath()
    {
        SpriteBatch sb = Main.spriteBatch;
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise);

        ManagedShader shine = AssetRegistry.GetShader("RadialShineShader");
        float completion = InverseLerp(0f, KablooeyMarker, AttackTimer);
        shine.TrySetParameter("glowPower", .3f * completion);
        shine.TrySetParameter("glowColor", SlateBlue.ToVector4());
        shine.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 3.8f);

        sb.EnterShaderRegionAlt();
        float fade = MakePoly(2f).OutFunction(completion);
        shine.Render("AutoloadPass", true, false);
        sb.Draw(tex, ToTarget(NPC.Center, new(800f * fade)), null, Icey * 0.4f * fade, 0f, tex.Size() / 2, 0, 0f);
        sb.ExitShaderRegion();
    }
    #endregion

    #region the spoils
    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CracklingFragments>(), 1, 2, 3));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Bergcrusher>(), 3, 1, 1));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Rimesplitter>(), 4, 1, 1));
    }
    #endregion
}
