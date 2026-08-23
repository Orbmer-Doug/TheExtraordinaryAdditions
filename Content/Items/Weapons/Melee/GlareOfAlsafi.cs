using System;
using System.Collections.Generic;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Simulations;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static System.MathF;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Utilities.QuaternionUtils;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee;

// balance thought was: looks cool
public sealed class GlareOfAlsafi : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.GlareOfAlsafi.Path;

    public override void SetDefaults()
    {
        Item.Size = new(24f);
        Item.damage = 3400;
        Item.knockBack = 4f;
        Item.crit = 46;
        Item.value = Item.buyPrice(10, 50, 25);
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.useTime = Item.useAnimation = 20;

        Item.channel = false;
        Item.UseSound = null;
        Item.useTurn = Item.noMelee = Item.noUseGraphic = Item.autoReuse = true;
        Item.rare = ModContent.RarityType<AlsafiRarity>();

        Item.shoot = ModContent.ProjectileType<AlsafiSword>();
        Item.useStyle = ItemUseStyleID.Swing;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type,
        int damage, float knockback)
    {
        AlsafiSword safi = Main
            .projectile[
                player.CreatePlayerProj(player.Center, Vector2.Zero, Item.shoot, Item.damage, Item.knockBack,
                    player.whoAmI)].As<AlsafiSword>();
        safi.CurrentState = player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed
            ? AlsafiSword.AlsafiState.Annihilation
            : AlsafiSword.AlsafiState.Cleaving;

        return false;
    }

    public override bool CanShoot(Player player) => true;

    public override bool AltFunctionUse(Player player) => true;
}

public sealed class AlsafiSword : ModProjectile
{
    #region Variables

    public enum SwingDirection : sbyte
    {
        Down = 1,
        Up = -1,
    }

    public enum AlsafiState : byte
    {
        Cleaving,
        Crush,
        Annihilation
    }

    public override string Texture => AssetRegistry.GennedTextures.GlareOfAlsafi.Path;
    public Texture2D Tex => Projectile.ThisProjectileTexture();

    public int Time;

    /// <summary>
    /// Hitlag
    /// </summary>
    public float TimeStop;

    public bool PlayedSound;

    public float VanishTime;
    public float OverallTime;
    public float RotationOffset;

    public bool Initialized;

    public float InitialMouseAngle;

    public SwingDirection SwingDir;

    public AlsafiState CurrentState;

    public Quaternion[] OldRotations = new Quaternion[5];

    public SpriteEffects Effects
    {
        get => (SpriteEffects) Projectile.spriteDirection;
        set => Projectile.spriteDirection = (int) value;
    }

    public int Direction
    {
        get => Projectile.direction;
        set => Projectile.direction = value;
    }

    /// <summary>
    /// How many times to update this projectile per frame
    /// </summary>
    public int MaxUpdates { get; set; } = 3;

    /// <summary>
    /// The rotation of this sword in 3D space
    /// </summary>
    public Quaternion Rotation;

    public float ForwardAngle;

    public const int MaxTrailPoints = 30;
    public TrailPoints3D Points = new(MaxTrailPoints);
    public float TrailAlpha;

    public Player Owner => Main.player[Projectile.owner];
    public PlayerMouse Modded => Owner.AdditionsMouse();
    public Item Item => Owner.HeldItem;
    public float MeleeScale => Owner.GetAdjustedItemScale(Item);
    public float MeleeSpeed => Owner.GetTotalAttackSpeed(DamageClass.MeleeNoSpeed);

