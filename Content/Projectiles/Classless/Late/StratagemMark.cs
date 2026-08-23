using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class StratagemMark : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Eagle500kgBomb.Path;

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Generic;
        Projectile.Size = Vector2.One * 36f;
        Projectile.timeLeft = 200;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
    }

    public bool HitGround
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.ai[1];
    public ref float GroundTime => ref Projectile.ai[2];

    public const int ThrowTime = 40;
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float Completion => InverseLerp(0f, ThrowTime, Time);

    public float ThrowDisplacement()
    {
        return Projectile.velocity.ToRotation() + (MathHelper.PiOver2 * new PiecewiseCurve()
            .Add(0f, -1f, .4f, Sine.OutFunction)
            .Add(-1f, -.1f, 1f, MakePoly(4).InFunction)
            .Evaluate(Completion) * Dir);
    }

    public Player Owner => Main.player[Projectile.owner];
    public static readonly float CallInTime = SecondsToFrames(3.45f);

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => InverseLerp(0.015f, 0.09f, c) * 20f * InverseLerp(0f, 20f, GroundTime),
                (c, pos) => Color.Red * Fade, null, 40);

        if (Time < ThrowTime)
        {
            Projectile.tileCollide = false;
            float rot = ThrowDisplacement();
            Projectile.rotation = rot;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rot - MathHelper.PiOver2);
            Projectile.Center = Owner.GetFrontHandPositionImproved() + PolarVector(Projectile.width / 2, rot);
        }

        if (Time == ThrowTime)
        {
            Projectile.tileCollide = true;
            Projectile.velocity *= 15f;
        }

        if (Time > ThrowTime)
        {
            Projectile.VelocityBasedRotation();
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .1f, -20f, 20f);
        }

        if (HitGround)
        {
            GroundTime++;
            cache.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center - Vector2.UnitY * 1000f, 40));
        }
        else
            Projectile.timeLeft = (int) CallInTime + 5;

        Time++;
    }

    public float Fade => InverseLerp(0f, 15f, Projectile.timeLeft);
    public override bool ShouldUpdatePosition() => Time >= ThrowTime;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -.1f, Volume = 1.1f }, Projectile.Center);
            HitGround = true;
        }

        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X * .3f;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y * .3f;
        Projectile.velocity *= .8f;

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
        {
            Vector2 pos = Projectile.Center - Vector2.UnitY.RotatedByRandom(.18f) * 1000f;
            Vector2 vel = pos.SafeDirectionTo(Projectile.Center) * 10f;
            Projectile.CreateProj(pos, vel, ModContent.ProjectileType<_500kg>(), Projectile.damage, 55f, Projectile.owner);
        }
    }

    public Trail trail;
    public TrailPoints cache = new(40);

    public override bool PreDraw(ref Color lightColor)
    {
        if (GroundTime > 0f)
        {
            void draw()
            {
                if (trail == null || trail.Disposed || cache == null)
                    return;

                ManagedShader shader = AssetRegistry.GennedShaders.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GennedTextures.DendriticNoise, 1);
                trail.DrawTrail(shader, cache.Points, 80);
            }

            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }

        Projectile.DrawBaseProjectile(lightColor * Fade);
        return false;
    }
}

