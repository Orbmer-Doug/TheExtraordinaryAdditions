using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class SanguineSteelsHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.LanceOfSanguineSteels.Path;
    public ref float Time => ref Projectile.ai[0];

    public int Wait
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public ref float Rot => ref Projectile.ai[2];
    public ref float Fade => ref Projectile.AdditionsInfo().ExtraAI[0];

    public int BoltWait
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[1];
        set => Projectile.AdditionsInfo().ExtraAI[1] = value;
    }

    public const int FadeIn = 30;
    public float PortalSize = 150f;
    public float ExtraPortalSize = 60f;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 5;
    }

    public override void Defaults()
    {
        Projectile.Size = new(70f);
        Projectile.friendly = Projectile.ignoreWater = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void SafeAI()
    {
        if (this.RunLocal() && (!Modded.MouseLeft.Current || Fade > 0f))
        {
            Fade++;
            if (Fade > 40f)
                Projectile.Kill();
        }

        Projectile.Opacity = InverseLerp(0f, FadeIn, Time) * InverseLerp(40f, 0f, Fade);
        if (this.RunLocal())
        {
            Projectile.velocity =
                Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Projectile.Center = Center + PolarVector(32f, Projectile.rotation - MathHelper.PiOver4);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetDummyItemTime(2);
        Projectile.timeLeft = 100;
        Projectile.SetAnimation(5, 5);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.ThreeQuarters,
            Projectile.rotation - MathHelper.PiOver4);

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Time > FadeIn && Fade <= 0f)
        {
            if (Wait <= 0f && TryUseMana())
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = pos.SafeDirectionTo((Center + Projectile.velocity * 100f));
                Projectile.NewProj(pos, vel * 10f, ModContent.ProjectileType<SanguineLance>(), Projectile.damage * 4,
                    Projectile.knockBack, Projectile.owner);

                for (int i = 0; i < 20; i++)
                {
                    ParticleRegistry.SpawnHeavySmokeParticle(pos,
                        vel.RotatedByRandom(.2f) * Main.rand.NextFloat(4f, 8f),
                        Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .7f), Color.DarkRed,
                        Main.rand.NextFloat(.2f, .3f));
                }

                SoundID.Item60.Play(pos, .6f, -.1f, 0f, 20, Name);

                Wait = 60;
            }

            if (BoltWait <= 0 && Item.CheckManaBetter(Owner, 2, true))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = Projectile.Center + PolarVector(PortalSize / 4 + ExtraPortalSize,
                        Utils.Remap(i, 0, 3, 0f, MathHelper.TwoPi) + Rot);
                    Vector2 vel = pos.SafeDirectionTo(Modded.MouseWorld) * 12f;
                    Projectile.NewProj(pos, vel, ModContent.ProjectileType<VermillionDart>(),
                        (int) (Projectile.damage / 4f), 0f,
                        Owner.whoAmI);
                }

                BoltWait = 25;
            }
        }

        if (Wait > 0f)
            Wait--;
        if (BoltWait > 0f)
            BoltWait--;

        Time++;
        Rot += .02f;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float rot = Main.GameUpdateCount / 40f;
        ManagedShader effect = AssetRegistry.GennedShaders.MagicRing;
        effect.SetTexture(AssetRegistry.GennedTextures.VoronoiShapes2, 1, SamplerState.LinearWrap);
        effect.TrySetParameter("firstCol", Color.DarkRed.ToVector3());
        effect.TrySetParameter("secondCol", Color.BlueViolet.ToVector3());
        effect.TrySetParameter("time", rot);
        effect.TrySetParameter("cosine", (float) Math.Cos(rot));
        effect.TrySetParameter("opacity", 1.5f);

        Main.spriteBatch.EnterShaderRegion(effect.Effect, BlendState.Additive);
        Texture2D portal = AssetRegistry.GennedTextures.UnfathomablePortal;

        Main.spriteBatch.DrawBetterRect(portal,
            ToTarget(Projectile.Center,
                new Vector2(PortalSize)),
            null, Color.Crimson * Projectile.Opacity, Projectile.rotation, portal.Size() / 2);

        for (int i = 0; i < 3; i++)
        {
            float extra = Utils.Remap(i, 0, 3, 0f, MathHelper.TwoPi);
            Vector2 pos = Projectile.Center + PolarVector(PortalSize / 4 + ExtraPortalSize,
                extra + Rot);

            Main.spriteBatch.DrawBetterRect(portal,
                ToTarget(pos,
                    new Vector2(ExtraPortalSize)),
                null, Color.DarkRed * Projectile.Opacity, -Projectile.rotation - extra, portal.Size() / 2);
        }

        Main.spriteBatch.ResetToDefault();

        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}

