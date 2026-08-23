using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using Utils = Terraria.Utils;


namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class FinalStrikeHoldout : ModProjectile
{
    public enum FinalStrikeState
    {
        Aim,
        Fire,
        Wait,
        DivinePierce,
        Stab
    }

    public Player Owner => Main.player[Projectile.owner];
    public PlayerMouse Modded => Owner.AdditionsMouse();
    public Vector2 TipOfSpear => Projectile.RotHitbox().TopRight;

    public FinalStrikeState CurrentState
    {
        get => (FinalStrikeState) Projectile.ai[0];
        set => Projectile.ai[0] = (int) value;
    }

    public int StateTime
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public float Counter
    {
        get => (int) Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public bool Init
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[0] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float VanishTime => ref Projectile.AdditionsInfo().ExtraAI[2];

    public bool Vanish
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[3] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[3] = value.ToInt();
    }

    public ref float DivineFormInterpolant => ref Projectile.localAI[0];
    public ref float OldArmRot => ref Projectile.localAI[1];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override string Texture => AssetRegistry.GennedTextures.FinalStrike.Path;

    public override void SetDefaults()
    {
        Projectile.width = 138;
        Projectile.height = 140;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = 14400;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Melee;
    }

    public override void AI()
    {
        switch (CurrentState)
        {
            case FinalStrikeState.Aim:
                DoBehavior_Aim();
                break;
            case FinalStrikeState.Fire:
                DoBehavior_Fire();
                break;
            case FinalStrikeState.Wait:
                DoBehavior_Wait();
                break;
            case FinalStrikeState.DivinePierce:
                DoBehavior_Pierce();
                break;
            case FinalStrikeState.Stab:
                DoBehavior_Stab();
                break;
        }

        if (CurrentState != FinalStrikeState.Aim)
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

        StateTime++;
        Time++;
    }

    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter);
    private static readonly int shootDelay = SecondsToFrames(2.4f);

    public void DoBehavior_Aim()
    {
        float animationCompletion = InverseLerp(0f, shootDelay, StateTime);
        DivineFormInterpolant = MakePoly(3).InFunction(animationCompletion);
        Projectile.Opacity = MakePoly(3f).InFunction(InverseLerp(0f, 12f, Time));

        int frequency = 5;
        if (animationCompletion is >= .33f and <= .66f)
            frequency = 3;
        if (animationCompletion is >= .66f and <= 1f)
            frequency = 1;

        if (StateTime % frequency == frequency - 1)
        {
            Vector2 pos = TipOfSpear + Main.rand.NextVector2Circular(150f, 150f);
            int life = Main.rand.Next(90, 120);
            float size = Main.rand.NextFloat(.3f, .6f);
            ParticleRegistry.SpawnBloomPixelParticle(pos, Vector2.Zero, life, size, Color.Wheat, Color.AntiqueWhite,
                TipOfSpear, 1f, 7);
        }

        if (StateTime == shootDelay)
        {
            for (int i = 0; i < 40; i++)
                ParticleRegistry.SpawnSquishyPixelParticle(TipOfSpear,
                    Main.rand.NextVector2CircularLimited(10f, 10f, .5f, 1f), Main.rand.Next(90, 150),
                    Main.rand.NextFloat(.9f, 1.6f), Color.AntiqueWhite, Color.Wheat, 9, false, false,
                    Main.rand.NextFloat(-.1f, .1f));
            AssetRegistry.GennedSounds.spearCharge.Play(Owner.Center, 1f, 0f, .1f, 1, Name);
        }

        if (StateTime >= shootDelay)
        {
            float speed = -Main.rand.NextFloat(5f, 12f);
            float scale = Main.rand.NextFloat(.7f, 1.2f);
            Vector2 sparkVelocity = Projectile.velocity.RotatedByRandom(.35f) * speed;
            ParticleRegistry.SpawnSparkParticle(TipOfSpear, sparkVelocity, Main.rand.Next(40, 50), scale, Color.Wheat);
        }

        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * animationCompletion * 1.4f);

        // Aim the spear
        if (this.RunLocal())
        {
            float aimInterpolant = Utils.GetLerpValue(5f, 25f, Center.Distance(Modded.MouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld),
                aimInterpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        // Stick to the player
        Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        float frontArmRotation = Projectile.rotation - PiOver4 - animationCompletion * Owner.direction * 0.74f;
        if (Owner.direction == 1)
            frontArmRotation += Pi;

        Projectile.Center = Center + (frontArmRotation + PiOver2).ToRotationVector2() * Projectile.scale * 27f +
                            Projectile.velocity * Projectile.scale;

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Projectile.spriteDirection = Owner.direction;

        Item heldItem = Owner.HeldItem;
        if (this.RunLocal() && !Owner.Available() || heldItem is null)
        {
            Projectile.Kill();
            return;
        }

        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        OldArmRot = frontArmRotation;

        if (!this.RunLocal() || Owner.channel)
            return;

        if (StateTime >= shootDelay)
        {
            AssetRegistry.GennedSounds.pierce.Play(Projectile.Center, 1f, 0f, .2f);

            StateTime = 0;
            CurrentState = FinalStrikeState.Fire;
            Projectile.netUpdate = true;

            Projectile.velocity *= heldItem.shootSpeed;
            return;
        }

        Projectile.Kill();
    }

    public void DoBehavior_Fire()
    {
        if (Projectile.timeLeft > 360)
            Projectile.timeLeft = 360;
        Projectile.extraUpdates = 3;
        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * 1.4f);

        float throwCompletion = InverseLerp(0f, 25f * Projectile.extraUpdates, StateTime);
        float rot = OldArmRot + Pi * Dir;
        float anim = MakePoly(6).OutFunction.Evaluate(OldArmRot, rot, throwCompletion);
        Owner.SetCompositeArmFront(throwCompletion < 1f, Player.CompositeArmStretchAmount.Full, anim);
        if (throwCompletion < 1f)
            Owner.ChangeDir(Dir);

        if (StateTime % 5f == 0f)
        {
            IEntitySource source = Projectile.GetSource_FromThis();
            int damage = (int) (Projectile.damage * .5f);
            float off = ToRadians(10f);

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = TipOfSpear;
                const float scale = 1.8f;
                Color col1 = Color.AntiqueWhite;

                Vector2 perturbedSpeed = new Vector2((0f - Projectile.velocity.X) / 3f,
                    (0f - Projectile.velocity.Y) / 3f).RotatedBy(Lerp(0f - off, off, i / (2f - 1)));
                for (int j = 0; j < 2; j++)
                {
                    if (this.RunLocal())
                        Projectile.NewProjectile(source, Projectile.Center, perturbedSpeed * 1.2f,
                            ModContent.ProjectileType<Streaks>(), damage, 0f, Projectile.owner);

                    for (int p = 0; p < 2; p++)
                    {
                        ParticleRegistry.SpawnSparkParticle(pos, perturbedSpeed * 2, 180, scale, col1);
                    }

                    perturbedSpeed *= 1.55f;
                }
            }
        }

        Projectile.spriteDirection = 1;
    }

    public const float WaitTime = 180f;
    public float WaitCompletion => InverseLerp(0f, WaitTime, StateTime);

    public void DoBehavior_Wait()
    {
        float ratio = MakePoly(2).InFunction(InverseLerp(0f, WaitTime / 2, StateTime));
        Cache ??= new(40);

        Vector2 dir = Projectile.Center.SafeDirectionTo(Modded.MouseWorld);
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, dir, ratio);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Cache.SetPoints(Projectile.RotHitbox().BottomLeft.GetLaserControlPoints(
            Projectile.RotHitbox().BottomLeft +
            Projectile.velocity.SafeNormalize(Vector2.Zero) * WaitCompletion * 2500f, 40));

        Projectile.timeLeft = 300;
        Projectile.extraUpdates = 3;

        if (StateTime > WaitTime)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 20, 0f, Vector2.One, 0f, .4f,
                Color.AntiqueWhite, true);
            Projectile.velocity *= 16f;
            Projectile.MaxUpdates = 8;
            AssetRegistry.GennedSounds.IkeFinal.Play(Projectile.Center, 1f, -.2f, .1f);
            AssetRegistry.GennedSounds.pierce.Play(Projectile.Center, 1.5f, -.5f, .1f);
            CurrentState = FinalStrikeState.DivinePierce;
            this.Sync();
        }
    }

    public void DoBehavior_Pierce()
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(12f, 19f);
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.5f, .9f);
            Color col = Color.Wheat.Lerp(Color.AntiqueWhite, Main.rand.NextFloat(.2f, 5f));
            ParticleRegistry.SpawnSparkParticle(TipOfSpear + Main.rand.NextVector2Circular(10f, 10f), vel, life, scale,
                col);
            ParticleRegistry.SpawnSparkleParticle(TipOfSpear, vel, life, scale, col, Color.Wheat, 1.2f,
                Main.rand.NextFloat(-.2f, .2f));
        }

        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * 2.3f);
    }

    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(Offset);
    public override void ReceiveExtraAI(BinaryReader reader) => Offset = reader.ReadVector2();

    public float Completion => InverseLerp(0f, 14f, StateTime);
    public float Bump => GetLerpBump(.1f, .55f, 1f, .85f, Completion);

    public void DoBehavior_Stab()
    {
        if (!Init)
        {
            Projectile.ResetLocalNPCHitImmunity();
            Projectile.numHits = 0;
            StateTime = 0;
            if (this.RunLocal())
                Projectile.velocity = Center.SafeDirectionTo(Modded.MouseWorld).RotatedByRandom(.09f);
            Init = true;
            this.Sync();
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = Projectile.timeLeft = 2;
        Owner.ChangeDir(Dir);

        if (Vanish)
        {
            Projectile.Opacity = MakePoly(3).OutFunction(1f - InverseLerp(0f, 15f, VanishTime));
            if (VanishTime > 15f)
                Projectile.Kill();

            VanishTime++;
        }

        if (Completion >= 1f)
        {
            if (Modded.SafeMouseRight.Current && VanishTime <= 0f)
            {
                Init = false;
            }
            else
            {
                Vanish = true;
            }
        }

        if (StateTime == 0f)
        {
            AssetRegistry.GennedSounds.etherealSwordAttackBasic3.Play(TipOfSpear, Main.rand.NextFloat(.8f, 1f), 0f, .2f,
                0, Name);
        }

        float pierce = new PiecewiseCurve()
            .Add(-20f, 80f, .6f, MakePoly(7).OutFunction)
            .Add(70f, -20f, 1f, MakePoly(3).OutFunction)
            .Evaluate(Completion);
        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * Bump * 1.4f);

        Projectile.Center = Center + Projectile.velocity * pierce;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        float length = (Projectile.height + 10f) * Projectile.scale;
        float width = 12f * Projectile.scale;
        Vector2 start = Projectile.Center;
        Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.Zero) * length;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end,
            width, ref collisionPoint);
    }

    public float WidthFunct(float c)
    {
        return 11f * WaitCompletion;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return Color.White * SmoothStep(1f, 0f, c.X) * InverseLerp(WaitTime, WaitTime - 24f, StateTime);
    }

    public TrailPoints Cache;

    public void DrawTele()
    {
        if (CurrentState != FinalStrikeState.Wait)
            return;

        void Draw()
        {
            ManagedShader shader = AssetRegistry.GennedShaders.SideStreakTrail;
            shader.SetTexture(AssetRegistry.GennedTextures.WavyBlotchNoise, 1);
            Trail line = new(WidthFunct, ColorFunct, null, 40);
            line.DrawTrail(shader, Cache.Points, 50);
        }

        PixelationSystem.QueuePrimitiveRenderAction(Draw, PixelationLayer.UnderProjectiles);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (CurrentState == FinalStrikeState.Stab)
        {
            for (int i = 0; i < 20; i++)
            {
                Vector2 pos = TipOfSpear + Main.rand.NextVector2Circular(5f, 5f);
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                ParticleRegistry.SpawnBloomPixelParticle(pos,
                    vel, Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, .7f),
                    Color.AntiqueWhite, Color.White, null, 1.2f, 7);
            }

            if (this.RunLocal())
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 end = TipOfSpear + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.4f) *
                        Main.rand.NextFloat(300f, 600f);
                    Projectile.CreateProj(TipOfSpear, Vector2.Zero, ModContent.ProjectileType<DivineLightning>(),
                        (int) (Projectile.damage * 4.25f), 0f, Owner.whoAmI, ai1: end.X, ai2: end.Y);
                }
            }

            ParticleRegistry.SpawnFlash(TipOfSpear, 30, .3f, 300f);
            ParticleRegistry.SpawnBlurParticle(TipOfSpear, 30, .2f, 200f);

            ScreenShakeSystem.New(new ScreenShake(.2f, .1f), TipOfSpear);

            AssetRegistry.GennedSounds.etherealSharpImpact.Play(TipOfSpear, 1.3f, -.1f, .3f, 12);
        }
        else
        {
            AssetRegistry.GennedSounds.MediumExplosion.Play(TipOfSpear, 1.2f, 0f, .2f);
            Projectile.damage = (int) MathF.Max(500f, Projectile.damage * 0.91f);
            Vector2 pos = CheckLinearCollision(Projectile.RotHitbox().TopRight, Projectile.RotHitbox().BottomLeft,
                target.Hitbox,
                out Vector2 start, out _)
                ? start
                : TipOfSpear;

            ScreenShakeSystem.New(
                new(CurrentState == FinalStrikeState.Fire ? 1f : .2f,
                    CurrentState == FinalStrikeState.Fire ? .5f : .1f), pos);

            Vector2 splatterDirection = CurrentState == FinalStrikeState.Stab
                ? Projectile.velocity * Main.rand.NextFloat(6f, 14f)
                : Projectile.velocity / 2;
            for (int i = 0; i < 10; i++)
            {
                int life = Main.rand.Next(55, 70);
                float scale = Main.rand.NextFloat(1.7f, Main.rand.NextFloat(3.3f, 5.5f)) * 0.85f;
                Color col = Color.Lerp(Color.Beige, Color.Wheat * 1.2f, Main.rand.NextFloat(0.7f));
                col = Color.Lerp(col, Color.AntiqueWhite, Main.rand.NextFloat());
                Vector2 vel = splatterDirection.RotatedByRandom(0.599) * Main.rand.NextFloat(.5f, 1.2f);

                ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, col);
                ParticleRegistry.SpawnSparkParticle(pos, vel * 1.5f, 80, scale * .7f, Color.AntiqueWhite);
            }
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (CurrentState != FinalStrikeState.Stab)
            return;
        modifiers.ScalingArmorPenetration += 1;
    }

    public override bool? CanDamage()
    {
        if (CurrentState == FinalStrikeState.Stab)
            return Completion is > 0f and < .7f ? null : false;
        return CurrentState is FinalStrikeState.Aim or FinalStrikeState.Wait ? false : null;
    }

    public void DrawBackglow()
    {
        SpriteBatch sb = Main.spriteBatch;

        float backglowWidth = DivineFormInterpolant * 2f;
        if (backglowWidth <= 0.5f)
            backglowWidth = 0f;

        Color backglowColor = Color.AntiqueWhite;
        backglowColor = Color.Lerp(backglowColor, Color.NavajoWhite,
            Utils.GetLerpValue(0.7f, 1f, DivineFormInterpolant, true) * 0.56f) * 0.4f;
        backglowColor.A = (byte) (20 * Projectile.Opacity);

        Texture2D glowmaskTexture = Projectile.ThisProjectileTexture();
        Rectangle frame = glowmaskTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;
        for (int i = 0; i < 10; i++)
        {
            Vector2 drawOffset = (TwoPi * i / 10f).ToRotationVector2() * backglowWidth;
            sb.Draw(glowmaskTexture, drawPosition + drawOffset, frame, backglowColor * Projectile.Opacity,
                Projectile.rotation, origin, Projectile.scale, 0, 0f);
        }

        if (CurrentState == FinalStrikeState.Stab)
            return;

        Vector2 offsets = new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition;
        float auraRotation = Projectile.velocity.ToRotation() + PiOver4;
        Vector2 drawStartOuter = offsets + Projectile.Center + Projectile.velocity;
        Vector2 spinPoint = -Vector2.UnitY * 6f * DivineFormInterpolant;
        float time = Main.GlobalTimeWrappedHourly;
        float rotation = TwoPi * time / 3f;
        float opacity = .85f * DivineFormInterpolant;
        for (int i = 0; i < 6; i++)
        {
            Vector2 spinStart = drawStartOuter + spinPoint.RotatedBy(rotation - (float) Math.PI * i / 3f);
            Color glowAlpha = Projectile.GetAlpha(backglowColor * Projectile.Opacity);
            glowAlpha.A = (byte) Projectile.alpha;
            sb.Draw(glowmaskTexture, spinStart, frame, glowAlpha * opacity, auraRotation, origin, Projectile.scale,
                0, 0f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D spearTexture = Projectile.ThisProjectileTexture();
        Rectangle frame = spearTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;

        DrawTele();
        Main.spriteBatch.Draw(spearTexture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation,
            origin, Projectile.scale, 0, 0f);
        if (CurrentState != FinalStrikeState.Stab)
            DrawBackglow();

        for (float i = 1f; i < 1.5f; i += .1f)
        {
            Texture2D flare = AssetRegistry.GennedTextures.LensStar;
            Vector2 size = new(30f * i * DivineFormInterpolant);
            size.Y += MathF.Sin(StateTime * .04f) * 10f * i;
            if (CurrentState == FinalStrikeState.Stab)
                size = new(60f * i * Bump);
            Rectangle target = ToTarget(Projectile.RotHitbox(Vector2.One / 2f).TopRight, size);
            Vector2 orig = flare.Size() / 2f;
            float rot = Projectile.rotation - PiOver4;
            SpriteBatch.DrawRectPixelated(PixelationLayer.OverProjectiles, BlendState.Additive, flare, target, null,
                Color.AntiqueWhite, rot, orig);
        }
        
        return false;
    }
}

public class Streaks : ModProjectile
{
    public const int Life = 75;
    public ref float Time => ref Projectile.ai[0];
    public override string Texture => AssetRegistry.GennedTextures.SeamStrike.Path;

    public override void SetDefaults()
    {
        Projectile.width = 600;
        Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = Life;
        Projectile.MaxUpdates = 4;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.noEnchantmentVisuals = true;
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Projectile.RotHitbox().Intersects(targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D bloomTexture = AssetRegistry.GennedTextures.GlowParticleSmall;
        float ratio = InverseLerp(0f, Life, Time);
        float completion = MakePoly(2).OutFunction(ratio);
        float opacity = 1f - MakePoly(2.5f).InFunction(ratio);
        Color color = MulticolorLerp(InverseLerp(0f, 10f, Projectile.identity / 10f % 1),
            Color.LightSteelBlue, Color.White, Color.WhiteSmoke, Color.FloralWhite, Color.LightSkyBlue) * opacity;

        float x = Projectile.width * completion;
        float y = Projectile.height * opacity;
        Vector2 scale = new(x, y);
        Vector2 bloomOrigin = bloomTexture.Size() / 2;

        for (float i = .1f; i <= 2f; i += .1f)
        {
            SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, bloomTexture,
                ToTarget(Projectile.Center, scale * i), null, color * (2f - i),
                Projectile.rotation, bloomOrigin);
        }

        return false;
    }
}

public class DivineLightning : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public const int Life = 30;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Life;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public ref float Time => ref Projectile.ai[0];

    public Vector2 End
    {
        get => new(Projectile.ai[1], Projectile.ai[2]);
        set
        {
            Projectile.ai[1] = value.X;
            Projectile.ai[2] = value.Y;
        }
    }

    public float Completion => MakePoly(6).OutFunction(InverseLerp(0f, Life, Time));

    private List<Line>[] Branches = [];
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Time == 0f)
            Branches = CreateLightningBranch(Projectile.Center, End, 0, 2f, 0f,
                Main.rand.NextFloat(40f, 80f)).ToArray();

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity is > 0f and .05f)
            Projectile.Kill();

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int) (Projectile.damage * .75f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (List<Line> list in Branches)
        {
            foreach (Line line in list)
            {
                const int width = 8;
                if (new Rectangle((int) line.A.X - width / 2, (int) line.A.Y - width / 2, width, width).Intersects(
                        targetHitbox))
                    return true;
            }
        }

        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Branches == null || Branches.Length == 0)
            return false;

        foreach (List<Line> list in Branches)
        {
            foreach (Line line in list)
            {
                line.DrawPixelated(PixelationLayer.OverNPCs, BlendState.Additive,
                    MulticolorLerp(Completion, Color.White, Color.AntiqueWhite, Color.WhiteSmoke)
                    * Projectile.Opacity);
            }
        }

        return false;
    }
}

public sealed class FinalStrikePlayer : ModPlayer
{
    public int Counter;
    public override void UpdateDead() => Counter = 0;
}
