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
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.AuroraTurret;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora.TEST;

[AutoloadBossHead]
public partial class AuroraGuard : ModNPC
{
    #region Defaults/Variables
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
        /// Used when the player tries to hide behind something
        /// </summary>
        IceSkewers,

        /// <summary>
        /// Either too many back hits or shots fired in sequence
        /// </summary>
        Overwhelmed,

        /// <summary>
        /// Death animation
        /// </summary>
        GoKablooey,

        /// <summary>
        /// Leave animation
        /// </summary>
        FadeAway,

        ResetCycle,
    }

    private PushdownAutomata<EntityAIState<AttackState>, AttackState> stateMachine;

    public PushdownAutomata<EntityAIState<AttackState>, AttackState> StateMachine
    {
        get
        {
            if (stateMachine is null)
                LoadStates();
            return stateMachine!;
        }
        set => stateMachine = value;
    }

    public ref int AttackTimer => ref StateMachine.CurrentState.Time;

    /// <summary>
    /// Simply so that the sound position can be updated
    /// </summary>
    public SlotId DeathSoundSlot;

    public void LoadStates()
    {
        // Initialize the AI state machine.
        StateMachine = new(new(AttackState.Awaken));
        StateMachine.OnStateTransition += ResetGenericVariables;

        // Register all of Asterlins states in the machine.
        foreach (AttackState type in Enum.GetValues(typeof(AttackState)))
            StateMachine.RegisterState(new EntityAIState<AttackState>(type));

        StateMachine.ApplyToAllStatesExcept(state =>
        {
            StateMachine.RegisterTransition(state, AttackState.GoKablooey, false, () => NPC.life <= 1, () =>
            {
                NPC.dontTakeDamage = true;
            });
        }, AttackState.GoKablooey);

        StateMachine.ApplyToAllStatesExcept(state =>
        {
            StateMachine.RegisterTransition(state, AttackState.GoKablooey, false, () => Stress >= MaxStress, () =>
            {

            });
        }, AttackState.GoKablooey);

        // Load state transitions.
        AutomatedMethodInvokeAttribute.InvokeWithAttribute(this);
    }

    public AttackState CurrentState
    {
        get
        {
            // Add the relevant phase cycle if it has been exhausted, to ensure that the attacks are cyclic
            if (StateMachine.StateStack is not null && (StateMachine?.StateStack?.Count ?? 1) <= 0)
                StateMachine?.StateStack.Push(StateMachine.StateRegistry[AttackState.ResetCycle]);

            return StateMachine?.CurrentState?.Identifier ?? AttackState.Awaken;
        }
    }

    public void ResetGenericVariables(bool stateWasPopped, EntityAIState<AttackState> oldState)
    {
        NPC.netUpdate = true;
    }

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

    public ref float Stress => ref NPC.ai[0];
    public ref float SpeedMult => ref NPC.ai[1];
    public ref float BarrelHeat => ref NPC.ai[2];

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AuroraTurretHead);
    public override string HeadTexture => AssetRegistry.GetTexturePath(AdditionsTexture.AuroraTurretHead);
    public override string BossHeadTexture => AssetRegistry.GetTexturePath(AdditionsTexture.AuroraTurretHead);

    public override void SetStaticDefaults()
    {
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Slow & BuffID.Webbed] = true;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.UsesNewTargetting[Type] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);

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

        NPC.aiStyle = -1;
        AIType = -1;
        NPC.knockBackResist = 0f;
        NPC.boss = true;
        NPC.value = Item.buyPrice(0, 5, 0, 0);
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.netAlways = true;
        NPC.scale = 1f;

        NPC.npcSlots = 3;
        NPC.rarity = 4;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<AuroraTurretBanner>();
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
        {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,
                new FlavorTextBestiaryInfoElement(this.GetLocalizedValue("Bestiary"))
        });
    }

    public Vector2 oldVisualOffset;
    public Vector2 visualOffset;

    // Note: NPC.Center is for the body
    public Vector2 HeadCenter => VisualCenter + PolarVector(-28f, bodyRotation + PiOver2) + PolarVector(Recoil, headRotation - Pi);
    public RotatedRectangle HeadRect => new(74f, HeadCenter + PolarVector(63f, headRotation - Pi), HeadCenter + PolarVector(63f, headRotation));
    public float headRotation;
    public float Recoil;
    public float bodyRotation;
    public SpriteEffects headFlip;
    public SpriteEffects bodyFlip;
    public Vector2 VisualCenter => NPC.Center + visualOffset - Vector2.UnitY * 10f;

    public static int CollisionBoxWidth => 50;
    public int CollisionBoxHeight => NPC.height + CollisionBoxYOffset;
    public int CollisionBoxYOffset => (int)(80);
    public Vector2 CollisionBoxOrigin => NPC.Top - Vector2.UnitX * (CollisionBoxWidth / 2);

    public Rectangle CollisionBox
    {
        get
        {
            Vector2 collisionBoxOrigin = NPC.Bottom - Vector2.UnitX * (CollisionBoxWidth / 2) - Vector2.UnitY * CollisionBoxHeight;
            return new Rectangle((int)collisionBoxOrigin.X, (int)collisionBoxOrigin.Y, CollisionBoxWidth, CollisionBoxHeight);
        }
    }

    public float kineticForce;
    public float kineticOffset;
    public float kineticOffsetVelocity;

    public float FloorHeight => NPC.Bottom.Y + CollisionBoxYOffset;
    public Vector2 FloorPosition => NPC.Bottom + Vector2.UnitY * CollisionBoxYOffset;
    public static readonly List<AStarNeighbour> TurretStride = AStarNeighbour.BigOmniStride(6, 8);
    public List<AuroraTurretLegSAFE> Legs;
    public List<Vector2> IdealStepPositions;
    public List<Vector2> PreviousIdealStepPositions;

    public Player Target => Main.player[NPC.target];

    public override void SendExtraAI(BinaryWriter writer)
    {

    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {

    }
    #endregion

    #region AI
    public override void OnSpawn(IEntitySource source)
    {
        if (!Target.active || Target.dead)
        {
            NPC.TargetClosest(false);
        }

        if (!Main.dedServ)
            InitializeLimbs();
    }

    public override bool PreAI()
    {
        NPC.netOffset = Vector2.Zero;
        return base.PreAI();
    }

    public Vector2 GunPos => HeadCenter + PolarVector(22f, headRotation);
    public override void AI()
    {
        PlayerTargeting.SearchForTarget(NPC, NPC.GetTargetData());

        SpeedMult = 1f;
        StateMachine?.PerformBehaviors();
        StateMachine?.PerformStateTransitionCheck();
        AttackTimer++;
        UpdateMovement();

        if (!Main.dedServ)
        {
            SimulateLimbs();
            UpdateVisuals();
        }
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Awaken()
    {
        StateMachine.RegisterTransition(AttackState.Awaken, AttackState.Skittering, false, () =>
        {
            return AttackTimer >= AwakenTime;
        });

        // Load the AI state behavior.
        StateMachine.RegisterStateBehavior(AttackState.Awaken, DoBehavior_Awaken);
    }

    public Vector2 GlacierPosition;
    public Rectangle GlacierHitbox => GlacierPosition.ToRectangle(132, 180);
    public Vector2 StruggleDir;
    public float StruggleDist;
    public Vector2 StrugglePos;
    public int StruggleSign;
    public float BreakCompletion;
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

            Gore.NewGorePerfect(NPC.GetSource_FromAI(), GlacierHitbox.RandomRectangle(), StruggleDir.RotatedByRandom(.4f) * Main.rand.NextFloat(3f, 5f) * power,
                Mod.Find<ModGore>($"GlacierBreak{Main.rand.Next(1, 5)}").Type);
        }
    }

    public void DoBehavior_Awaken()
    {
        if (AttackTimer <= FirstBreak)
        {
            GlacierPosition = FindNearestSurface(NPC.Center, true, 1000f, 10).Value - Vector2.UnitY * 80f;
            AdditionsSound.AuroraRise.Play(NPC.Center);
            NPC.Center = FindNearestSurface(NPC.Center, true, 2000f, 100, true).Value; // TODO: better spawning

            StruggleSign = Main.rand.NextFromList(-1, 1);
            StruggleDir = StruggleSign == -1 ? -Vector2.UnitY.RotatedBy(-MaxRadius).RotatedByRandom(MaxRadius) : -Vector2.UnitY.RotatedBy(MaxRadius).RotatedByRandom(MaxRadius);
            StruggleDist = Main.rand.NextFloat(20f, 30f);
            StrugglePos = NPC.Center + StruggleDir * StruggleDist;
            ScreenShakeSystem.New(new(.1f, .3f, 1500f), NPC.Center);
            SpawnBreaks();
            BreakCompletion = .4f;
        }
        else if (AttackTimer < SecondBreak)
        {
            NPC.Center = Vector2.Lerp(NPC.Center, StrugglePos, MakePoly(3f).OutFunction(InverseLerp(FirstBreak, SecondBreak, AttackTimer)));
        }
        else if (AttackTimer == SecondBreak)
        {
            StruggleSign = -StruggleSign;
            StruggleDir = StruggleSign == -1 ? -Vector2.UnitY.RotatedBy(-MaxRadius).RotatedByRandom(MaxRadius) : -Vector2.UnitY.RotatedBy(MaxRadius).RotatedByRandom(MaxRadius);
            StruggleDist = Main.rand.NextFloat(20f, 30f);
            StrugglePos = NPC.Center + StruggleDir * StruggleDist;
            ScreenShakeSystem.New(new(.4f, .5f, 1500f), NPC.Center);
            SpawnBreaks(1.3f, 4);
            BreakCompletion = .8f;
        }
        else if (AttackTimer < FinalBreak)
        {
            NPC.Center = Vector2.Lerp(NPC.Center, StrugglePos, MakePoly(6f).OutFunction(InverseLerp(SecondBreak, FinalBreak, AttackTimer)));
        }
        else if (AttackTimer == FinalBreak)
        {
            StruggleSign = -StruggleSign;
            StruggleDir = StruggleSign == -1 ? -Vector2.UnitY.RotatedBy(-MaxRadius).RotatedByRandom(MaxRadius) : -Vector2.UnitY.RotatedBy(MaxRadius).RotatedByRandom(MaxRadius);
            StruggleDist = Main.rand.NextFloat(50f, 60f);
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
            headRotation = headRotation.SmoothAngleLerp(HeadCenter.SafeDirectionTo(Target.Center).ToRotation(), .7f, .4f);
            ParticleRegistry.SpawnBlurParticle(NPC.Center, 10, InverseLerp(Scream, Scream + 15f, AttackTimer) * .2f, 6000f);
            ScreenShakeSystem.New(new(.1f, .1f, 2500f), NPC.Center);
        }

        if (AttackTimer < Scream)
            headRotation = headRotation.SmoothAngleLerp(StruggleDir.ToRotation(), .6f, .2f);
        else
            headRotation = headRotation.SmoothAngleLerp(HeadCenter.SafeDirectionTo(Target.Center).ToRotation(), .5f, .3f);
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Skittering()
    {
        StateMachine.RegisterTransition(AttackState.Skittering, AttackState.ChargeAndBlast, false, () =>
        {
            return AttackTimer >= ShootTime;
        });

        // Load the AI state behavior.
        StateMachine.RegisterStateBehavior(AttackState.Skittering, DoBehavior_Skittering);
    }
    public void DoBehavior_Skittering()
    {
        Movement = Movements.Chasing;
        headRotation = headRotation.SmoothAngleLerp(HeadCenter.SafeDirectionTo(Target.Center + Target.velocity * 5f).ToRotation(), .4f, .15f);

        if (AttackTimer % ShootWait == ShootWait - 1)
        {
            Vector2 vel = headRotation.ToRotationVector2() * ShootSpeed;
            NPC.NewNPCProj(GunPos, vel, ModContent.ProjectileType<GlacialShell>(), IcicleDamage, 0f);
            AdditionsSound.GunLoop.Play(GunPos, Main.rand.NextFloat(.8f, 1f), 0f, .1f, 20);

            for (int i = 0; i < 10; i++)
                ParticleRegistry.SpawnGlowParticle(GunPos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.3f, .8f), Main.rand.Next(20, 30), Main.rand.NextFloat(22f, 31f), Color.SkyBlue);

            BarrelHeat = .7f;
            Recoil = 3;
        }
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_ChargeAndBlast()
    {
        StateMachine.RegisterTransition(AttackState.ChargeAndBlast, AttackState.IceSkewers, false, () =>
        {
            return AttackTimer >= TimeForBigShot + 50;
        });

        // Load the AI state behavior.
        StateMachine.RegisterStateBehavior(AttackState.ChargeAndBlast, DoBehavior_ChargeAndBlast);
    }
    public void DoBehavior_ChargeAndBlast()
    {
        Movement = Movements.Standing;

        Vector2 homeIn = Utility.GetHomingVelocity(NPC.position, Target.position, Target.velocity, 800f);
        float fallOff = InverseLerp(TimeForBigShot, TimeForBigShot - 15f, AttackTimer);
        headRotation = headRotation.SmoothAngleLerp(homeIn.ToRotation(), .2f, .21f * fallOff);
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

            Vector2 velocity = headRotation.ToRotationVector2() * 20f;
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = velocity.RotatedByRandom(.7f) * Main.rand.NextFloat(.1f, .87f);
                ParticleRegistry.SpawnBloomPixelParticle(GunPos, vel, Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, 1.2f), Color.SkyBlue, Color.LightSkyBlue, null, 1.4f, 5);
            }
            ScreenShakeSystem.New(new(1f, .5f, 2000f), GunPos);

            NPC.NewNPCProj(GunPos, velocity, ModContent.ProjectileType<HeavyFrostBlast>(), HeavyBlastDamage, 10f);
            Recoil = 12;
            BarrelHeat = 1.5f;
            NPC.netUpdate = true;
        }
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_IceSkewers()
    {
        StateMachine.RegisterTransition(AttackState.IceSkewers, AttackState.Skittering, false, () =>
        {
            return AttackTimer >= SecondsToFrames(8f);
        });

        // Load the AI state behavior.
        StateMachine.RegisterStateBehavior(AttackState.IceSkewers, DoBehavior_IceSkewers);
    }
    public void DoBehavior_IceSkewers()
    {
        Movement = Movements.Chasing;
        SpeedMult = .4f;
        headRotation = headRotation.SmoothAngleLerp(HeadCenter.AngleTo(Target.Center), .7f, .07f);

        if (AttackTimer % SkewerWait == (SkewerWait - 1))
        {
            Vector2 potential = Target.Center + (Vector2.UnitX * (Clamp(Target.velocity.X * 15f, -40f, 40f) + Main.rand.NextFloat(-4f, 4f)));
            Vector2? ground = FindNearestSurface(potential, true, 2000f, 50, true);
            if (ground.HasValue)
            {
                AdditionsSound.ColdHitMedium.Play(GunPos, 1f, -.1f, .1f, 10);
                for (int i = 0; i < 30; i++)
                    ParticleRegistry.SpawnSquishyPixelParticle(HeadRect.RandomPoint(), -Vector2.UnitY * Main.rand.NextFloat(2f, 7f), Main.rand.Next(60, 80), Main.rand.NextFloat(1.4f, 1.8f), SlateBlue, Icey, 4);
                NPC.NewNPCProj(potential, Vector2.Zero, ModContent.ProjectileType<GlacialSpike>(), SkewerDamage, 0f);
            }
        }
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Overwhelmed()
    {
        StateMachine.ApplyToAllStatesExcept(previousState =>
        {
            StateMachine.RegisterTransition(previousState, AttackState.Overwhelmed, false, () =>
            {
                return Stress >= MaxStress;
            }, () =>
            {
                NPC.netUpdate = true;
            });
        }, AttackState.FadeAway, AttackState.GoKablooey);

        // Load the AI state behavior.
        StateMachine.RegisterStateBehavior(AttackState.Overwhelmed, DoBehavior_Overwhelmed);
    }
    public void DoBehavior_Overwhelmed()
    {
        Movement = Movements.Standing;

        // Crash into the ground
        float interpolant = MakePoly(6f).InFunction(InverseLerp(0f, 110f, AttackTimer));
        NPC.Center = Vector2.Lerp(NPC.Center, CollisionBox.Bottom(), interpolant);
        headRotation = headRotation.SmoothAngleLerp(NPC.direction == -1 ? -(PiOver4 + PiOver2) : -PiOver4, .7f, .06f * interpolant);

        if (Main.rand.NextBool(4))
        {
            ParticleRegistry.SpawnMistParticle(HeadRect.RandomPoint(), -Vector2.UnitY * Main.rand.NextFloat(1f, 9f), Main.rand.NextFloat(.7f, 1.2f), Icey, DeepBlue, Main.rand.NextFloat(100f, 240f));
        }
    }

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_GoKablooey()
    {
        // Load the AI state behavior.
        StateMachine.RegisterStateBehavior(AttackState.GoKablooey, DoBehavior_GoKablooey);
    }
    public void DoBehavior_GoKablooey()
    {
        Movement = Movements.Chasing;
        SpeedMult = MakePoly(2f).OutFunction.Evaluate(AttackTimer, 0f, KablooeyMarker, .1f, 1.4f);
        headRotation = headRotation.SmoothAngleLerp(HeadCenter.AngleTo(Target.Center), .2f, .8f);

        if (SoundEngine.TryGetActiveSound(DeathSoundSlot, out var t) && t.IsPlaying)
            t.Position = NPC.Center;
        else
            DeathSoundSlot = AdditionsSound.AuroraKABLOOEY.Play(NPC.Center);

        if (AttackTimer == KablooeyMarker)
        {
            NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<OverheatedBlast>(), HeavyBlastDamage * 2, 5f);
            ParticleRegistry.SpawnFlash(NPC.Center, 50, .5f, 900f);
            ScreenShakeSystem.New(new(.9f, 2f, 2000f), NPC.Center);

            for (int i = 0; i < 30; i++)
            {
                Gore.NewGorePerfect(NPC.GetSource_FromAI(), HeadRect.RandomPoint(), Main.rand.NextVector2Circular(20f, 20f), Mod.Find<ModGore>($"GlacierBreak{Main.rand.Next(1, 5)}").Type);
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
    public Movements Movement
    {
        get => (Movements)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = (int)value;
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
            SpeedMult = 2f;
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
            float walkSpeed = (3f + InverseLerp(300f, 600f, distanceToPlayer) * 4f) * InverseLerp(0f, 60f, distanceToPlayer) * SpeedMult;
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
        if (ShouldJump() && JumpTime <= 0)
        {
            NPC.velocity.Y -= jumpVelocity;
            foreach (AuroraTurretLegSAFE safe in Legs)
            {
                Vector2 vel = safe.legKnee.SafeDirectionTo(safe.legTipGraphic);
                ParticleRegistry.SpawnPulseRingParticle(safe.legTipGraphic, vel * 2f, 30, vel.ToRotation(), new Vector2(.5f, 1f), 0f, 80f, Color.SkyBlue);
                for (int i = 0; i < 3; i++)
                    ParticleRegistry.SpawnDustParticle(safe.legTipGraphic, -vel.RotatedByRandom(.4f) * Main.rand.NextFloat(3f, 7f), Main.rand.Next(20, 34), Main.rand.NextFloat(.5f, .9f), Color.Cyan, .1f, true, true);
            }

            JumpTime++;
        }
    }

    public void DoFalling()
    {
        if (NPC.velocity.Y <= 2)
        {
            // Landing
            if (NPC.velocity.Y <= 0 && kineticForce > 30f)
            {
                float force = InverseLerp(30, 50, kineticForce) * 0.5f + 0.5f;

                ScreenShakeSystem.New(new(force, force * .1f), NPC.Center);
                AdditionsSound.MediumExplosion.Play(NPC.Center, 1.1f, -.1f);

                foreach (AuroraTurretLegSAFE safe in Legs)
                    ParticleRegistry.SpawnShockwaveParticle(safe.legTip, 20, .8f, 20f, 10f, .3f);

                kineticOffset = 40f;
                kineticOffsetVelocity = force * 3f;

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

            kineticForce = 0;
        }
        else
            kineticForce += 1;
    }

    public bool ShouldJump()
    {
        AuroraTurretLegSAFE safe = Legs.OrderBy(c => c.legTip.Distance(Target.Center)).FirstOrDefault();
        if (IsThereAChasm(safe, widthThreshold: 16, out Point start, out Point end))
        {
            if (safe.legTip.Distance(start.ToWorldCoordinates()) < 100f)
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
                Tile t = Main.tile[origin + new Point(y, i)];
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
        AuroraTurretLegSAFE highestReleaseLeg = null;
        int attachedLegs = 0;

        foreach (AuroraTurretLegSAFE limb in Legs)
        {
            limb.Update();
            if (limb.latchedOn)
                attachedLegs++;

            if (limb.ReleaseScore() > highestReleaseScore && limb.stepTimer <= 0)
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
            AuroraTurretLegSAFE leg = new(this, i < 1 || i > 2, i < 2, baseRotation);
            Legs.Add(leg);
        }

        for (int i = 0; i < 4; i++)
        {
            int set = i < 2 ? 0 : 2;
            int otherSisterOffset = i % 2 == 0 ? 1 : 0;
            int pairedleg = i == 3 ? 0 : (i == 0 ? 3 : (i == 1 ? 2 : 1));

            Legs[i].pairedLeg = Legs[pairedleg];
            Legs[i].sisterLeg = Legs[set + otherSisterOffset];
        }
    }
    #endregion

    #region Updaters
    public void UpdateVisuals()
    {
        oldVisualOffset = visualOffset;

        visualOffset = Vector2.Zero;
        visualOffset.Y += 9f * Sin01(Main.GlobalTimeWrappedHourly * 3);

        bodyRotation = NPC.velocity.X * 0.04f;
        Vector2 averageLeftLegs = Vector2.Lerp(Legs[0].legTip, Legs[1].legTip, 0.5f);
        Vector2 averageRightLegs = Vector2.Lerp(Legs[2].legTip, Legs[3].legTip, 0.5f);

        if (averageLeftLegs.X < averageRightLegs.X)
        {
            bodyRotation += averageLeftLegs.AngleTo(averageRightLegs) * 0.25f;
        }

        headFlip = MathF.Cos(headRotation) < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
        bodyFlip = NPC.velocity.X.NonZeroSign().ToSpriteDirection();

        if (Recoil > 0)
            Recoil--;

        if (BarrelHeat > 0f)
        {
            BarrelHeat -= .01f;
            Lighting.AddLight(GunPos, new Vector3(0.75f, 0.85f, 1.4f) * BarrelHeat);
        }
    }
    #endregion

    #region Legs
    public class AuroraTurretLegSAFE : Entity
    {
        public float maxlength;
        public bool latchedOn = false;

        public AuroraTurretLegSAFE pairedLeg;
        public AuroraTurretLegSAFE sisterLeg;

        public bool frontPair;
        public bool leftSet;

        public Vector2 legOrigin;
        public Vector2 legKnee;
        public Vector2 legTip;

        public Vector2 legTipGraphic;
        public Vector2 legOriginGraphic;

        public float grabDelay = 0;
        public float stepTimer = 0;
        public float strideTimer = 0;
        public float fallTime = 0f;

        /// <summary>
        /// The absolutely ideal grab position of the leg
        /// </summary>
        public Vector2 desiredGrabPosition;

        /// <summary>
        /// The best grab position we found
        /// </summary>
        public Vector2? grabPosition;

        /// <summary>
        /// The previously best grab position we found
        /// </summary>
        public Vector2? previousGrabPosition;

        /// <summary>
        /// The best grab tile we found
        /// </summary>
        public Point? GrabTile
        {
            get
            {
                if (grabPosition != null)
                    return grabPosition.Value.ToTileCoordinates();
                return null;
            }
        }

        public float Foreleglength => 40.5f;
        public float Leglength => 96f;

        public AuroraGuard turret;
        public NPC NPC => turret.NPC;
        public float baseRotation;

        public bool playedStepEffects = true; // If it needs to play its stepping sound
        public float stepEffectForce = 1f; // Volume of the stepping sound when played. Amps up the longer the foot is left in the air

        public int Direction => leftSet ? -1 : 1;

        public float SisterInfluence => sisterLeg.latchedOn ? sisterLeg.stepTimer : 1;

        public AuroraTurretLegSAFE(AuroraGuard turret, bool frontPair, bool leftSet, float baseRotation)
        {
            this.turret = turret;
            this.frontPair = frontPair;
            this.leftSet = leftSet;
            this.baseRotation = baseRotation;

            legOrigin = GetLegOrigin();
            legKnee = legOrigin + Vector2.UnitY * Foreleglength;
            legTip = legKnee + Vector2.UnitY * Leglength;
            legTipGraphic = legTip;
            legOriginGraphic = legOrigin + turret.visualOffset;

            ForelimbAsset = AssetRegistry.GetTexture(AdditionsTexture.AuroraLimbStart);
            LimbAsset = AssetRegistry.GetTexture(AdditionsTexture.AuroraLimbEnd);

            forelegSpriteOrigin = new Vector2(6, 6);

            if (leftSet)
                forelegSpriteOrigin.Y = ForelimbAsset.Height - forelegSpriteOrigin.Y;

            legSpriteOrigin = new Vector2(8, 24);

            if (leftSet)
                legSpriteOrigin.Y = LimbAsset.Height - legSpriteOrigin.Y;
        }

        public void Update()
        {
            NPC owner = turret.NPC;
            maxlength = Leglength + Foreleglength;
            Vector2 legDirection = (baseRotation + owner.rotation).ToRotationVector2();

            legOrigin = GetLegOrigin();
            legOriginGraphic = legOrigin + turret.visualOffset;

            // Check if the leg is latched onto something based on if its close enough to the grab position
            latchedOn = false;
            if (grabPosition != null && Vector2.Distance(legTip, grabPosition.Value) < 10f)
            {
                legTip = grabPosition.Value;
                latchedOn = true;
            }

            UpdateDesiredGrabPosition(legDirection);
            bool frontSet = Math.Sign(turret.NPC.velocity.X) == Direction;

            width = height = (int)maxlength;
            position = legOrigin;
            Center = legKnee;

            int dir = turret.NPC.velocity.X.NonZeroSign();
            if (leftSet)
                direction = -1;
            else
                direction = 1;

            // When grappled
            if (latchedOn)
            {
                // Check if the leg is "uncomfortable" enough and release if it is
                if (ShouldReleaseLeg(frontSet, out bool noStepDelay))
                {
                    ReleaseGrip();
                    if (noStepDelay)
                        grabDelay = 0;
                }

                // Step effects
                if (!playedStepEffects)
                {
                    if (stepEffectForce > 0.4f)
                    {
                        // Step sound volume scales with how long the leg has been out in the air
                        float stepPitch = InverseLerp(300f, 1000f, legTip.Distance(Main.LocalPlayer.Center), true) * .3f;
                        float stepVolume = InverseLerp(0.5f, 1f, stepEffectForce);
                        AdditionsSound.LegStomp.Play(legTip, stepVolume * .36f, stepPitch, .1f, 20);
                        Collision.HitTiles(legTip, legTip.SafeDirectionTo(turret.VisualCenter), 15, 15);

                        // Screenshake if big enough
                        ScreenShakeSystem.New(new(stepVolume * .03f, .3f), legTip);
                    }
                    playedStepEffects = true;
                }

                stepEffectForce = 0f;
                fallTime = 0f;

                // Tick down the step timer (Controls the small ground stab motion when it finishes a new step)
                stepTimer -= 1 / (60f * 0.3f);
                if (stepTimer < 0)
                    stepTimer = 0;
            }

            // When free
            else
            {
                // Check for a new position to latch on if we dont have one
                if (grabPosition == null)
                    FindGrabPos();

                // If we still dont have a valid grab position
                if (grabPosition == null)
                {
                    // Fall
                    if (owner.velocity.Y > 2)
                    {
                        fallTime++;

                        // When falling, flail legs around a point
                        Vector2 fallingPosition = desiredGrabPosition - Vector2.UnitY * 100f;
                        Vector2 fallPositionOffset = new((float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f) * 40f, 21f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 30f) * 70f);
                        fallingPosition += fallPositionOffset;

                        // Back set of legs is a bit more retracted towards the center
                        if (!frontPair)
                            fallingPosition.X -= Direction * 30f;

                        // Undo the lateral displacement of the desired grab position, the legs should be spread evenly
                        fallingPosition.X -= DesiredGrabPositionVelocityXOffset;

                        // Move towards the falling leg position
                        legTip = Vector2.SmoothStep(legTip, fallingPosition, Animators.MakePoly(3f).OutFunction(Min(1f, fallTime / 8f) * 0.1f));

                        // Entirely lose your previous grab position if falling for too long
                        if (fallTime > 10f)
                            previousGrabPosition = null;
                    }

                    // Leg limps down
                    else
                    {
                        legTip.Y += 4.2f;
                        if (SolidCollisionFix(legTip, 2, 2, true))
                            legTip.Y -= 4.2f;
                    }
                }

                // Otherwise
                else
                {
                    // If we have a previous position to step from, do a nicely eased step
                    if (previousGrabPosition.HasValue)
                    {
                        float amount = MakePoly(2.7f).InOutFunction(1 - strideTimer);
                        legTip = Vector2.SmoothStep(previousGrabPosition.Value, grabPosition.Value, amount);

                        // Upwards bump motion
                        legTip.Y -= 12.5f * (float)Math.Sin(strideTimer * Pi);
                    }

                    // If this is the first step after spawning, or after falling, do a slightly less clean motion towards the Target
                    else
                    {
                        // Move faster towards the tip if the leg has been falling for a while
                        float moveSpeed = (10f + InverseLerp(20f, 40f, fallTime) * 15f);
                        legTip = legTip.MoveTowards(grabPosition.Value, moveSpeed);
                        legTip.Y -= 4.5f * InverseLerp(0f, 50f, Math.Abs(legTip.X - grabPosition.Value.X));
                    }

                    // Time to move between the last grab position and the new one. Increases with Turrets speed
                    float stepTime = 0.32f - 0.12f * InverseLerp(4f, 8f, Math.Abs(NPC.velocity.X));
                    strideTimer -= 1 / (60f * stepTime);
                    if (strideTimer < 0)
                        strideTimer = 0;

                    // If we somehow moved away from the grab position so far that the leg cant even reach it, stop trying to grip it and find a new one next frame
                    if (legOrigin.Distance(grabPosition.Value) > maxlength)
                    {
                        ReleaseGrip();
                    }
                }

                // Reset visual variables and charge up the force of the step effects
                stepEffectForce = Math.Min(1f, stepEffectForce + 0.125f);
                playedStepEffects = false;
                stepTimer = 1f;
            }

            legTipGraphic = legTip;

            // Leg tip "pierces" the ground a bit when stepping
            legTipGraphic += Vector2.UnitY * 7f;
            if (stepTimer < 1)
                legTipGraphic.Y += 10f * MakePoly(2.4f).InFunction(stepTimer);

            legKnee = CalculateJointPosition(legOriginGraphic, legTipGraphic, Foreleglength, Leglength, !leftSet);
            
            if (legTipGraphic.Distance(legOriginGraphic) > maxlength)
                legTipGraphic = legOriginGraphic + legOriginGraphic.DirectionTo(legTipGraphic) * maxlength;
        }

        public float DesiredGrabPositionVelocityXOffset => (Math.Abs(NPC.velocity.X) > 2f && (NPC.velocity.X * Direction < 0)) ?
            NPC.velocity.X.NonZeroSign() * 150f : 0f;

        public void UpdateDesiredGrabPosition(Vector2 legDirection)
        {
            desiredGrabPosition = NPC.Center + (legDirection * 1.25f + Vector2.UnitY).SafeNormalize(Vector2.UnitY) * maxlength * 0.9f;

            // Offset grab positions sideways
            desiredGrabPosition += Vector2.UnitX * 90f * Direction;

            // Offset the grab positions latterally by the NPCs velocity if on the set of legs trailing behind
            desiredGrabPosition.X += DesiredGrabPositionVelocityXOffset;

            // Clamp the distance
            if (desiredGrabPosition.Distance(legOrigin) >= maxlength)
                desiredGrabPosition = legOrigin + legOrigin.DirectionTo(desiredGrabPosition) * maxlength;
        }

        public Vector2 GetLegOrigin()
        {
            float x = false ? (frontPair ? 46f : 32f) * (leftSet ? -1 : 1) : (frontPair ? 70f : 46f) * (leftSet ? -1 : 1);
            float y = false ? (frontPair ? 7f : 20f) : (frontPair ? 5f : -11f);
            Vector2 offset = new Vector2(x / 2, y);

            return turret.NPC.Center + offset;
        }

        #region Check if leg should release
        public bool ShouldReleaseLeg(bool frontSet, out bool noDelay)
        {
            noDelay = false;

            float maxExtensionTreshold = 1f - SisterInfluence * 0.15f;

            // If the legs are the ones being walked away from, the max length treshold is also shortened even more
            if (!frontSet)
                maxExtensionTreshold -= (1 - SisterInfluence) * 0.2f;

            // Keep the treshold full if walking slowly enough
            if (Math.Abs(NPC.velocity.X) < 1.4f)
                maxExtensionTreshold = 1f;

            float minExtensionTreshold = 0.26f - SisterInfluence * 0.16f;

            float tooFarUnderTreshold = (0.25f + SisterInfluence * 0.75f) * 40f;
            float maxHeightTreshold = 30f;

            float extension = legTip.Distance(legOrigin);

            if (legTip.Distance(grabPosition.Value) > maxlength)
                return true;

            // Ungrip when extended too far out
            if (extension > maxlength * maxExtensionTreshold)
                return true;

            // Ungrip when the leg is too compressed
            else if (extension < maxlength * minExtensionTreshold)
            {
                noDelay = true;
                return true;
            }

            // Ungrip when the leg is too far behind and should take a new step forward
            // Either immediately if part of the front set of legs, or if the step timer is over (to avoid back legs rapid fire)
            else if ((legOrigin.X - legTip.X) * Direction > tooFarUnderTreshold && (frontSet || stepTimer <= 0))
            {
                noDelay = true;
                return true;
            }

            // Ungrip when the leg is too far above the turret and too close to the turret
            else if (legOrigin.Y - legTip.Y > maxHeightTreshold && (legTip.X - legOrigin.X) * Direction < maxlength * 0.2f)
                return true;

            return false;
        }

        public void ReleaseGrip()
        {
            if (pairedLeg.grabDelay < 1 && grabDelay < 1)
                grabDelay = 4;

            strideTimer = 1f;
            previousGrabPosition = grabPosition ?? legTip;
            grabPosition = null;
            latchedOn = false;
        }
        #endregion

        #region Grab Position Scanning
        private void FindGrabPos(bool debugView = false)
        {
            // Dont grab if in delay period
            if (grabDelay > 0)
            {
                grabDelay--;
                return;
            }

            bool frontSet = Math.Sign(turret.NPC.velocity.X) == Direction;

            // The position tracing from the shoulder to the desired grab position
            Vector2 shoulder = legOrigin;
            Vector2 grip = desiredGrabPosition;
            if (frontSet)
            {
                shoulder.X += turret.NPC.velocity.X * 40f;
                grip.X += turret.NPC.velocity.X * 10f;
                grip.Y -= 20f;

                // Clamp distances
                if (grip.Distance(legOrigin) > maxlength)
                    grip = legOrigin + legOrigin.DirectionTo(grip) * maxlength;

                if (shoulder.Distance(legOrigin) > maxlength)
                    shoulder = legOrigin + Vector2.UnitX * Direction * maxlength;

                if (debugView)
                {
                    shoulder.SuperQuickDust(Color.Red);
                    grip.SuperQuickDust(Color.Yellow);
                    AdditionsDebug.DebugLine(shoulder, grip, Color.Blue);
                }
            }
            Vector2? trace = RaytraceTiles(shoulder, grip, true);
            Point? fromShoulderGuess = trace.HasValue ? trace.Value.ToTileCoordinates() : null;
            Point? bestGuess = null;
            bool tooClose = false;

            // We dont really want to grab a tile thats too close
            if (fromShoulderGuess != null)
            {
                if (TileToGripPoint(fromShoulderGuess.Value).Distance(legOrigin) < maxlength * 0.45f)
                    tooClose = true;
                else
                    bestGuess = fromShoulderGuess;
            }

            if (bestGuess == null)
            {
                // Look down to find a potential grab position
                if (!tooClose)
                    bestGuess = RadialDownGrabPosScan(12, 1.2f, ref tooClose, debugView);

                // Look around to find a grab position, without any raycasting
                if (tooClose)
                {
                    float radius = maxlength * (frontPair ? 0.8f : 0.6f);
                    float startAngle = frontSet ? PiOver4 : PiOver2 * 0.8f;
                    bestGuess = RadialGrabPosScan(startAngle, Pi * 0.95f, radius, debugView);
                }
            }

            // If we couldnt find anything better with the radial check, just go with the straight raycast as a fallback
            if (bestGuess == null && fromShoulderGuess.HasValue)
                bestGuess = fromShoulderGuess;

            if (bestGuess != null)
                ConfirmGrabPosition(bestGuess.Value);
        }

        /// <summary>
        /// Tries to look downwards for solid ground by raycasting from the shoulder to the desired grab position, rotated more and more towards the floor
        /// </summary>
        /// <param name="iterations">How many raycasts should happen</param>
        /// <param name="angle">How far down should the check be</param>
        /// <param name="tooClose"></param>
        /// <returns></returns>
        public Point? RadialDownGrabPosScan(int iterations, float angle, ref bool tooClose, bool debugView = false)
        {
            int i = 0;
            Point? bestGuess = null;
            Vector2 toGrabPosition = legOrigin.DirectionTo(desiredGrabPosition);

            while (i < iterations && bestGuess == null)
            {
                // Try tilting the grab position downwards until we find ground
                Vector2 tiltedGrabPosition = legOrigin + toGrabPosition.RotatedBy(i * Direction / (float)iterations * angle) * maxlength * 0.95f;

                if (debugView)
                    tiltedGrabPosition.SuperQuickDust(Color.Green);

                Vector2? trace = RaytraceTiles(legOrigin, tiltedGrabPosition, true);
                bestGuess = trace.HasValue ? trace.Value.ToTileCoordinates() : null;

                // Cant grab if the resulting grip location would be too close
                if (bestGuess.HasValue && TileToGripPoint(bestGuess.Value).Distance(legOrigin) < maxlength * 0.45f)
                {
                    bestGuess = null;
                    tooClose = true;
                }
                else
                {
                    if (debugView)
                        tiltedGrabPosition.SuperQuickDust(Color.White);

                    tooClose = false;
                }
                i++;
            }

            return bestGuess;
        }

        /// <summary>
        /// Tries to look in a radius to the side of the leg for any solid ground tile. Prioritizes gripping on tiles that are exposed to air, but cant grab inside the ground
        /// Prefers having a grab spot thats close to straight to the side
        /// </summary>
        /// <param name="angleStart"></param>
        /// <param name="angleEnd"></param>
        /// <param name="searchRadius"></param>
        /// <returns></returns>
        public Point? RadialGrabPosScan(float angleStart, float angleEnd, float searchRadius, bool debugView = false)
        {
            Vector2 origin = legOrigin;

            // If the turret is moving
            if (Math.Abs(turret.NPC.velocity.X) > 2f)
            {
                // Move the check for the pair of legs that is being dragged a bit ahead
                if (turret.NPC.velocity.X * Direction < 0)
                    origin.X += turret.NPC.velocity.X.NonZeroSign() * 90f;

                // Make the radius for the pair of legs that is moving forward a bit bigger, but not bigger than the max leg length
                else
                    searchRadius = Math.Min(maxlength, searchRadius * 1.2f);
            }

            float totalAngle = angleEnd - angleStart;
            bool lastInAir = false;
            float progress = 0f;
            float halfTileAngle = 8f / searchRadius;
            float step = halfTileAngle / totalAngle;
            List<Point> potentialGrabPoints = [];
            List<Point> insideTilesPositions = [];

            while (progress <= 1f)
            {
                float angle = (angleStart + progress * totalAngle) * Direction;
                Vector2 tiltedGrabPosition = origin + (-Vector2.UnitY).RotatedBy(angle) * searchRadius;
                Point candidate = tiltedGrabPosition.ToTileCoordinates();
                Tile t = Main.tile[candidate];

                if (t.HasUnactuatedTile && Main.tileSolid[t.TileType] || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0))
                {
                    // If we find a solid tile and we were previously in the air, thats a potential new step candidate
                    if (lastInAir)
                    {
                        potentialGrabPoints.Add(candidate);

                        if (debugView)
                            candidate.SuperQuickDust(Color.Red);
                    }
                    else
                        insideTilesPositions.Add(candidate);
                    lastInAir = false;
                }

                if (debugView)
                    candidate.SuperQuickDust(Color.Blue);

                if (!t.HasUnactuatedTile || (!Main.tileSolid[t.TileType] && !TileID.Sets.Platforms[t.TileType]))
                    lastInAir = true;

                progress += step;
            }

            if (potentialGrabPoints.Count > 0)
                return potentialGrabPoints.OrderBy(RadialPosScanRating).Last();
            else if (insideTilesPositions.Count > 0)
                return insideTilesPositions.OrderBy(RadialPosScanRating).Last();

            return null;
        }

        public float RadialPosScanRating(Point p)
        {
            Vector2 worldPos = TileToGripPoint(p);
            float length = worldPos.Distance(legOrigin);

            // Check the angle from the left so its easier to get
            Vector2 angleStart = worldPos;
            if (angleStart.X < legOrigin.X)
                angleStart.X += (legOrigin.X - angleStart.X) * 2;
            float angle = legOrigin.AngleTo(angleStart);

            Vector2 idealAngleStart = desiredGrabPosition;
            if (idealAngleStart.X < legOrigin.X)
                idealAngleStart.X += (legOrigin.X - idealAngleStart.X) * 2;
            float idealAngle = legOrigin.AngleTo(idealAngleStart);

            float idealGrabHeightBias = 0.2f + 0.8f * InverseLerp(100f, 10f, Math.Abs(turret.FloorPosition.Y - worldPos.Y));

            // Platforms with a close enough Y position are penalized to prevent from grabbing onto platforms that are going through itself
            float closePlatformScoreReduction = 0f;
            if (Main.tileSolidTop[Main.tile[p].TileType])
                closePlatformScoreReduction += InverseLerp(16f, 80f, legOrigin.Y - worldPos.Y);

            return (1 - Math.Abs(angle - idealAngle) / PiOver2) * InverseLerp(0, maxlength * 0.85f, length) * idealGrabHeightBias - closePlatformScoreReduction;
        }

        private void ConfirmGrabPosition(Point potentialGrabPosition)
        {
            if (grabPosition == null || RateGripPoint(potentialGrabPosition) > RateGripPoint(GrabTile.Value))
            {
                Vector2 attachPoint = TileToGripPoint(potentialGrabPosition);

                // Grab destination is the closest point on the tile
                if (GrabTile != null && GrabTile.Value == attachPoint.ToTileCoordinates())
                    return;

                grabPosition = attachPoint;
            }
        }

        public Vector2 TileToGripPoint(Point tilePosition)
        {
            Tile t = Main.tile[tilePosition];
            Vector2 tileWorldCoordinates = tilePosition.ToWorldCoordinates();
            Rectangle aroundTile = RectangleFromVectors(tileWorldCoordinates - Vector2.One * 9f, tileWorldCoordinates + Vector2.One * 9f);
            if (t.IsHalfBlock || t.Slope != SlopeType.Solid)
            {
                aroundTile.Y += 8;
                aroundTile.Height -= 8;
            }

            return legOrigin.ClampInRect(aroundTile);
        }

        public float RateGripPoint(Point gripPoint)
        {
            return 100f / Vector2.Distance(gripPoint.ToWorldCoordinates(), desiredGrabPosition);
        }

        public float ReleaseScore()
        {
            float releaseScore;
            if (grabPosition == null)
                releaseScore = Vector2.Distance(legTip, desiredGrabPosition);
            else
                releaseScore = Vector2.Distance(grabPosition.Value, desiredGrabPosition) * 2f;

            if (latchedOn)
                releaseScore *= 2f;

            // We really dont want to release if the other leg in the pair isnt latched on
            if (!pairedLeg.latchedOn)
            {
                releaseScore /= 100f;
            }

            int direction = leftSet ? -1 : 1;
            if ((legTip.X - turret.NPC.Center.X).NonZeroSign() != direction)
                releaseScore *= 100f;

            return releaseScore;
        }
        #endregion

        #region Drawing
        internal readonly Texture2D ForelimbAsset;
        internal readonly Texture2D LimbAsset;

        public readonly Vector2 forelegSpriteOrigin;
        public readonly Vector2 legSpriteOrigin;

        public void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SpriteEffects flip = leftSet ? SpriteEffects.FlipVertically : SpriteEffects.None;
            spriteBatch.Draw(ForelimbAsset, legOriginGraphic - screenPos, null, drawColor, legOriginGraphic.AngleTo(legKnee), forelegSpriteOrigin, turret.NPC.scale, flip, 0);
            spriteBatch.Draw(LimbAsset, legKnee - screenPos, null, drawColor, legKnee.AngleTo(legTipGraphic), legSpriteOrigin, turret.NPC.scale, flip, 0);
        }
        #endregion
    }
    #endregion

    #region Drawing
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D head = AssetRegistry.GetTexture(AdditionsTexture.AuroraTurretHead);
        Texture2D turretBase = AssetRegistry.GetTexture(AdditionsTexture.AuroraTurretBase);

        if (Legs == null)
            return false;

        foreach (AuroraTurretLegSAFE leg in Legs)
        {
            if (!leg.frontPair)
                leg.Draw(spriteBatch, screenPos, drawColor);
        }

        spriteBatch.Draw(turretBase, VisualCenter - screenPos, null, drawColor, bodyRotation, turretBase.Size() / 2, NPC.scale, bodyFlip, 0);
        spriteBatch.Draw(head, HeadCenter - screenPos, null, drawColor, headRotation, head.Size() / 2, NPC.scale, headFlip, 0);
        DrawSight();
        if (BarrelHeat > 0f)
            DrawBarrelHeat();

        foreach (AuroraTurretLegSAFE leg in Legs)
        {
            if (leg.frontPair)
                leg.Draw(spriteBatch, screenPos, drawColor);
        }

        if (BreakCompletion < 1f)
            DrawEncasement();

        if (CurrentState == AttackState.GoKablooey)
            DrawDeath();

        return false;
    }

    public void DrawSight()
    {
        Texture2D texture = AssetRegistry.InvisTex;

        Vector2 sightPos = HeadCenter + PolarVector(22f * (headFlip == SpriteEffects.FlipVertically ? -1 : 1), headRotation) + PolarVector(8f * (headFlip == SpriteEffects.FlipVertically ? 1 : -1), headRotation - PiOver2);

        float sightsSize = 500f;
        foreach (Player player in Main.ActivePlayers)
        {
            if (player != null && player.Hitbox.LineCollision(sightPos, sightPos + PolarVector(500f, headRotation), 10f))
                sightsSize = Clamp(sightPos.Distance(player.Center) * 2.4f, 10f, 500f);
        }

        float sightsResolution = 2f;
        Color color = Color.DeepSkyBlue;

        ManagedShader scope = ShaderRegistry.PixelatedSightLine;
        scope.TrySetParameter("noiseOffset", Main.GameUpdateCount * -0.003f);
        scope.TrySetParameter("mainOpacity", 1f);
        scope.TrySetParameter("resolution", new Vector2(sightsResolution * sightsSize));
        scope.TrySetParameter("rotation", -headRotation);
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

    public void DrawEncasement()
    {
        ManagedShader shader = AssetRegistry.GetShader("RadialCrackingShader");
        shader.TrySetParameter("Completion", BreakCompletion);

        Main.spriteBatch.EnterShaderRegion(BlendState.NonPremultiplied, shader.Effect);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Glacier);

        shader.Render();
        Main.spriteBatch.DrawBetter(tex, GlacierPosition, null, Color.White, 0f, tex.Size() / 2, 2f);

        Main.spriteBatch.ExitShaderRegion();
    }

    public void DrawBarrelHeat()
    {
        void heat()
        {
            Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.AuroraTurretBarrelGlow);
            Texture2D ball = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Main.spriteBatch.DrawBetter(glow, HeadCenter + PolarVector(2f, headRotation) + PolarVector(1f, headRotation - PiOver2), null, Icey * 5f * BarrelHeat, headRotation, glow.Size() / 2f, 1f);
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
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CracklingFragments>(), 1, 1, 3));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Bergcrusher>(), 3, 1, 1));
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Rimesplitter>(), 4, 1, 1));
    }
    #endregion
}
