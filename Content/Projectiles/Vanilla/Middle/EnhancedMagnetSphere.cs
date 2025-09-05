using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class EnhancedMagnetSphere : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.scale = 1f;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 300;
        Projectile.penetrate = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 8;
    }
    public ref float Time => ref Projectile.ai[0];
    public const float ArcDistance = 750f;
    public const float ArcWidth = 4f;
    public const int FadeTime = 15;
    public override void AI()
    {
        // Create some light
        Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 1.1f);
        float interpolant = InverseLerp(0f, 10f, Time) * InverseLerp(0f, FadeTime, Projectile.timeLeft, true);
        Projectile.scale = Projectile.Opacity = interpolant;

        // Emit sparkles as it dies
        if (Projectile.timeLeft < FadeTime)
        {
            Projectile.velocity *= .985f;
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * Projectile.scale;
            Vector2 vel = RandomVelocity(2f, 2f, 5f);
            int lifetime = Main.rand.Next(14, 28);
            float scale = Main.rand.NextFloat(.5f, 1f) * Projectile.scale;
            Color color = Color.Lerp(Color.Cyan, Color.DarkCyan, interpolant);
            ParticleRegistry.SpawnSparkleParticle(pos, vel, lifetime, scale, color, color * 2, 1.1f);
        }
        Time++;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Bounce off of tiles
        if (Projectile.velocity.X != oldVelocity.X)
            Projectile.velocity.X = -oldVelocity.X;
        if (Projectile.velocity.Y != oldVelocity.Y)
            Projectile.velocity.Y = -oldVelocity.Y;

        return false;
    }
    private List<Projectile> OtherSpheres => Projectile.GetOtherProjs(ArcDistance, 3);

    internal float WidthFunction(float completionRatio)
    {
        return 4f * Convert01To010(completionRatio) * Projectile.scale;
    }
    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        Color magnet = new(2, 254, 201); // Magnet sphere spark

        float opacity = 0f;
        foreach (Projectile sphere in OtherSpheres)
        {
            float remainingDistance = Projectile.Center.Distance(sphere.Center) - Projectile.width;
            opacity = 1f - Utils.GetLerpValue(0f, ArcDistance, remainingDistance, true);
        }

        // Fade out based on distance from spheres
        return magnet * opacity * .7f;
    }

    internal float BackgroundWidthFunction(float completionRatio) => WidthFunction(completionRatio) * 2f;

    internal Color BackgroundColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        Color dimmerMagnet = new(24, 184, 108); // Magnet sphere middle

        float opacity = 0f;
        foreach (Projectile sphere in OtherSpheres)
        {
            float remainingDistance = Projectile.Center.Distance(sphere.Center) - Projectile.width;
            opacity = 1f - Utils.GetLerpValue(0f, ArcDistance, remainingDistance, true);
        }

        return dimmerMagnet * opacity * .6f;
    }

    public void DrawArcs(Vector2 destination)
    {
        void draw()
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.SafeDirectionTo(destination) * (start.Distance(destination) - Projectile.width);
            List<Line> lightning = CreateBolt(start, end, 1f, 20f);
            ManualTrailPoints final = new(lightning.Count * 2);
            List<Vector2> ends = [];
            for (int i = 0; i < lightning.Count; i++)
            {
                Line line = lightning[i];
                ends.Add(line.a);
                ends.Add(line.b);
            }
            final.SetPoints(ends);

            ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CausticNoise), 1);

            OptimizedPrimitiveTrail trail = new(WidthFunction, ColorFunction, null, final.Count + 10);
            trail.DrawTrail(shader, final.Points, 30);

            OptimizedPrimitiveTrail trail2 = new(BackgroundWidthFunction, BackgroundColorFunction, null, final.Count + 10);
            trail.DrawTrail(shader, final.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Projectile.timeLeft <= FadeTime)
            return false;

        foreach (Projectile sphere in OtherSpheres)
        {
            float _ = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, sphere.Center, ArcWidth, ref _))
                return true;
        }
        return null;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Draw arcs to each available sphere
        foreach (Projectile sphere in OtherSpheres)
            DrawArcs(sphere.Center);

        ManagedShader shader = ShaderRegistry.Forcefield;
        float intensity = 1f;
        float flickerPower = 0.25f;
        float opacity = MathHelper.Lerp(1f, MathHelper.Max(1f - flickerPower, 0.56f), MathF.Pow(MathF.Cos(Main.GlobalTimeWrappedHourly * MathHelper.Lerp(3f, 5f, flickerPower)), 24f)) * 2f;
        Color color = new(15, 88, 113); // Magnet sphere within
        Color color2 = new(25, 153, 126); // Magnet sphere outline
        float noiseScale = MathHelper.Lerp(0.4f, 0.8f, Sin01(Main.GlobalTimeWrappedHourly * 0.3f));

        // Colors get effected by the opacity
        color *= opacity;
        color2 *= opacity;

        shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 1.4f);
        shader.TrySetParameter("color", color);
        shader.TrySetParameter("edgeColor", color2);
        shader.TrySetParameter("saturation", intensity);
        shader.TrySetParameter("opacity", opacity);
        shader.TrySetParameter("noiseScale", noiseScale);
        shader.TrySetParameter("blowUpPower", 1f);
        shader.TrySetParameter("blowUpSize", .5f);
        shader.TrySetParameter("edgeBlendStrength", 4f);
        shader.TrySetParameter("resolution", Projectile.Size);
        
        PixelationSystem.QueueTextureRenderAction(DrawMagnetField, PixelationLayer.OverPlayers, BlendState.Additive, shader, Type);
        return false;
    }

    public void DrawMagnetField()
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin);
        Main.spriteBatch.Draw(tex, ToTarget(Projectile.Center, new(Projectile.Opacity * Projectile.height)), null,
            Color.White * Projectile.Opacity, MathHelper.PiOver2, tex.Size() * 0.5f, SpriteEffects.None, 0f);
    }
}
