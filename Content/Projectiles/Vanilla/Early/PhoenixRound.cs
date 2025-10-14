using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class PhoenixRound : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 600;
        Projectile.extraUpdates = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override void AI()
    {
        Projectile.FacingRight();

        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 5);

        if (Main.rand.NextBool(7))
            ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(.9f, 1.27f),
                Main.rand.Next(80, 90), Main.rand.NextFloat(.4f, .7f), Color.OrangeRed, Color.Chocolate, 6, true, true);
        if (Main.rand.NextBool(7))
            ParticleRegistry.SpawnMistParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(.3f, .5f),
                Main.rand.NextFloat(.5f, .7f), Color.LightGray, Color.DarkGray, Main.rand.NextFloat(80f, 120f), Main.rand.NextFloat(-.14f, .14f));

        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * Projectile.Opacity);

        cache ??= new(5);
        cache.Update(Projectile.Center);
    }

    private float WidthFunction(float c)
    {
        return OptimizedPrimitiveTrail.PyriformWidthFunct(c, Projectile.width * 2);
    }

    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.OrangeRed * GetLerpBump(0f, .1f, .8f, .27f, c.X) * Projectile.Opacity;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && !trail.Disposed && cache != null)
            {
                ManagedShader shader = ShaderRegistry.FlameTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FireNoise), 1);
                trail.DrawTrail(shader, cache.Points, 30);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        Projectile.Opacity = 0f;

        SoundID.Item14.Play(Projectile.Center);

        for (int i = 0; i < 20; i++)
        {
            Vector2 pos = Projectile.Center;
            Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
            int life = Main.rand.Next(30, 60);
            float scale = Main.rand.NextFloat(.5f, .9f);
            ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life + 70, scale * 2.9f, Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(.4f, .8f)), Color.Orange, 6, true, true);
        }

        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 45, Vector2.Zero, 20, Color.OrangeRed, null, Color.Red, true);
        if (this.RunLocal())
        {
            Projectile.penetrate = -1;
            Projectile.ExpandHitboxBy(45);
            Projectile.Damage();
        }
    }
}