    /// <summary>
    /// The owners center
    /// </summary>
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter);

    /// <summary>
    /// A quick way to access the current light level of this sword
    /// </summary>
    public float Brightness
    {
        get
        {
            Point point = Projectile.Center.ToTileCoordinates();
            return Lighting.Brightness(point.X, point.Y);
        }
    }

    public int SwingTime
    {
        get
        {
            return CurrentState switch
            {
                AlsafiState.Cleaving => 25,
                AlsafiState.Crush => CrushTotalTime,
                AlsafiState.Annihilation => 1,
                _ => 0
            };
        }
    }

    public int MaxTime => (int) (SwingTime * MaxUpdates / MeleeSpeed);
    public int StopTimeFrames => 4;
    public float StopTime => (StopTimeFrames - (int) ((MeleeSpeed - 1) * 5f)) * MaxUpdates;

    public float SwingCompletion => InverseLerp(0f, MaxTime, Time);
    public Vector2 SwordDirection;

    public RotatedRectangle Rect()
    {
        Vector2 size = Tex.Size();
        Vector2 visibleSize = new(size.X / 2f, size.Y);
        Vector3 size3D = new(0, visibleSize.Y, 0);

        Quaternion angleFix = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Pi);
        Quaternion final = Quaternion.Concatenate(Rotation, angleFix);

        Vector3 tip = Vector3.Transform(size3D,
            Quaternion.Concatenate(final, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, InitialMouseAngle)));
        Vector2 begin = Projectile.Center;
        Vector2 end = begin + new Vector2(tip.X, tip.Y);
        float projectedWidth = visibleSize.X * Abs(Cos(final.AngleAroundZ()));

        RotatedRectangle rect = new(projectedWidth, begin, end);
        return rect;
    }

    #endregion

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.CanHitPastShimmer[Type] = true;
        ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false; // Manual
    }

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.MeleeNoSpeed;

        Projectile.timeLeft = 10000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;

        Projectile.penetrate = -1;
        Projectile.MaxUpdates = MaxUpdates;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;

        Projectile.ContinuouslyUpdateDamageStats = true;

        Projectile.noEnchantmentVisuals = true;
        Projectile.netImportant = true;
    }

    #region Collision

    /// <summary>
    /// Defaults to <see cref="Rect"/> seeing if it intersects with a target.
    /// </summary>
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Rect().Intersects(targetHitbox);
    }

    public override bool? CanDamage() => CurrentState == AlsafiState.Annihilation ? Points.LeadingOpacity > .4f :
        SwingCompletion is >= .2f and <= .8f ? null : false;

    public override void CutTiles()
    {
        RotatedRectangle rect = Rect();
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(rect.BottomLeft, rect.TopRight, rect.Width, DelegateMethods.CutTiles);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ItemLoader.OnHitNPC(Owner.HeldItem, Owner, target, hit, damageDone);
        NPCLoader.OnHitByItem(target, Owner, Owner.HeldItem, hit, damageDone);
        PlayerLoader.OnHitNPC(Owner, target, hit, damageDone);

        RotatedRectangle rect = Rect();

        (Vector2 start, Vector2 end)? line = rect.GetIntersectionLine(rect.Bottom, rect.Top);
        if (Simulation != null && line.HasValue)
        {
            Vector2 relative = target.Center - Simulation.Center;
            Vector2 gridOffset = relative / Simulation.Scale;
            Vector2 gridPos = Simulation.GridSize * 0.5f + gridOffset;
            Simulation.DrawOmnidirectional(1.3f, 5f, gridPos, Vector2.One * 100f);
        }

        ScreenShakeSystem.New(new(.6f, .3f), target.Center);

        if (!HitTargetNPC)
        {
            TargetNPCIndex = target.whoAmI;
            SavedNPCCenter = target.Center;
            HitTargetNPC = true;
        }

        if (TargetNPCAbsolutelyFlung && target == Main.npc[TargetNPCIndex])
        {
            Vector2 start = target.Center;
            float dir = SwordDirection.ToRotation();
            Vector2 end = start + PolarVector(2000f, dir);
            target.Center = RaycastTiles(start, end) ?? end;

            for (int i = 0; i < 110; i++)
            {
                Vector2 vel = -PolarVector(Main.rand.NextFloat(4f, 30f), dir).RotatedByRandom(.8f);

                Color col = Color.Lerp(Color.Chocolate, Color.OrangeRed, Main.rand.NextFloat(.2f, .6f));
                float scale = Main.rand.NextFloat(240f, 290f);
                int life = Main.rand.Next(10, 20);
                ParticleRegistry.SpawnGlowParticle(target.Center, vel * .1f, life, scale, col, 3f);
                ParticleRegistry.SpawnSparkParticle(target.Center + vel * 10f, vel * 4.46f, life * 25,
                    Main.rand.NextFloat(.4f, .9f), Color.Lerp(col, Color.DarkOrange, .4f), true);

                ParticleRegistry.SpawnSquishyPixelParticle(target.Center, vel * Main.rand.NextFloat(1.5f, 2.2f),
                    Main.rand.Next(170, 190), Main.rand.NextFloat(8.4f, 12.2f), col,
                    Color.Lerp(col, Color.OrangeRed, .6f) * 1.2f, 8, false, true);
            }


            Projectile.CreateProj(target.Center, Vector2.Zero, ModContent.ProjectileType<AlsafiExplosion>(), 500_000, 0f,
                Owner.whoAmI);
            Projectile.CreateProj(target.Center - Vector2.UnitY * 80f, Vector2.Zero,
                ModContent.ProjectileType<AlsafiPlasmaFlare>(), 1000, 0f, Owner.whoAmI);

            AssetRegistry.GennedSounds.harpoonStop.Play(rect.Center, 1.1f, -.3f, .1f);
            AssetRegistry.GennedSounds.MeteorImpact.Play(target.Center, 1.8f);
            AssetRegistry.GennedSounds.LargeWeaponFireDifferent.Play(target.Center, 1.1f, -.2f);

            ParticleRegistry.SpawnShockwaveParticle(target.Center, 30, 1.4f, 1000f, 400f, .5f);
            ParticleRegistry.SpawnBlurParticle(target.Center, 50, 2f, 2000f, .4f);
            ScreenShakeSystem.New(new(.5f, .3f), target.Center);

            const int frames = 5;
            ParticleRegistry.SpawnChromaticAberration(start, frames, 2f, 5000f);
            ImpactSystem.QueueImpact(frames);
            TimeStopSystem.StopFrames = frames * 2;

            // Create some interesting cracks in the sky during impact
            for (int i = 0; i < 40; i++)
            {
                ParticleRegistry.SpawnLightningArcParticle(start, Main.rand.NextVector2CircularEdge(1500f, 1500f),
                    frames, 20f, Color.Blue);
            }
        }
        else
        {
            AssetRegistry.GennedSounds.BraveSmashS02.Play(target.Center, .9f, .2f, .3f, 20);
        }
    }

    #endregion

    public override void AI()
    {
        if (!Owner.Available())
        {
            KillEffect();
        }

        Projectile.width = Tex?.Width ?? 1;
        Projectile.height = Tex?.Height ?? 1;

        if (TimeStop <= 0f)
        {
            for (int i = OldRotations.Length - 1; i > 0; i--)
            {
                OldRotations[i] = OldRotations[i - 1];
            }

            OldRotations[0] = Rotation;
        }

        Projectile.rotation = Rotation.AngleAroundZ();

        if (!Initialized)
        {
            Points.Clear();

            Projectile.ResetLocalNPCHitImmunity();

            // Reset time and sync
            PlayedSound = false;

            Projectile.velocity = Center.SafeDirectionTo(Modded.MouseWorld);
            Direction = Projectile.velocity.X.NonZeroSign();
            if (CurrentState == AlsafiState.Crush)
                InitialMouseAngle = Direction == 1 ? 0f : Pi;
            else
                InitialMouseAngle = Projectile.velocity.ToRotation();
            Time = 0;

            Initialized = true;
        }

        #region Logic

        switch (CurrentState)
        {
            case AlsafiState.Cleaving:
                DoState_Cleaving();
                break;
            case AlsafiState.Crush:
                DoState_Crush();
                break;
            case AlsafiState.Annihilation:
                DoState_Annilihation();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateFluids();

        Projectile.Center = Owner.GetFrontHandPositionImproved();

        // Find the orthonormal key frame of the blade 
        Vector3 size3D = new(0, Projectile.height, 0);
        Quaternion angleFix = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Pi);
        Quaternion fixedRot = Quaternion.Multiply(Rotation, angleFix);
        Quaternion final = Quaternion.Concatenate(fixedRot,
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, InitialMouseAngle));
        Quaternion normalRot = Quaternion.Concatenate(final, Quaternion.CreateFromAxisAngle(final.Forward(), PiOver2));
        Vector3 normal = Vector3.Normalize(Vector3.Transform(size3D, normalRot));
        Vector3 center = Vector3.Transform(size3D * .9f, final);
        Vector3 finalNormal = Vector3.Cross(center, normal);

        // Compute angular delta between the two most recent recorded rotations
        // The dot product gives cos(half-angle) between the two orientations
        float dot = Math.Clamp(Math.Abs(Quaternion.Dot(OldRotations[0], OldRotations[1])), 0f, 1f);

        // Gives the full rotation angle
        float angularDelta = 2f * Acos(dot);

        // The delta (in radians per frame) at which alpha reaches 1
        const float referenceAngularSpeed = 0.12f;

        // Smoothly remap delta onto [0,1] with a slight ease so that very slow motion
        // fades hard and moderate motion looks full. SmoothStep avoids a harsh knee.
        float smoothAlpha = SmoothStep(0f, 1f, angularDelta / referenceAngularSpeed);

        // Lerp toward the new value rather than snapping, so fast to slow transitions bleed out gracefully over a few frames rather than cutting off abruptly
        const float smoothing = 0.18f;
        TrailAlpha = Lerp(TrailAlpha, smoothAlpha, 1f - Pow(1f - smoothing, 1f / MaxUpdates));
        Points.Update(new(center, finalNormal, TrailAlpha));

        RotatedRectangle rect = Rect();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, rect.Top.AngleTo(rect.Bottom));
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
        }
        else
        {
            Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 20f * MaxUpdates, 1f, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

        #endregion

        Vector3 dir = Vector3.Transform(Vector3.UnitY, normalRot);
        SwordDirection = new Vector2(dir.X, dir.Y).SafeNormalize(Vector2.Zero) *
                         (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;

        if (!Main.dedServ)
        {
            WaterShaderData water = (WaterShaderData) Filters.Scene["WaterDistortion"].GetShader();
            const float power = 12f;
            float waveSine = 1f * (float) Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
            Vector2 size = Projectile.Size / 2f;
            Vector2 ripplePos = rect.Center;
            Color waveData = new Color(power, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
            water.QueueRipple(ripplePos, waveData, size, RippleShape.Square, SwordDirection.ToRotation());
        }

        switch (TimeStop)
        {
            case <= 0f:
                Time++;
                break;
            case > 0f:
                TimeStop--;
                break;
        }

        OverallTime++;
        AlsafiRenderer.Register(this);
    }

    public const int CleaveTotalTime = 36;

    public int SwingCounter;

    public void DoState_Cleaving()
    {
        bool dir = Direction == 1;

        Quaternion sliceStart = CreateFromPolarAngles(-.06f, .3f, dir);
        Quaternion sliceAnticipation = CreateFromPolarAngles(-1.96f, .8f, dir);
        Quaternion sliceSlash = CreateFromPolarAngles(0f, 1.3f, dir);
        Quaternion slice = new PiecewiseRotation().Add(Sine.InFunction, sliceAnticipation, 0.3f, sliceStart)
            .Add(MakePoly(4f).OutFunction, sliceSlash, 1f, null, false)
            .Evaluate(SwingCompletion);

        Quaternion sweepEnd = CreateFromPolarAngles(ThreePIOver2, 1.3f, dir);
        Quaternion sweep = sliceSlash.SlerpLong(sweepEnd, MakePoly(5f).InOutFunction(SwingCompletion));

        Quaternion slamAnticipation = CreateFromPolarAngles(ThreePIOver2 + .4f, clockwise: dir);
        Quaternion slamSlash = CreateFromPolarAngles(Pi + .2f, clockwise: dir);
        Quaternion slam = new PiecewiseRotation().Add(Sine.InFunction, slamAnticipation, 0.3f, sweepEnd)
            .Add(MakePoly(5f).OutFunction, slamSlash, 1f, null, false)
            .Evaluate(SwingCompletion);

        Quaternion up = slamSlash.SlerpLong(sliceStart, Expo(2.2f).InOutFunction(SwingCompletion));

        if (Time == 0)
        {
            AssetRegistry.GennedSounds.BigSwing2.Play(Owner.Center, .84f, -.1f, .2f, 10);
            AssetRegistry.GennedSounds.FireBeamEnd.Play(Owner.Center, 1.24f, -.1f, .3f, 10);
        }

        switch (SwingCounter % 4)
        {
            case 0:
                Rotation = slice;
                SwingDir = SwingDirection.Up;
                break;
            case 1:
                Rotation = sweep;
                SwingDir = SwingDirection.Down;
                break;
            case 2:
                Rotation = slam;
                SwingDir = SwingDirection.Down;
                break;
            case 3:
                Rotation = up;
                SwingDir = SwingDirection.Up;
                break;
        }

        // swoosh
        if (!PlayedSound)
        {
            PlayedSound = true;
        }

        // Reset if still holding left, otherwise fade
        if (SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                SwingCounter = (SwingCounter + 1) % 4;
                Initialized = false;
            }
            else
                VanishTime++;
        }
    }

    public const int CrushReelTime = 90;
    public const int CrushSlamTime = 45;
    public const int CrushTotalTime = CrushReelTime + CrushSlamTime;
    public bool Collided;
    public int CollideWait;

    public void DoState_Crush()
    {
        bool dir = Direction == 1;

        Quaternion start = CreateFromPolarAngles(PiOver2, clockwise: dir);
        Quaternion anticipation = CreateFromPolarAngles(PiOver2 + .4f, clockwise: dir);
        Quaternion reel = CreateFromPolarAngles(ThreePIOver2, 1.2f, clockwise: dir);

        PiecewiseRotation crush = new PiecewiseRotation()
            .Add(MakePoly(2.8f).OutFunction, anticipation, 0.3f, start)
            .Add(MakePoly(3.5f).InOutFunction, reel, .8f, null, false)
            .Add(MakePoly(4.6f).InFunction, anticipation, 1f, null, false);

        Rotation = crush.Evaluate(SwingCompletion);
        SwingDir = SwingDirection.Down;

        if (!(SwingCompletion >= 1f))
            return;

        Owner.velocity.X *= .4f;
        Owner.velocity.Y += 2f;
        Owner.AdditionsMove().FastFall = true;
        if (Owner.velocity.Y > 75f)
            Owner.velocity.Y = 75f;

        RotatedRectangle rect = Rect();
        if (rect.SolidCollision())
        {
            if (!Collided)
            {
                Vector2 pos = rect.Center;
                for (int i = 0; i < 70; i++)
                {
                    Vector2 vel = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-PiOver2, PiOver2)) *
                                  Main.rand.NextFloat(5f, 20f);
                    Color col = Color.Lerp(Color.Chocolate, Color.OrangeRed, Main.rand.NextFloat(.2f, .6f));
                    float scale = Main.rand.NextFloat(240f, 290f);
                    int life = Main.rand.Next(10, 20);
                    ParticleRegistry.SpawnGlowParticle(pos, vel * .1f, life, scale, col, 3f);
                    ParticleRegistry.SpawnSparkParticle(pos + vel * 10f, vel * 4.46f, life * 25,
                        Main.rand.NextFloat(.4f, .9f), Color.Lerp(col, Color.DarkOrange, .4f), true);

                    ParticleRegistry.SpawnSquishyPixelParticle(pos, vel * Main.rand.NextFloat(1.5f, 2.2f),
                        Main.rand.Next(170, 190), Main.rand.NextFloat(8.4f, 12.2f), col,
                        Color.Lerp(col, Color.OrangeRed, .6f) * 1.2f, 8, false, true, .015f * Direction);

                    ParticleRegistry.SpawnBloomLineParticle(pos, vel * Main.rand.NextFloat(2f, 4f),
                        Main.rand.Next(20, 25), Main.rand.NextFloat(2.5f, 3.1f), col);
                }

                ParticleRegistry.SpawnBlurParticle(pos, 40, .5f, 400f, .4f);
                ParticleRegistry.SpawnShockwaveParticle(pos, 30, .2f, 1900f, 600f);
                ParticleRegistry.SpawnChromaticAberration(pos, 100, .7f, 800f);

                Collided = true;
            }

            if (Collided)
                CollideWait++;

            if (CollideWait > 30)
                VanishTime++;
        }
    }

    public enum AnnilihationState
    {
        Swipe,
        Turn,
        UpSlash,
        Reel,
        Slam
    }

    public AnnilihationState CurrentAnnilihationState;

    public int AnnilihationSwipeTime = 55;
    public int AnnilihationTurnTime = 45;
    public int AnnilihationUpSlashTime = 95;
    public int AnnilihationReelTime = 70;
    public int AnnilihationSlamTime = 28;

    public bool HitTargetNPC;
    public int TargetNPCIndex = -1;
    public Vector2 SavedNPCCenter;
    public Vector2 SavedOwnerCenter;
    public bool TargetNPCAbsolutelyFlung;

    public void DoState_Annilihation()
    {
        bool dir = Direction == 1;
        SwingDir = SwingDirection.Down;

        Quaternion start = CreateFromPolarAngles(0f, 1.4f, dir);
        Quaternion swipeEnd = CreateFromPolarAngles(ThreePIOver2, 1.4f, dir);

        Quaternion turn = CreateFromPolarAngles(ThreePIOver2, Pi, dir);

        Quaternion slash = CreateFromPolarAngles(ThreePIOver2 - PiOver4, Pi, dir);

        Quaternion reel = CreateFromPolarAngles(ThreePIOver2 + PiOver4 - .3f, 0f, dir);

        Quaternion slam = CreateFromPolarAngles(ThreePIOver2 - .2f, 0f, dir);

        NPC npc = TargetNPCIndex == -1 ? null : Main.npc[TargetNPCIndex];
        if (npc != null)
        {
            npc.velocity = Vector2.Zero;
            Owner.AdditionsMove().DisableAllMovement = true;
        }

        switch (CurrentAnnilihationState)
        {
            case AnnilihationState.Swipe:
                Rotation = start.SlerpLong(swipeEnd,
                    MakePoly(2f).OutFunction(InverseLerp(0f, AnnilihationSwipeTime, Time)));
                if (Time > AnnilihationSwipeTime)
                {
                    if (npc != null)
                        NextState();
                    else
                        VanishTime++;
                }

                break;
            case AnnilihationState.Turn:
                Rotation = swipeEnd.Slerp(turn,
                    MakePoly(2f).InOutFunction(InverseLerp(0f, AnnilihationTurnTime, Time)));
                if (Time > AnnilihationTurnTime)
                    NextState();
                break;
            case AnnilihationState.UpSlash:
                float slashInterpol = InverseLerp(0f, AnnilihationUpSlashTime, Time);
                Rotation = turn.SlerpLong(slash,
                    MakePoly(4.1f).InOutFunction(slashInterpol));

                float riseInterpol = MakePoly(4f).OutFunction(slashInterpol);
                float len = npc.Size.Length();
                float height = len + 600f;
                float horiz = MathF.Max(len, 120f);
                npc.Center = Vector2.Lerp(SavedNPCCenter, SavedNPCCenter - Vector2.UnitY * height, riseInterpol);
                Owner.Center = Vector2.Lerp(SavedOwnerCenter, npc.Center + new Vector2(horiz * -Direction, -40f),
                    riseInterpol);

                if (Time > AnnilihationUpSlashTime)
                    NextState();
                break;
            case AnnilihationState.Reel:
                Rotation = slash.Slerp(reel, MakePoly(2f).InFunction(InverseLerp(0f, AnnilihationReelTime, Time)));
                if (Time > AnnilihationReelTime)
                {
                    SavedNPCCenter = npc.Center;
                    TargetNPCAbsolutelyFlung = true;
                    NextState();
                }

                break;
            case AnnilihationState.Slam:
                Rotation = reel.SlerpLong(slam, Expo(1.4f).OutFunction(InverseLerp(0f, AnnilihationSlamTime, Time)));
                if (Time > AnnilihationSlamTime)
                {
                    VanishTime++;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return;

        void NextState()
        {
            SavedOwnerCenter = Center;
            CurrentAnnilihationState = (AnnilihationState) ((int) CurrentAnnilihationState + 1);
            Projectile.ResetLocalNPCHitImmunity();
            Time = 0;
        }
    }

    public void KillEffect()
    {
        Projectile.Kill();
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // We want the target to die at the impact, not the sword
        if (TargetNPCAbsolutelyFlung)
        {
            modifiers.DisableCrit();
            modifiers.FinalDamage *= 0.01f;
        }
    }

    /// <summary>
    /// Glue to the player
    /// </summary>
    public override bool ShouldUpdatePosition() => false;

    #region Rendering

    public static float WidthFunct(float c)
    {
        return 242f;
    }

    public Color ColorFunct(SystemVector2 c)
    {
        return Color.Orange;
    }

    public SystemVector3 OffsetFunct(float c) => new(Projectile.Center.ToNumerics(), 0f);

    /// <summary>
    /// A target for the brush of the fluid simulation at the size of the screen to account for any cases of zoom
    /// </summary>
    public ManagedRenderTarget SwordTarget;

    private static readonly FluidSettings AlsafiFluidSettings = new()
    {
        GridWidth = 2048,
        GridHeight = 2048,
        DivergenceClearanceIterations = 5,
        DiffusionCoefficient = 0f,
        DissipationDecayFactor = 0.973f,
        CollidesWithTiles = false,
        Vorticity = 0.20f,
        NoiseInjectionAcceleration = 0f,
    };

    internal FluidSimulationHandle Simulation;

    public void UpdateFluids()
    {
        if (SwordTarget == null || SwordTarget.IsDisposed)
        {
            Main.QueueMainThreadAction(() => { SwordTarget = new ManagedRenderTarget(true, CreateScreenSizedTarget); });
        }

        if (SwordTarget == null)
            return;

        Simulation = ModContent.GetInstance<AlsafiFluidResidueSystem>()
            .RegisterActivity(Owner, AlsafiFluidSettings, 1f);

        if (Simulation == null)
            return;

        Simulation.Delta = (Owner.oldPosition - Owner.position) / Simulation.Scale * .5f;
        Simulation.Center = Owner.Center;

        if (VanishTime <= 0)
        {
            Vector2 pos = Simulation.GridSize / 2;
            Texture2D brush = SwordTarget;
            float dot = Math.Clamp(Math.Abs(Quaternion.Dot(OldRotations[0], OldRotations[1])), 0f, 1f);
            float angularDelta = 2f * Acos(dot);
            Vector2 vel = -SwordDirection * angularDelta * 5f;
            Simulation.DrawOnCanvas(brush, .2f * Projectile.scale, vel, pos, null, 0f,
                SwordTarget.Size() / 2f, Vector2.One);
        }
    }

    public override void OnKill(int timeLeft)
    {
        Main.QueueMainThreadAction(() =>
        {
            SwordTarget?.Dispose();
            SwordTarget = null;
        });
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (SwordTarget != null)
        {
            Main.spriteBatch.End(out SpriteBatchSnapshot ss);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Matrix.Identity);
            Main.spriteBatch.Draw(SwordTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.Restart(ss);
        }

        return false;
    }

    #endregion
}

public sealed class AlsafiRenderer : ModSystem
{
    private static readonly List<AlsafiSword> CurrentSwords = [];

    public static void Register(AlsafiSword sword)
    {
        if (!CurrentSwords.Contains(sword) && CurrentSwords.Count < Main.maxPlayers)
            CurrentSwords.Add(sword);
    }

    public override void PostUpdateProjectiles()
    {
        for (int i = CurrentSwords.Count - 1; i >= 0; i--)
        {
            AlsafiSword sword = CurrentSwords[i];
            if (sword == null || !sword.Projectile.active)
                CurrentSwords.RemoveAt(i);
        }
    }

    public override void OnModLoad()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent += RenderAllSwords;
    }

    public override void OnModUnload()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent -= RenderAllSwords;
    }

    private static void RenderAllSwords()
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        foreach (AlsafiSword sword in CurrentSwords)
        {
            gd.SetRenderTarget(sword.SwordTarget);
            gd.Clear(Color.Transparent);

            Vector2 center = sword.Projectile.Center;
            Vector2 correction = Main.screenLastPosition - Main.screenPosition;
            int dir = (sword.Direction < 0 ? -(int) sword.SwingDir : (int) sword.SwingDir) * (int) sword.Owner.gravDir;
            Draw3D(sword.Tex, center + correction, sword.Rotation, sword.Projectile.scale, sword.InitialMouseAngle,
                Color.White, new Vector2(.5f, 0f), dir == -1);
        }

        gd.SetRenderTarget(null);
    }
}