public class VermillionDart : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.ignoreWater = Projectile.hostile = Projectile.tileCollide = false;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 3;
        Projectile.timeLeft = 1000;
    }

    public ref float Time => ref Projectile.ai[0];

    public PlayerMouse Modded => Main.player[Projectile.owner].AdditionsMouse();

    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.Zero), 35, Projectile.velocity.ToRotation(),
                new(.3f, 1.1f), 0f, 50f, Color.Crimson);
            for (int i = 0; i < 8; i++)
            {
                ParticleRegistry.SpawnSparkleParticle(Projectile.Center,
                    Projectile.velocity * Main.rand.NextFloat(.1f, .3f),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .8f),
                    Color.Crimson, Color.DarkRed, .7f, Main.rand.NextFloat(-.1f, .1f));
            }
        }

        after ??= new(30, () => Projectile.Center);

        float squish = MathHelper.Clamp(Projectile.velocity.Length() / 5f, 1f, 2f);
        after.UpdateFancyAfterimages(new(Projectile.Center, new Vector2(1f, .4f * squish) * Projectile.Opacity * 90f,
            Projectile.Opacity, Projectile.rotation, 0, 255, 0, 0f, null, true, -.1f));

        if (Main.rand.NextBool(9))
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(),
                -Projectile.velocity * Main.rand.NextFloat(.2f, .5f), Main.rand.Next(20, 30),
                Main.rand.NextFloat(.2f, .5f), Color.Red,
                Color.Crimson, null, 1.2f, 3);

        NPC target = NPCTargeting.GetWeakestNPC(new(Projectile.Center, 700, false, true));
        if (target.CanHomeInto())
        {
            Projectile.velocity += Projectile.Center.SafeDirectionTo(target.Center) * .6f;
            Projectile.velocity -= Projectile.Center.SafeDirectionTo(target.Center) * .2f;
            if (Modded.SafeMouseRight.Current)
                Projectile.velocity += Projectile.Center.SafeDirectionTo(target.Center) * 10f;
            Projectile.velocity = Projectile.velocity.ClampLength(-10f, 10f);
        }

        Projectile.Opacity = InverseLerp(0f, 12f, Time) * InverseLerp(0f, 1f, Projectile.velocity.Length());
        Projectile.rotation = Projectile.velocity.ToRotation();

        if (Time % 10 == 0 && Projectile.damage < Projectile.originalDamage * 3)
            Projectile.damage = (int) (Projectile.damage * 1.03f);
        Time++;
    }

    public override bool? CanDamage() => Modded.SafeMouseRight.Current ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 6; i++)
        {
            ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .7f), Color.DarkRed, Color.Red, .9f);
        }
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D soft = AssetRegistry.GennedTextures.GlowHarsh;
        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, soft, ToTarget(Projectile.Center, Projectile.Size * 3.5f), null,
            Color.Crimson * Projectile.Opacity, 0f, soft.Size() / 2);

        Texture2D tex = AssetRegistry.GennedTextures.LensStar;
        after?.DrawFancyAfterimagesPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, tex,
            [
                Color.Lerp(Color.DarkRed, Color.Crimson,
                    InverseLerp(0f, Projectile.originalDamage * 3, Projectile.damage))
            ], Projectile.Opacity, 1f, 0f,
            true);
        return false;
    }
}


