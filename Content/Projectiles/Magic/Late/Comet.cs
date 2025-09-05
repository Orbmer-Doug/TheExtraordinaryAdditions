using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class Comet : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 64;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = 300;
        Projectile.extraUpdates = 1;
        Projectile.scale = 1f;
    }
    public ref float Timer => ref Projectile.Additions().ExtraAI[0];
    public ref float StoredY => ref Projectile.Additions().ExtraAI[1];
    public ref float trailWidth => ref Projectile.Additions().ExtraAI[2];
    public ref float SizeFactor => ref Projectile.ai[0];
    public bool HitGround
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool Init
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public float WidthFunct(float c)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(4f).InFunction(InverseLerp(0.3f, 0f, c)));
        float width = InverseLerp(1f, 0.4f, c) * tipInterpolant * Projectile.scale * SizeFactor * trailWidth;
        return width * Projectile.width * 4.5f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color col = new Color(20 + (Projectile.identity % byte.MaxValue), 50 + (int)(60 * c.X), 255);
        return col * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true) * trailWidth;
    }
    public Color TrailColorFunctionAlt(SystemVector2 c, Vector2 position)
    {
        return ColorFunct(c, position) * .55f;
    }

    public TrailPoints points = new(30);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail._disposed || points == null)
                return;

            ManagedShader w = ShaderRegistry.PierceTrailShader;
            trail.DrawTrail(w, points.Points, 90);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }

    public override void AI()
    {
        if (!Init)
        {
            trailWidth = 1f;
            StoredY = Owner.Additions().mouseWorld.Y;
            SizeFactor = Main.rand.NextFloat(.4f, 1f);
            Projectile.scale = SizeFactor;
            Projectile.netSpam = 0;
            Init = true;
        }

        if (Projectile.position.HasNaNs() || SizeFactor == 0f)
        {
            Projectile.Kill();
            return;
        }
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, 30);

        points.Update(Projectile.Center + Projectile.velocity);
        
        if (Projectile.position.Y > StoredY)
        {
            if (Collision.SolidCollision(Projectile.Center, Projectile.width * (int)SizeFactor, Projectile.height * (int)SizeFactor))
            {
                if (Projectile.timeLeft > 40)
                    Projectile.timeLeft = 40;
                if (!HitGround)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        Vector2 pos = Projectile.Center;
                        Vector2 vel = -Projectile.velocity.RotatedByRandom(Main.rand.NextFloat(.4f, .6f)) * Main.rand.NextFloat(.3f, 1f);

                        ParticleRegistry.SpawnGlowParticle(pos, vel * Main.rand.NextFloat(1.28f, 1.5f), 20, Main.rand.NextFloat(.9f, 1.4f) * SizeFactor, Color.Cyan);
                        ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(120, 150), Main.rand.NextFloat(.7f, 1.2f), Color.White.Lerp(Color.DeepSkyBlue, Main.rand.NextFloat(.1f, .9f)), true, true);
                    }
                    if (this.RunLocal())
                    {
                        Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CometBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner, SizeFactor);
                    }
                    AdditionsSound.etherealSlam.Play(Projectile.Center, .6f, -SizeFactor * .18f, .06f, 50);

                    Projectile.velocity *= 0f;
                    Projectile.extraUpdates = 0;
                    Projectile.netUpdate = true;
                    HitGround = true;
                }
                Projectile.velocity *= .2f;
            }
        }

        if (!HitGround)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 rot = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * i);
                Vector2 vel = rot * 0.33f + Projectile.velocity / 4f;
                Vector2 posit = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f + rot;

                ParticleRegistry.SpawnGlowParticle(posit, vel, Main.rand.Next(27, 30), SizeFactor, ColorFunct(SystemVector2.One * Main.rand.NextFloat(1f), Vector2.Zero));
                ParticleRegistry.SpawnDustParticle(posit, vel * 1.4f, Main.rand.Next(10, 18), SizeFactor * .7f, Color.Cyan);
            }
        }
        else
        {
            trailWidth *= 0.945f;

            if (trailWidth > 0.05f)
                trailWidth -= 0.04f;
            else
                trailWidth = 0;
        }
        Timer++;
    }
    public override bool? CanDamage() => Projectile.velocity != Vector2.Zero;
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Lose 10% of damage per strike
        Projectile.damage = (int)(Projectile.damage * .9f);
        if (SizeFactor > 0f)
            SizeFactor -= .1f;

        if (this.RunLocal() && Projectile.damage > (int)(Projectile.damage * .1f))
        {
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CometBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner, SizeFactor);
        }
    }
}
