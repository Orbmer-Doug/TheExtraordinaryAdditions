using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late.Zenith;

public class ExtraordinaryHyperBlast : ModProjectile, ILocalizedModType, IModType
{
    private const int Lifetime = 35;
    public float Completion => 1f - Utils.GetLerpValue(0f, Lifetime, Projectile.timeLeft);
    public float Radius => Circ.OutFunction.Evaluate(0f, 120f * Projectile.scale, Completion);
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, amt);

        if (Projectile.ai[2] == 0f)
        {
            AdditionsSound.etherealMagicBlast.Play(Projectile.Center, 2.5f, 0f, .1f, 1, Name);
            for (int i = 0; i < 130; i++)
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = Main.rand.NextVector2CircularLimited(14f, 14f, .75f, 1f) + RandomVelocity(1f, 2f, 4f);
                float size = Main.rand.NextFloat(.8f, 1.1f);
                int life = Main.rand.Next(20, 35);
                Color col = Color.Lerp(Color.Gold, Color.DarkGoldenrod, Main.rand.NextFloat());
                ParticleRegistry.SpawnGlowParticle(pos, vel, life, size, col);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, size, col * 1.4f, .9f, true);

                if (i % 2 == 1)
                    ParticleRegistry.SpawnBloomLineParticle(pos, vel * Main.rand.NextFloat(1.4f, 2.2f), life - 10, size + Main.rand.NextFloat(.1f, .3f), col);
            }

            ScreenShakeSystem.New(new(.9f, .8f, 3000f), Projectile.Center);
            Projectile.ai[2] = 1f;
        }

        ManageCaches();

        for (int k = 0; k < 2; k++)
        {
            float rot = RandomRotation();

            Vector2 pos = Projectile.Center + Vector2.One.RotatedBy(rot) * (Radius + 35);
            Vector2 vel = Vector2.One.RotatedBy(rot + Main.rand.NextFloat(1.1f, 1.3f)) * 2 * Main.rand.NextFromList(-1, 1);
            Color color = Color.Violet;
            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, 20, Main.rand.NextFloat(.2f, .4f), color, .7f);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, (int)Radius + 75, targetHitbox);
    }

    private List<Vector2> cache;
    public const int amt = 70;
    private readonly ManualTrailPoints points = new(amt);
    private void ManageCaches()
    {
        if (cache is null)
        {
            cache = [];

            for (int i = 0; i < amt; i++)
            {
                cache.Add(Projectile.Center);
            }
        }

        for (int k = 0; k < amt; k++)
        {
            cache[k] = Projectile.Center + Vector2.One.RotatedBy(k / (float)(amt - 1) * (MathF.Tau + float.Epsilon)) * (Radius + 20);
        }

        while (cache.Count > amt)
        {
            cache.RemoveAt(0);
        }
        points.SetPoints(cache);
    }

    private float WidthFunct(float c) => 28f * Projectile.scale * (1f - Completion);
    private Color ColorFunct(SystemVector2 c, Vector2 position) => Color.Lerp(Color.White, Color.Gold, Completion) * GetLerpBump(0f, .1f, 1f, .8f, Completion);

    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader beam = ShaderRegistry.EnlightenedBeam;
            beam.TrySetParameter("time", Projectile.timeLeft * 0.01f);
            beam.TrySetParameter("repeats", 6f);
            beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1, SamplerState.LinearWrap);
            beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 2, SamplerState.LinearWrap);
            trail.DrawTrail(beam, points.Points, 500);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        return false;
    }
}