using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class LesserBloodBeacon : ModProjectile
{
    public Vector2 Start;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Start);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Start = reader.ReadVector2();
    }
    public const int Lifetime = 60;
    public ref float Time => ref Projectile.ai[0];

    public const float LaserLength = 2000f;
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 200;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 9;
    }

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 24);
        if (trail2 == null || trail2._disposed)
            trail2 = new(AltWidthFunction, AltColorFunction, null, 24);

        if (Time == 0f)
        {
            Start = Projectile.Center;
            Projectile.velocity = Start.SafeDirectionTo(Start + Vector2.UnitY * 10f).RotatedByRandom(.3f);
            this.Sync();
        }

        Projectile.scale = InverseLerp(0f, Lifetime / 2, Time);
        Projectile.Opacity = 1f - InverseLerp(Lifetime * .7f, Lifetime, Time);
        Projectile.Center = Vector2.Lerp(Start - Projectile.velocity * LaserLength / 2, Start + Projectile.velocity * LaserLength / 2, Animators.MakePoly(3f).OutFunction(Projectile.scale));
        if (Time < Lifetime / 2)
            cache.Update(Projectile.Center);

        Time++;
    }

    public float WidthFunction(float _) => Projectile.width * 2f * Projectile.scale;

    public Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float colorInterpolant = 0.5f * MathF.Sin(-9f * Main.GlobalTimeWrappedHourly) + 0.5f;
        return Color.Lerp(Color.DarkRed, Color.Black, 0.25f * colorInterpolant) * Projectile.Opacity;
    }
    public float AltWidthFunction(float _) => WidthFunction(_) * 2f;
    public Color AltColorFunction(SystemVector2 completionRatio, Vector2 position) => ColorFunction(completionRatio, position) * .4f;

    public TrailPoints cache = new(200);
    public OptimizedPrimitiveTrail trail;
    public OptimizedPrimitiveTrail trail2;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail2 == null || trail._disposed || trail2._disposed || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.BloodBeacon;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 2);

            trail.DrawTrail(shader, cache.Points, 80);

            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WarpMap), 2);

            trail2.DrawTrail(shader, cache.Points, 80);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);
        PixelationSystem.QueueTextureRenderAction(Portal, PixelationLayer.OverProjectiles, null, ShaderRegistry.PortalShader);
        return false;
    }

    public void Portal()
    {
        Texture2D noiseTexture = AssetRegistry.GetTexture(AdditionsTexture.FractalNoise);
        Vector2 drawPosition = Start - Projectile.velocity * LaserLength / 2 - Main.screenPosition;
        Vector2 origin = noiseTexture.Size() * 0.5f;

        Color col1 = ColorSwap(Color.Crimson, Color.DarkRed * 2f, 1f);
        Color col2 = Color.Crimson * 1.5f;

        Vector2 diskScale = 2.5f * Projectile.scale * new Vector2(.3f, 1f);
        ManagedShader portal = ShaderRegistry.PortalShader;

        portal.TrySetParameter("opacity", Projectile.Opacity);
        portal.TrySetParameter("color", col1);
        portal.TrySetParameter("secondColor", col2);
        portal.TrySetParameter("globalTime", Projectile.scale * 1.2f);
        portal.Render();

        Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, Projectile.velocity.ToRotation(), origin, diskScale, SpriteEffects.None, 0f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(cache.Points, WidthFunction);
    }
}