public sealed class AlsafiFluidResidueSystem : ModSystem
{
    public const int ResidueLifetime = 300;

    public const int MaxConcurrentSlots = 8;

    private struct Slot
    {
        public FluidSimulationHandle Handle;

        public int TimeLeft;

        public bool Fed;

        public readonly bool IsActive => Handle is { InUse: true };
    }

    private static Slot[] _slots;

    private int _activeSlotCount;

    public override void Load()
    {
        Main.QueueMainThreadAction(() => { On_Main.DrawProjectiles += Render; });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => { On_Main.DrawProjectiles -= Render; });
    }

    public override void OnWorldLoad() => _slots = new Slot[MaxConcurrentSlots];

    public override void OnWorldUnload()
    {
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
            ReleaseSlot(i);

        _activeSlotCount = 0;
    }

    public FluidSimulationHandle RegisterActivity(Player player, FluidSettings requestSettings, float scale)
    {
        if (_slots == null || player == null || player.whoAmI < 0 || player.whoAmI >= _slots.Length)
            return null;

        ref Slot slot = ref _slots[player.whoAmI];

        if (slot.IsActive)
        {
            slot.TimeLeft = ResidueLifetime;
            slot.Fed = true;
            return slot.Handle;
        }

        if (_activeSlotCount >= MaxConcurrentSlots)
            return null;

        int slotIndex = player.whoAmI;
        requestSettings.AutomaticDisposalFunction = () => !_slots[slotIndex].Fed && _slots[slotIndex].TimeLeft <= 0;

        FluidSimulationHandle handle = FluidSimulationProcessor.Instance.RequestNew(scale, requestSettings);
        if (handle == null)
            return null;

        slot.Handle = handle;
        slot.TimeLeft = ResidueLifetime;
        slot.Fed = true;
        _activeSlotCount++;

        return handle;
    }

    public override void PostUpdateEverything()
    {
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            ref Slot slot = ref _slots[i];
            if (!slot.IsActive)
                continue;
            if (Main.netMode != NetmodeID.Server)
            {
                for (int j = 0; j < 3; j++)
                    slot.Handle.ForceUpdate();
            }

            if (slot.Fed)
            {
                slot.Fed = false;
                continue;
            }

            slot.TimeLeft--;

            if (slot.TimeLeft <= 0)
                ReleaseSlot(i);
        }
    }

    private void ReleaseSlot(int index)
    {
        ref Slot slot = ref _slots[index];
        if (slot.Handle != null)
        {
            FluidSimulationProcessor.Release(slot.Handle);
            _activeSlotCount = Math.Max(0, _activeSlotCount - 1);
        }

        slot = default;
    }

    #region Rendering

    private static void Render(On_Main.orig_DrawProjectiles orig, Main self)
    {
        if (!Main.dedServ && _slots != null)
        {
            SpriteBatch sb = Main.spriteBatch;

            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                if (!slot.IsActive)
                    continue;

                DrawSlot(sb, slot.Handle);
            }
        }

        orig(self);
    }

    private static void DrawSlot(SpriteBatch sb, FluidSimulationHandle handle)
    {
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

        ManagedShader shader = AssetRegistry.GennedShaders.FireShader;
        shader.TrySetParameter("finalColorExponent", 1.5f);
        shader.SetTexture(AssetRegistry.GennedTextures.SuperWavyPerlin, 1, SamplerState.LinearWrap);
        shader.Render();

        Texture2D target = handle.VelocityDensityTarget;
        sb.Draw(target, handle.Center - Main.screenPosition, null,
            Color.Coral, 0f, target.Size() / 2f, handle.Scale, SpriteEffects.None, 0f);

        sb.End();
    }

    #endregion
}

