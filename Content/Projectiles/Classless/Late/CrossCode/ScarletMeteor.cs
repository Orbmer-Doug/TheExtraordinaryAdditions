using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class ScarletMeteor : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ScarletMeteor);
    public ref float Time => ref Projectile.ai[0];
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, cache.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Projectile.DrawBaseProjectile(Color.White * Projectile.Opacity);
        return false;
    }

    public override void SetDefaults()
    {
        Projectile.width = 36;
        Projectile.height = 38;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 450;
        Projectile.MaxUpdates = 2;
        Projectile.DamageType = DamageClass.Generic;
    }

    public SlotId Whoosh;
    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override void AI()
    {
        if (Time > 20f)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .1f, -20f, 20f);
        Projectile.Opacity = InverseLerp(0f, 20f, Time);

        if (trail == null || trail._disposed)
            trail = new(c => Projectile.width, (c, pos) => Color.OrangeRed * MathHelper.SmoothStep(1f, 0f, c.X) * Projectile.Opacity, null, 10);
        cache ??= new(10);
        cache.Update(Projectile.Center + Projectile.velocity);

        if (SoundEngine.TryGetActiveSound(Whoosh, out var t) && t.IsPlaying)
            t.Position = Projectile.Center;
        else
            Whoosh = AdditionsSound.HeatMeteorFall.Play(Projectile.Center, .5f, 0f, .1f, 20);

        ParticleRegistry.SpawnHeavySmokeParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.2f, .5f),
            Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), Color.OrangeRed.Lerp(Color.Chocolate, Main.rand.NextFloat(.3f, .6f)) * Projectile.Opacity);
        ParticleRegistry.SpawnSparkleParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.7f, 1.4f), Main.rand.Next(15, 25),
            Main.rand.NextFloat(.3f, .4f), Color.OrangeRed * Projectile.Opacity, Color.Chocolate * Projectile.Opacity, Main.rand.NextFloat(.7f, 1.7f), Main.rand.NextFloat(-.2f, .2f));

        Projectile.VelocityBasedRotation();
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        ScreenShakeSystem.New(new(.1f, .1f), Projectile.Center);
        AdditionsSound.HeatMeteorBoom.Play(Projectile.Center, .7f, 0f, .1f, 10);
        if (this.RunLocal())
        {
            float off = RandomRotation();
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * InverseLerp(0f, 4, i) + off).ToRotationVector2();
                Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<ScarletMeteorExplosion>(),
                    Projectile.damage / 4, Projectile.knockBack / 4f, Projectile.owner, 0f, 0f, 0f);
            }
        }
    }
}