public class _500kg : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures._500kg.Path;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.penetrate = -1;
        Projectile.friendly = true;
        Projectile.hide = true;
        Projectile.DamageType = DamageClass.Generic;
    }

    public bool HitGround
    {
        get => (int) Projectile.ai[0] == 1;
        set => Projectile.ai[0] = value.ToInt();
    }

    public ref float Timer => ref Projectile.ai[1];
    public const int TimeBeforeBoom = 120;
    public TrailPoints cache;
    public Trail trail;

    public override void AI()
    {
        cache ??= new(20);
        cache.Update(Projectile.Center);
        if (trail == null || trail.Disposed)
            trail = new(c => Projectile.height, (c, pos) => Color.WhiteSmoke * MathHelper.SmoothStep(1f, 0f, c.X), null,
                20);

        if (HitGround)
        {
            Timer++;
        }
        else
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -50f, 50f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.timeLeft = TimeBeforeBoom;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            AssetRegistry.GennedSounds.LegStomp.Play(Projectile.Center, 1.5f, -.3f, .1f);
            HitGround = true;
            Projectile.netUpdate = true;
        }

        Projectile.velocity *= .5f;
        if (Projectile.velocity.Length() < 4f)
            Projectile.velocity = Vector2.Zero;

        return false;
    }

    public override bool? CanDamage()
    {
        return Projectile.timeLeft < 2;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        behindNPCsAndTiles.Add(index);
    }

    public const int Size = 1000;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        float fallOff = Utils.Remap(Size - target.Distance(Projectile.Center) * 2, 0f, Size, 0.05f, 1f);
        target.velocity += Projectile.Center.SafeDirectionTo(target.Center) * Projectile.knockBack * fallOff *
                           (target.knockBackResist * .9f);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.Knockback *= 0f;
        float fallOff = Utils.Remap(Size - target.Distance(Projectile.Center) * 2, 0f, Size, 0.05f, 1f);
        modifiers.FinalDamage *= fallOff;
    }

    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
        {
            Projectile.penetrate = -1;
            Projectile.Resize(Size, Size);
            Projectile.Damage();
        }

        Vector2 pos = Projectile.Center;
        for (int i = 0; i < 400; i++)
        {
            Vector2 vel = -Vector2.UnitY.RotatedByRandom(1.8f) * Main.rand.NextFloat(0f, 30f);
            int life = Main.rand.Next(120, 220);
            float scale = Main.rand.NextFloat(1.2f, 2.4f);
            Color color = Color.OrangeRed.Lerp(Color.Chocolate, Main.rand.NextFloat(.3f, .6f));

            ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale * 150f, color);
            ParticleRegistry.SpawnGlowParticle(pos, vel * 1.2f, life + 20, scale * 100f, color,
                Main.rand.NextFloat(.6f, 1.1f), true);

            ParticleRegistry.SpawnCloudParticle(pos, vel, color, Color.Transparent, life, scale,
                Main.rand.NextFloat(.7f, 1.5f));
            ParticleRegistry.SpawnCloudParticle(pos, vel * 1.6f, color, Color.Transparent, life - 10, scale - .1f,
                Main.rand.NextFloat(.7f, 1.2f));

            ParticleRegistry.SpawnSquishyLightParticle(pos, vel * 4f, life / 2, scale, color * 1.4f);
            ParticleRegistry.SpawnSquishyLightParticle(pos, vel * 6f, life / 3, scale * 1.3f, color * 1.8f,
                Main.rand.NextFloat(.5f, 1f), 1.2f);

            ParticleRegistry.SpawnDustParticle(pos, vel * 5f, life / 2, scale * 1.2f, color, .2f, true, true);

            Dust.NewDustPerfect(Projectile.Center, DustID.Stone, vel * 2.1f, 0, default,
                Main.rand.NextFloat(.9f, 1.5f));
            Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, vel * 2.2f, 0, default, Main.rand.NextFloat(.9f, 1.5f));
        }

        ScreenShakeSystem.New(new(3f, 2.3f), Projectile.Center);

        ParticleRegistry.SpawnFlash(Projectile.Center, 22, 1.6f, Size * 1.5f);
        ParticleRegistry.SpawnChromaticAberration(Projectile.Center, 142, .8f, Size * 2f);

        AssetRegistry.GennedSounds.GaussBoom.Play(Projectile.Center, 1.4f, -.2f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        trail?.DrawTrail(AssetRegistry.GennedShaders.StandardPrimitiveShader, cache.Points, 30, false, false);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f,
            Projectile.scale, 0, 0);
        return false;
    }
}

