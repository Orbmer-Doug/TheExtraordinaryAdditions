using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class ShotgunBullet : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ShotgunBullet);
    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 6;
        Projectile.timeLeft = 900;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.aiStyle = 0;
    }

    public float Interpolant => 1f - Utils.GetLerpValue(0f, DropOff, Time, true);
    private float WidthFunction(float x)
    {
        return Projectile.height * MathHelper.SmoothStep(1f, 0f, x) * Interpolant;
    }
    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        Color color = MulticolorLerp(c.X, Color.Yellow, Color.OrangeRed, Color.Chocolate);
        return color * Interpolant;
    }

    private ref float Time => ref Projectile.ai[0];
    private static readonly float DropOff = SecondsToFrames(1.25f);
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 10);
        points.Update(Projectile.RotHitbox().Right);

        if (Time < DropOff)
            Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * .5f * Interpolant);
        if (Time.BetweenNum(DropOff - 30f, DropOff))
            Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.Smoke, Projectile.velocity * Main.rand.NextFloat(.3f, .6f) - Vector2.UnitY * 4f);
        if (Time > DropOff)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .2f, -20f, 18f);

        Projectile.FacingRight();
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = Projectile.RotHitbox().Right;

        for (int i = 0; i < 12; i++)
        {
            Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 9f);
            int life = Main.rand.Next(18, 24);
            float scale = Main.rand.NextFloat(.4f, .6f);

            if (!target.IsFleshy())
            {
                ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, Color.OrangeRed, true, true);
                ParticleRegistry.SpawnGlowParticle(pos, vel * 1.4f, life - 8, scale * .8f, Color.Chocolate * 1.2f, Main.rand.NextFloat(.8f, 1f), true);
            }
            else
            {
                if (Main.rand.NextBool(2))
                    ParticleRegistry.SpawnBloodParticle(pos, vel, life, scale, Color.Red.Lerp(Color.DarkRed, Main.rand.NextFloat(.3f, .6f)));
            }
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        return true;
    }

    public TrailPoints points = new(4);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && !trail.Disposed && Interpolant > 0f)
            {
                ManagedShader shader = ShaderRegistry.FlameTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        void bullet()
        {
            Projectile.DrawBaseProjectile(Lighting.GetColor(Projectile.Center.ToTileCoordinates()));
            Projectile.DrawProjectileBackglow(Color.Chocolate * Interpolant, 3f * Interpolant, 0, 10);
        }
        PixelationSystem.QueueTextureRenderAction(bullet, PixelationLayer.UnderProjectiles);
        return false;
    }
}