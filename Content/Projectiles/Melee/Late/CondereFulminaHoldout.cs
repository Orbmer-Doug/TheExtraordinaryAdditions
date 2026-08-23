using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class CondereFulminaHoldout : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.CondereFulmina.Path;
    public Player Owner => Main.player[Projectile.owner];
    public PlayerMouse Modded => Owner.AdditionsMouse();
    public ref float Time => ref Projectile.ai[0];

    public const int ReelTime = 30;
    public const int Charge1 = 30;
    public const int Charge2 = 20;
    public const int Charge3 = 10;
    public const int Charge4 = 5;
    public const int TotalReelTime = ReelTime + Charge1 + Charge2 + Charge3 + Charge4;
    public const int ThrowTime = 40;

    public enum FulminaState
    {
        Aiming,
        Firing,
    }

    public FulminaState State
    {
        get => (FulminaState) Projectile.ai[1];
        set => Projectile.ai[1] = (int) value;
    }

    public enum FulminaCharge
    {
        None,
        First,
        Second,
        Third,
        Fourth,
    }

    public FulminaCharge Charge
    {
        get => (FulminaCharge) Projectile.ai[2];
        set => Projectile.ai[2] = (int) value;
    }

    public ref float OldArmRot => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float TotalTime => ref Projectile.AdditionsInfo().ExtraAI[1];

    public Vector2 Tip => Projectile.RotHitbox().TopRight;
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter);
    public int Dir => Projectile.velocity.X.NonZeroSign();

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(184);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = 1;
    }

    public void OwnerDefaults()
    {
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);
        Owner.SetDummyItemTime(2);
    }

    public override void AI()
    {
        after ??= new(14, () => Projectile.Center);
        switch (State)
        {
            case FulminaState.Aiming:
                if (this.RunLocal())
                {
                    Projectile.velocity = Center.SafeDirectionTo(Modded.MouseWorld);
                    if (Projectile.velocity != Projectile.oldVelocity)
                        this.Sync();
                }

                Projectile.Center = Owner.GetFrontHandPositionImproved();
                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
                OwnerDefaults();

                float vel = Projectile.velocity.ToRotation();
                float reelAnim = MakePoly(3f).InOutFunction.Evaluate(vel, vel - (2f * Dir * Owner.gravDir),
                    InverseLerp(0f, ReelTime, Time));
                Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, reelAnim);
                OldArmRot = reelAnim;

                switch (Charge)
                {
                    case FulminaCharge.None:
                        if (Time >= Charge1)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(5f, 5f, .4f, 1f), Main.rand.Next(15, 20),
                                    Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                                ParticleRegistry.SpawnBloomPixelParticle(Tip, RandomVelocity(1f, 1f, 4f),
                                    Main.rand.Next(30, 50), Main.rand.NextFloat(.1f, .3f), Color.Cyan, Color.White);
                            }

                            SoundID.DD2_LightningAuraZap.Play(Tip, 1.3f, .1f);
                            Charge = FulminaCharge.First;
                        }

                        break;
                    case FulminaCharge.First:
                        if (Time >= (Charge1 + Charge2))
                        {
                            for (int i = 0; i < 3; i++)
                                ParticleRegistry.SpawnLightningArcParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(60f, 60f, .5f, 1f), Main.rand.Next(8, 11),
                                    Main.rand.NextFloat(.1f, .4f), Color.Cyan);

                            for (int i = 0; i < 20; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f), Main.rand.Next(15, 20),
                                    Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                                ParticleRegistry.SpawnBloomPixelParticle(Tip, RandomVelocity(1f, 1f, 5f),
                                    Main.rand.Next(40, 60), Main.rand.NextFloat(.1f, .3f), Color.Cyan, Color.White);
                            }

                            SoundID.DD2_LightningAuraZap.Play(Tip, 1.7f, 0f);
                            Charge = FulminaCharge.Second;
                        }

                        break;
                    case FulminaCharge.Second:
                        if (Time >= (Charge1 + Charge2 + Charge3))
                        {
                            for (int i = 0; i < 5; i++)
                                ParticleRegistry.SpawnLightningArcParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(80f, 80f, .5f, 1f), Main.rand.Next(8, 11),
                                    Main.rand.NextFloat(.2f, .5f), Color.Cyan);

                            for (int i = 0; i < 30; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f), Main.rand.Next(15, 22),
                                    Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                                ParticleRegistry.SpawnBloomPixelParticle(Tip, RandomVelocity(1f, 2f, 7f),
                                    Main.rand.Next(40, 60), Main.rand.NextFloat(.1f, .3f), Color.Cyan, Color.White);
                            }

                            SoundID.DD2_LightningAuraZap.Play(Tip, 2.3f, -.1f);
                            Charge = FulminaCharge.Third;
                        }

                        break;
                    case FulminaCharge.Third:
                        if (Time >= (Charge1 + Charge2 + Charge3 + Charge4))
                        {
                            for (int i = 0; i < 6; i++)
                                ParticleRegistry.SpawnLightningArcParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(100f, 100f, .5f, 1f), Main.rand.Next(8, 11),
                                    Main.rand.NextFloat(.3f, .6f), Color.Cyan);

                            for (int i = 0; i < 50; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip,
                                    Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f), Main.rand.Next(20, 25),
                                    Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                            }

                            ParticleRegistry.SpawnBlurParticle(Tip, 30, .4f, 200f);

                            AssetRegistry.GennedSounds.LightningStrike.Play(Tip, 1f, 0f);
                            Charge = FulminaCharge.Fourth;
                        }

                        break;
                    case FulminaCharge.Fourth:

                        if (Time % 2 == 1)
                            ParticleRegistry.SpawnLightningArcParticle(Tip,
                                Main.rand.NextVector2CircularLimited(200f, 200f, .5f, 1f), Main.rand.Next(8, 11),
                                Main.rand.NextFloat(.4f, .8f), Color.Cyan);
                        break;
                }

                if (this.RunLocal() && !Modded.MouseLeft.Current)
                {
                    if (Charge == FulminaCharge.None)
                    {
                        Projectile.Kill();
                        return;
                    }

                    AssetRegistry.GennedSounds.etherealReleaseA.Play(Owner.Center, 1.1f, 0f, .1f, 20);
                    State = FulminaState.Firing;
                    Time = 0f;

                    Projectile.MaxUpdates = 2;
                    Projectile.velocity = Projectile.Center.SafeDirectionTo(Modded.MouseWorld) *
                                          (Charge == FulminaCharge.First || Charge == FulminaCharge.Second ? 22f : 34f);
                    this.Sync();
                }

                break;

            case FulminaState.Firing:
                if (Time < ThrowTime)
                {
                    OwnerDefaults();

                    float throwCompletion = InverseLerp(0f, ThrowTime, Time);
                    float rot = OldArmRot + (Pi * Dir * Owner.gravDir);
                    float anim = MakePoly(6f).OutFunction.Evaluate(OldArmRot, rot, throwCompletion);
                    Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, anim);
                }

                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

                for (int p = 0; p < 2; p++)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float comp = InverseLerp(0f, 3, j);
                            Vector2 dir = -Projectile.velocity.RotatedBy(Lerp(.5f, 0f, comp) * i) *
                                          Lerp(.4f, .8f, comp);
                            float scale = Lerp(1.1f, 2f, comp);
                            ParticleRegistry.SpawnSparkParticle(Tip, dir, 40, scale, Color.DeepSkyBlue);
                        }
                    }
                }

                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, 1f, Projectile.rotation, 0, 90));
                break;
        }

        Time++;

        TotalTime++;
    }

    public void SummonLightning(NPC target)
    {
        Vector2 pos = target.Center - new Vector2(Main.rand.NextFloat(-150f, 150f),
            Main.screenHeight + Main.rand.NextFloat(-180f, 180f));
        Vector2 vel = Vector2.UnitY;
        int type = ModContent.ProjectileType<HonedLightning>();
        HonedLightning lightning = Main
            .projectile[
                Projectile.CreateProj(pos, vel, type, Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f,
                    TotalTime)].As<HonedLightning>();
        lightning.End = target.RandAreaInEntity();
        lightning.Sync();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!this.RunLocal())
            return;

        LightningChain chain = Main
            .projectile[
                Projectile.CreateProj(Tip, Vector2.Zero, ModContent.ProjectileType<LightningChain>(), Projectile.damage,
                    0f, Projectile.owner, 0f, TotalTime)].As<LightningChain>();

        for (int i = 0; i < (Charge == FulminaCharge.First || Charge == FulminaCharge.Second ? 1 : 2); i++)
            SummonLightning(target);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight, 20f);
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;

        if (State == FulminaState.Firing)
            after?.DrawFancyAfterimages(tex, [Color.Cyan]);

        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White, Projectile.rotation, orig,
            Projectile.scale);

        return false;
    }

    public override bool? CanDamage()
    {
        if (State == FulminaState.Firing)
            return Projectile.numHits <= 0 ? null : false;
        return false;
    }
}

