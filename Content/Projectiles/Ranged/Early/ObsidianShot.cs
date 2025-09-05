using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class ObsidianShot : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ObsidianShot);
    public ref float Time => ref Projectile.ai[0];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.timeLeft = 180;
        Projectile.penetrate = 2;
        Projectile.MaxUpdates = 2;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.scale = Projectile.Opacity = 1f;
    }

    public override void AI()
    {
        int count = 2 * Projectile.MaxUpdates;
        cache ??= new(count);
        cache.Update(Projectile.Center);

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

        Projectile.velocity *= .988f;

        if (Projectile.velocity.Length() < .1f)
            Projectile.Kill();
        Projectile.Opacity = InverseLerp(.1f, 3f, Projectile.velocity.Length());
        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ArmorPenetration += 5;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f;

        for (int i = 0; i < 4; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.24f) * Main.rand.NextFloat(.5f, .8f);
            int life = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.3f, .5f);
            Color sparkCol = Color.Lerp(Color.White, Color.Violet, Main.rand.NextFloat(.2f, .7f)) * .7f;
            Color bloodCol = Color.Lerp(Color.DarkRed, Color.Red, Main.rand.NextFloat()) * .9f;

            if (target.IsFleshy())
            {
                ParticleRegistry.SpawnBloodParticle(pos, vel, life, scale + .1f, bloodCol);
            }
            else
            {
                ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, sparkCol, true);
            }
        }

        Projectile.velocity *= .7f;
        Projectile.damage = Projectile.damage / 2;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        SoundEngine.PlaySound(SoundID.Dig, Projectile.Center, null);
        Collision.HitTiles(Projectile.Center, Projectile.velocity, Projectile.width, Projectile.height);
        return true;
    }

    public TrailPoints cache;
    private float WidthFunct(float c) => Projectile.height / 2 * MathHelper.SmoothStep(1f, 0f, c);
    private Color ColorFunct(SystemVector2 c, Vector2 position) => MulticolorLerp(MathHelper.SmoothStep(1f, 0f, c.X), Color.Violet, Color.BlueViolet, Color.DarkViolet) * Projectile.Opacity;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.FadedStreak;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 1);
            OptimizedPrimitiveTrail trail = new(WidthFunct, ColorFunct, null, 2 * Projectile.MaxUpdates);
            trail.DrawTrail(shader, cache.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}