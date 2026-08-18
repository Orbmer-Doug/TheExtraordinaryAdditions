using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class VoidBlast : ModProjectile
{
    private const int Lifetime = 35;
    public float Completion => InverseLerp(0f, Lifetime, Time);
    public float Radius => MakePoly(4).OutFunction.Evaluate(0f, 120f, Completion);
    public ref float Time => ref Projectile.ai[0];
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector2 veloc = Main.rand.NextVector2Circular(10f, 10f) + Main.rand.NextVector2Circular(10f, 10f);
                ShaderParticleRegistry.SpawnCosmicParticle(Projectile.Center, veloc / 2,
                    new Vector2(50f + Main.rand.NextFloat(-20f, 20f), 50f + Main.rand.NextFloat(-20f, 20f)));
            }

            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, Lifetime, 1.24f, 250f);
            for (int i = 0; i < 25; i++)
                ParticleRegistry.SpawnMistParticle(Projectile.Center, Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextFloat(.7f, 1.4f), Color.Violet, Color.DarkViolet, Main.rand.NextFloat(150f, 220f),
                    Main.rand.NextFloat(-.1f, .1f));
        }
        
        Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(Radius, Radius);
        int life = Main.rand.Next(30, 40);
        float scale = Main.rand.NextFloat(.4f, .8f);
        Color col = MulticolorLerp(Completion, Color.White.Lerp(Color.Violet, .5f), Color.Violet, Color.DarkViolet,
            Color.Black) * MathHelper.Lerp(2f, .5f, Completion);
        Vector2 vel = Projectile.Center.SafeDirectionTo(pos) * 11f * Completion;

        ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 3, scale * 3f, col, Color.White, 8);

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius / 2f, targetHitbox);
    }
}