internal class HonedLightning : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    private const int Life = 30;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
    }

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Power => ref Projectile.ai[1];
    public float Width => Utils.Remap(Power, 0f, CondereFulminaHoldout.TotalReelTime, 32f, 100f);
    public Vector2 End { get; set; }
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(End);
    public override void ReceiveExtraAI(BinaryReader reader) => End = reader.ReadVector2();
    public float Completion => MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            points = new(100);
            points.SetPoints(GetBoltPoints(Projectile.Center, End, 150f, 4f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity is > 0f and .05f)
            Projectile.Kill();

        Time++;
    }

    public override bool? CanDamage() => Projectile.numHits <= 0;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c) => Width * Projectile.Opacity;

    public Color ColorFunct(SystemVector2 c, Vector2 pos) =>
        MulticolorLerp(Completion, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan) * Projectile.Opacity;

    public TrailPoints points;
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GennedTextures.CausticNoise, 1);
                trail.DrawTrail(shader, points.Points);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}

public class LightningChain : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    private const int Life = 30;

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Power => ref Projectile.ai[1];

    public bool NotPrimary
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public int Current
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = value;
    }

    public int MaxChains => (int) MathF.Ceiling(Utils.Remap(Power, CondereFulminaHoldout.TotalReelTime / 2,
        CondereFulminaHoldout.TotalReelTime, 2, 8));

    public float Width => Utils.Remap(Power, CondereFulminaHoldout.TotalReelTime / 2,
        CondereFulminaHoldout.TotalReelTime, 8f, 50f);

    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Start);
        writer.WriteVector2(End);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Start = reader.ReadVector2();
        End = reader.ReadVector2();
    }

    public float Completion => MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));
    public override bool ShouldUpdatePosition() => false;

    public HashSet<NPC> PreviousNPCs = [null];

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            if (!NotPrimary)
            {
                Start = Projectile.Center;
                NPC close = NPCTargeting.GetClosestNPC(new(Start, 1000, true));
                if (!close.CanHomeInto())
                {
                    Projectile.Kill();
                    return;
                }

                End = close.RandAreaInEntity();
                for (float i = .2f; i < 1f; i += .1f)
                    ParticleRegistry.SpawnGlowParticle(End, Vector2.Zero, 30, Width * i,
                        ColorFunct(SystemVector2.Zero, Vector2.Zero));
            }

            points = new(100);
            points.SetPoints(GetBoltPoints(Start, End, 10f, 10f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity is > 0f and .05f)
            Projectile.Kill();

        Time++;
    }

    public override bool? CanDamage() => Projectile.numHits <= 0;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.friendly = false;
        Time = 1f;
        PreviousNPCs.Add(target);

        if (Current < MaxChains)
        {
            NPC close = NPCTargeting.GetClosestNPC(new(Projectile.Center, 1000, true, false, PreviousNPCs));
            if (close.CanHomeInto())
            {
                Vector2 end = close.RandAreaInEntity();
                LightningChain chain = Main
                    .projectile[
                        Projectile.CreateProj(end, Vector2.Zero, ModContent.ProjectileType<LightningChain>(),
                            Projectile.damage, Projectile.knockBack, Projectile.owner)].As<LightningChain>();
                chain.NotPrimary = true;
                chain.Start = End;
                chain.End = end;
                chain.Current = Current + 1;
                chain.Power = Power;
                chain.PreviousNPCs = new HashSet<NPC>(PreviousNPCs) { close };
                chain.Sync();
                for (float i = .2f; i < 1f; i += .1f)
                    ParticleRegistry.SpawnGlowParticle(end, Vector2.Zero, 30, Width * i,
                        ColorFunct(SystemVector2.Zero, Vector2.Zero));
            }
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Utils.CenteredRectangle(End, Vector2.One * WidthFunct(.5f)).Intersects(targetHitbox);
    }

    public float WidthFunct(float c) => Width * Projectile.Opacity;

    public Color ColorFunct(SystemVector2 c, Vector2 pos) =>
        MulticolorLerp(Completion, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan) * Projectile.Opacity;

    public TrailPoints points;
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GennedTextures.WavyNeurons, 1);
                trail.DrawTrail(shader, points.Points);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}