public sealed class AlsafiExplosion : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.NoLiquidDistortion[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(1800f);
        Projectile.timeLeft = 10;
        Projectile.knockBack = 10f;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = Projectile.friendly = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = 10;
        Projectile.noEnchantmentVisuals = true;
    }
}

public sealed class AlsafiPlasmaFlare : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.NoLiquidDistortion[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(4800f);
        Projectile.timeLeft = Lifetime;
        Projectile.knockBack = 0f;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = Projectile.friendly = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 2;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = Lifetime;
        Projectile.noEnchantmentVisuals = true;
    }

    internal FluidSimulationHandle simulation;

    public int Time { get; set; }
    public int Lifetime = 100;
    public float LifetimeRatio => Time / (float) Lifetime;

    public override void AI()
    {
        if (simulation == null || !simulation.InUse)
        {
            Vector2 gridSize = new(1024f, 1024f);
            float scale = Projectile.width / gridSize.X;
            simulation = FluidSimulationProcessor.Instance.RequestNew(scale, new FluidSettings()
            {
                GridWidth = (int) gridSize.X,
                GridHeight = (int) gridSize.Y,
                DivergenceClearanceIterations = 12,
                DiffusionCoefficient = 0.0f,
                DissipationDecayFactor = 0.983f,
                CollidesWithTiles = true,
                Vorticity = 0.4f,
                NoiseInjectionAcceleration = 0f,
                AutomaticDisposalFunction = () => !Projectile.active
            });
        }

        if (simulation == null)
            return;

        simulation.Center = Projectile.Center;

        Vector2 origin = simulation.GridSize * 0.5f;
        Texture2D brush = AssetRegistry.GennedTextures.Pixel;
        float scaleFactor = Main.rand.NextFloat(0.2f, 1.4f);
        Vector2 sourceScale = Vector2.One * scaleFactor * (30f * Projectile.Opacity) / brush.Size();
        Vector2 sourcePosition = origin + Main.rand.NextVector2Circular(8f, 8f);

        if (Time <= Lifetime * 0.85f)
        {
            simulation.AddForce(-Vector2.UnitY * .01f);
            simulation.DrawOmnidirectional(.7f * Projectile.Opacity, 14f * Projectile.Opacity, sourcePosition,
                sourceScale);
        }

        if (Projectile.numUpdates == -1 && Main.netMode != NetmodeID.Server)
        {
            for (int i = 0; i < 3; i++)
                simulation.ForceUpdate();
        }

        Projectile.Opacity = InverseLerp(1f, 0.7f, LifetimeRatio);
        Projectile.damage = (int) (Projectile.originalDamage * Projectile.Opacity);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (simulation == null || !simulation.InUse)
        {
            return false;
        }

        Main.spriteBatch.End(out SpriteBatchSnapshot ss);
        Main.spriteBatch.Begin(ss with
        {
            SortMode = SpriteSortMode.Immediate, SamplerState = SamplerState.LinearClamp
        });

        ManagedShader shader = AssetRegistry.GennedShaders.FireShader;
        shader.TrySetParameter("finalColorExponent", 1.2f);
        shader.SetTexture(AssetRegistry.GennedTextures.PerlinCloud, 1, SamplerState.LinearWrap);
        shader.Render();

        Texture2D target = simulation.VelocityDensityTarget;
        Main.spriteBatch.Draw(target, Projectile.Center - Main.screenPosition, null,
            Color.DarkOrange * Projectile.Opacity, 0f, target.Size() * 0.5f,
            new Vector2(simulation.Scale), 0, 0f);
        Main.spriteBatch.Restart(ss);

        return false;
    }
}