public class SanguineLance : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.SanguineLance.Path;
    private const int StartingLife = 200;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.extraUpdates = 4;
        Projectile.timeLeft = StartingLife;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = -1;
        Projectile.hide = true;
    }

    public enum CurrentState
    {
        Thrown,
        HitEnemy,
        HitGround
    }

    public CurrentState State
    {
        get => (CurrentState) Projectile.ai[0];
        set => Projectile.ai[0] = (float) value;
    }

    public ref float AccumulatedVel => ref Projectile.ai[1];
    public ref float EnemyID => ref Projectile.ai[2];
    public ref float FlailAmt => ref Projectile.localAI[0];
    public ref float Timer => ref Projectile.localAI[1];
    public ref float StickTime => ref Projectile.localAI[2];
    private Vector2 offset;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(FlailAmt);
        writer.Write(Timer);
        writer.WriteVector2(offset);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        FlailAmt = reader.ReadSingle();
        Timer = reader.ReadSingle();
        offset = reader.ReadVector2();
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 50);

        cache ??= new(50);
        cache.Update(Projectile.Center);

        if (State == CurrentState.Thrown)
        {
            const int Slow = 30;
            if (Timer > (StartingLife - Slow))
            {
                AccumulatedVel -= .6f;
                Projectile.Opacity =
                    Projectile.scale = 1f - InverseLerp(StartingLife - Slow, StartingLife, Timer, true);
                Projectile.velocity *= .6f;
                Projectile.extraUpdates = 1;
            }
            else
            {
                Projectile.Opacity = InverseLerp(0f, 30f, Timer);
                AccumulatedVel += Projectile.velocity.Length() * 0.05f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            Timer++;
            return;
        }

        Projectile.velocity *= 0.91f;

        if (State == CurrentState.HitEnemy)
        {
            // Stick to the target
            NPC target = Main.npc[(int) EnemyID];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.velocity *= InverseLerp(20f * Projectile.MaxUpdates, 0f, StickTime);
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
                offset += Projectile.velocity * .85f;
                StickTime++;
            }

            AccumulatedVel -= 0.6f;
        }

        if (State == CurrentState.HitGround)
        {
            FlailAmt = MathHelper.Clamp(FlailAmt - 0.015f, 0f, 1f);
            Projectile.rotation -= MathF.Sin(AccumulatedVel * (MathHelper.TwoPi * 2f)) * 0.2f * FlailAmt *
                                   Projectile.direction;
            AccumulatedVel -= 0.6f;
        }

        Projectile.Opacity = InverseLerp(0f, 14f * Projectile.MaxUpdates, Projectile.timeLeft, true);
    }

    private void SetCollided(bool stick)
    {
        Projectile.extraUpdates = 1;
        State = stick ? CurrentState.HitGround : CurrentState.HitEnemy;
        FlailAmt = 1f;
        Projectile.timeLeft = stick ? 150 : 120;
        if (stick)
        {
            Projectile.tileCollide = false;
            SoundID.Item108.Play(Projectile.Center, .3f, 1f, .2f, 20, Name);
        }

        this.Sync();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (State == 0f)
            SetCollided(true);

        Projectile.velocity *= 0.01f;
        Projectile.Center += oldVelocity * 3f;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = Projectile.BaseRotHitbox().Right;
        for (int i = 0; i < 20; i++)
        {
            if (i < 4)
                ParticleRegistry.SpawnBloodStreakParticle(pos, Projectile.velocity.SafeNormalize(Vector2.Zero),
                    Main.rand.Next(30, 45), Main.rand.NextFloat(.4f, .5f), Color.DarkRed);
            ParticleRegistry.SpawnGlowParticle(pos,
                Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.3f) * Main.rand.NextFloat(4f, 9f),
                Main.rand.Next(30, 50), Main.rand.NextFloat(20f, 30f), Color.DarkRed, .8f);
        }

        // Stick to the target
        if (target.life > 0)
        {
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;

            SetCollided(false);
        }
    }

    public override bool? CanDamage()
    {
        return State == CurrentState.Thrown ? null : false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (State == CurrentState.HitEnemy)
        {
            behindNPCsAndTiles.Add(index);
        }
        else
        {
            Projectile.hide = false;
        }
    }

    public float WidthFunct(float c)
    {
        return Trail.HemisphereWidthFunct(c,
            MathHelper.SmoothStep(Projectile.height * .9f, 0f, c) * Projectile.scale * 1.5f);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color color = Color.Lerp(Color.Crimson, Color.DarkRed, Main.GlobalTimeWrappedHourly + c.X);
        float speed = Utils.GetLerpValue(0f, 60f, AccumulatedVel, true);
        return color * speed * Projectile.Opacity * InverseLerp(20f * Projectile.MaxUpdates, 0f, StickTime);
    }

    public TrailPoints cache;
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            if (AccumulatedVel > 2f)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.FlameTrail;

                shader.SetTexture(AssetRegistry.GennedTextures.DarkRidgeNoise, 1);
                trail.DrawTrail(shader, cache.Points, 200, true);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();

        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 direction = Projectile.rotation.ToRotationVector2() * 10f;

        Vector2 pos = Projectile.Center + direction - Main.screenPosition;
        Vector2 orig = texture.Size() * new Vector2(1f, 0.5f);

        for (int i = 0; i < 8; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / 8 + Main.GlobalTimeWrappedHourly).ToRotationVector2() * 5f;
            Color col = Color.DarkRed with { A = 0 } * 0.95f * Animators.MakePoly(4f).OutFunction(Projectile.Opacity);
            Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, col, Projectile.rotation, orig,
                Projectile.scale, 0, 0f);
        }

        Main.EntitySpriteDraw(texture, pos, null, lightColor * Projectile.Opacity, Projectile.rotation, orig,
            Projectile.scale, 0, 0f);
        return false;
    }
}

