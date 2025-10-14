using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class CometBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.width = 100;
        Projectile.height = 100;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.95);
    }

    public ref float SizeFactor => ref Projectile.ai[0];

    private List<Vector2> cache;
    private TrailPoints points = new(40);
    private const int Lifetime = 24;
    public float Completion => 1f - Utils.GetLerpValue(0f, Lifetime, Projectile.timeLeft);
    public float Radius => MakePoly(4).OutFunction.Evaluate(0f, 80f * SizeFactor, Completion);
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 40);

        Lighting.AddLight(Projectile.Center, Color.CornflowerBlue.ToVector3() * 4f);
        if (Projectile.ai[2] == 0f)
        {
            int count = 26;
            for (int i = 0; i < count; i++)
            {
                float offset = Main.rand.NextFloat(MathHelper.TwoPi);

                Vector2 pos = Projectile.Center;
                Vector2 vel = (MathHelper.TwoPi * i / count + offset).ToRotationVector2() * (14f + SizeFactor * 2);
                Color color = Color.Lerp(Color.BlueViolet * 1.8f, Color.DeepSkyBlue, Main.rand.NextFloat(.4f, .89f));
                int lifetime = Main.rand.Next(20, 40);
                float scale = .4f + SizeFactor;
                float rand = Main.rand.NextFloat(.4f, .9f);

                for (int j = 0; j <= 1; j++)
                {
                    ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * rand, lifetime, scale, Main.rand.NextBool() ? new Color(35, 119, 213) : new Color(30, 64, 128), .6f, true);
                }
            }

            Projectile.ai[2] = 1f;
        }

        ManageCaches();

        for (int k = 0; k < 2; k++)
        {
            float rot = RandomRotation();

            Vector2 pos = Projectile.Center + Vector2.One.RotatedBy(rot) * (Radius + 35);
            Vector2 vel = Vector2.One.RotatedBy(rot + Main.rand.NextFloat(1.1f, 1.3f)) * 2 * Main.rand.NextFromList(-1, 1);
            Color color = Color.Cyan;
            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, 20, Main.rand.NextFloat(.2f, .4f), color, .4f, 3f, 6f);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, (int)Radius + 75, targetHitbox);
    }

    private void ManageCaches()
    {
        if (cache is null)
        {
            cache = [];

            for (int i = 0; i < 40; i++)
            {
                cache.Add(Projectile.Center);
            }
        }

        for (int k = 0; k < 40; k++)
        {
            cache[k] = Projectile.Center + Vector2.One.RotatedBy(k / 19f * MathHelper.TwoPi) * (Radius + 20);
        }

        while (cache.Count > 40)
        {
            cache.RemoveAt(0);
        }
        points.SetPoints(cache);
    }

    private float WidthFunct(float c) => 18f * GetLerpBump(0f, .2f, 1f, .7f, Completion);
    private Color ColorFunct(SystemVector2 c, Vector2 position) => Color.Lerp(Color.White, Color.Cyan * 1.4f, Completion) * GetLerpBump(0f, .2f, 1f, .7f, Completion);

    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;

            ManagedShader shader = ShaderRegistry.EnlightenedBeam;

            shader.TrySetParameter("time", Projectile.timeLeft * 0.01f);
            shader.TrySetParameter("repeats", 6f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 2, SamplerState.LinearWrap);

            trail.DrawTrail(shader, points.Points, 300, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}