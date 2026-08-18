using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.SolarGuardian;

public class Sunray : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
    }

    public const int Lifetime = 220;

    public override void SetDefaults()
    {
        Projectile.Size = new(52);
        Projectile.penetrate = 3;
        Projectile.ignoreWater = Projectile.hostile = Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.tileCollide = Projectile.friendly = false;
        Projectile.MaxUpdates = 3;
        Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
    }

    public int Time
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 20);
        points.Update(Projectile.Center);

        if (Projectile.velocity.Length() < 30f)
            Projectile.velocity *= 1.01f;

        Projectile.Opacity = Animators.MakePoly(3f).InFunction(InverseLerp(0f, 40f, Time));
        Projectile.scale = Animators.MakePoly(2f).OutFunction(InverseLerp(0f, 40f, Projectile.timeLeft));
        Time++;
    }

    public float WidthFunct(float c)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(4f).InFunction(InverseLerp(0.3f, 0f, c)));
        float width = InverseLerp(1f, 0.4f, c) * tipInterpolant * Projectile.scale;
        return width * Projectile.width;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return new Color(255, 172, 28) * Projectile.Opacity;
    }

    public Trail trail;
    public TrailPoints points = new(20);

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;

            ManagedShader shader = AssetRegistry.GennedShaders.SpecialLightningTrail;
            shader.SetTexture(AssetRegistry.GennedTextures.Perlin, 1, SamplerState.LinearWrap);
            trail.DrawTrail(shader, points.Points, 100);
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
