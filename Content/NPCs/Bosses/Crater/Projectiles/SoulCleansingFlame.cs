using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class SoulCleansingFlame : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public static readonly int FormationTime = 50;
    public override void SetDefaults()
    {
        Projectile.Size = new(200f);
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 200;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => 200f * Projectile.scale, (c, pos) => Color.White, null, 25);
        Projectile.scale = Animators.MakePoly(2.4f).OutFunction(InverseLerp(0f, FormationTime, Time));
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity *= .989f;
        Projectile.velocity = Projectile.velocity.ClampLength(6f, 60f);

        points.Update(Projectile.Center);
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 30; i++)
        {
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center, Main.rand.NextVector2Circular(30f, 30f), Main.rand.Next(30, 40),
                Main.rand.NextFloat(.5f, .9f), Color.Gold, Color.Goldenrod, null, 1.2f, 5, true);
            ParticleRegistry.SpawnBloomLineParticle(Projectile.Center, Main.rand.NextVector2Circular(10f, 10f) + Main.rand.NextVector2Circular(10f, 10f),
                Main.rand.Next(22, 30), Main.rand.NextFloat(1.4f, 1.8f), Color.Goldenrod);

            if (i < 5)
            {
                float size = Utils.Remap(i, 0, 5, 150f, 250f);
                float opacity = Utils.Remap(i, 0, 5, 1f, .4f);
                int life = (int)Utils.Remap(i, 0, 5, 20, 50);
                Color col = MulticolorLerp(InverseLerp(0, 5, i), Color.LightGoldenrodYellow, Color.Gold, Color.DarkGoldenrod);
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * size, Vector2.Zero, life, col * opacity, RandomRotation());
            }
        }
        for (int i = 0; i < 4; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * InverseLerp(0, 4, i)).ToRotationVector2();
            if (this.RunServer())
                SpawnProjectile(Projectile.Center, vel, ModContent.ProjectileType<BurstingLight>(), Asterlin.LightAttackDamage, 0f);
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(15);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;
            ManagedShader shader = AssetRegistry.GetShader("SoulCleansingFlameShader");
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 1.2f);
            shader.TrySetParameter("opacity", Projectile.scale);
            trail.DrawTrail(shader, points.Points, 200, true, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles, null);
        return false;
    }
}