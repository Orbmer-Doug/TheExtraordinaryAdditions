using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class HeavenForgedSpear : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.HeavenForgedSpear.Path;

    public Player Owner => Main.player[Projectile.owner];

    private const int Lifetime = 360;

    public override void SetDefaults()
    {
        Projectile.width = 84;
        Projectile.height = 18;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = 2;
        Projectile.MaxUpdates = 2;
        Projectile.timeLeft = Lifetime;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Fade => ref Projectile.ai[1];

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(
                c => Trail.HemisphereWidthFunct(c, MathHelper.SmoothStep(Projectile.height, 0f, c)) *
                     Projectile.Opacity,
                (c, _) => MulticolorLerp(c.X, Color.DarkBlue, new Color(50, 200, 220), new Color(120, 140, 220)) *
                          Projectile.Opacity,
                null,
                40
            );
        cache ??= new(20);
        cache.Update(Projectile.RotHitbox().Right);

        Projectile.FacingRight();

        if (Time % 2f == 0f)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 pos = Projectile.Center +
                              Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f;
                Vector2 vel = -Projectile.velocity.RotatedBy(.45f * i) * Main.rand.NextFloat(.3f, .5f);
                ParticleRegistry.SpawnGlowParticle(pos, vel, Main.rand.Next(12, 20), Main.rand.NextFloat(.2f, .3f),
                    Color.DeepSkyBlue);
            }
        }

        if (Projectile.numHits > 0)
        {
            Projectile.velocity *= .5f;
            Projectile.timeLeft = Lifetime;

            Projectile.Opacity = 1f - InverseLerp(0f, 30f, Fade);
            if (Fade > 30f)
                Projectile.Kill();
            Fade++;
        }
        else
        {
            NPC closest = NPCTargeting.GetClosestNPC(new(Projectile.Center, 850, true, true));
            if (Time > 3 && closest.CanHomeInto())
            {
                Vector2 vel = Projectile.Center.SafeDirectionTo(closest.Center) * 20f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, vel, .1f);
            }
        }

        Projectile.scale = GetLerpBump(0f, 20f, Lifetime, Lifetime - 5f, Time);
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.friendly = false;
        AssetRegistry.GennedSounds.PlasticHit.Play(Projectile.Center, .6f, .66f, .2f, 30);
    }

    public TrailPoints cache;
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader prim = AssetRegistry.GennedShaders.GammaRay;
            prim.TrySetParameter("time", Time * .2f);
            prim.SetTexture(AssetRegistry.GennedTextures.DarkTurbulentNoise, 1, SamplerState.LinearWrap);
            trail.DrawTrail(prim, cache.Points);
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
