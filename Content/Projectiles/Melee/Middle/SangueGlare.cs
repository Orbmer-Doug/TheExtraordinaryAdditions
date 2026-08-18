using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SangueGlare : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Gleam.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
    }

    public ref float Time => ref Projectile.ai[0];
    private static readonly int Life = SecondsToFrames(1.8f);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {
        Projectile.Opacity = GetLerpBump(0f, 10f, Life, Life - 30f, Time);
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

        if (Projectile.velocity.Length() < 33f)
            Projectile.velocity *= 1.033f;

        Time++;
        if (Time > 20f && NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 400, true, true), out NPC target))
            Projectile.velocity =
                Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 12f, .14f);
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.4f) * Main.rand.NextFloat(2f, 5f),
                Main.rand.Next(30, 60), Main.rand.NextFloat(.6f, 1.4f), Color.DarkRed, Color.Crimson, null, 1.2f, 4);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
        Vector2 origin = texture.Size() * 0.5f;
        for (int i = 0; i < Projectile.oldPos.Length; ++i)
        {
            float afterimageRot = Projectile.oldRot[i];
            Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition +
                              new Vector2(0f, Projectile.gfxOffY);
            Color afterimageColor = Color.DarkRed *
                                    ((Projectile.oldPos.Length - i) / (float) Projectile.oldPos.Length) *
                                    Projectile.Opacity;
            SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture, drawPos,
                null, afterimageColor, afterimageRot, origin,
                Projectile.scale * 0.5f);

            if (i > 0)
            {
                for (float j = 0.2f; j < 0.8f; j += 0.2f)
                {
                    drawPos = Vector2.Lerp(Projectile.oldPos[i - 1], Projectile.oldPos[i], j) +
                        Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                    SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture,
                        drawPos, null, afterimageColor, afterimageRot, origin,
                        Projectile.scale * 0.5f);
                }
            }
        }

        Color color = Color.DarkRed * 0.5f;
        color.A = 0;

        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture, drawPosition,
            null, color, Projectile.rotation, origin,
            Projectile.scale * 0.9f);
        Color bigGleamColor = color;
        Color smallGleamColor = color * 0.5f;
        float opacity = Projectile.Opacity *
                        (1f + 0.2f * MathF.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * MathHelper.Pi * 6f)) *
                        0.8f;
        Vector2 bigGleamScale = new Vector2(0.5f, 5f) * opacity;
        Vector2 smallGleamScale = new Vector2(0.5f, 2f) * opacity;
        bigGleamColor *= opacity;
        smallGleamColor *= opacity;

        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture, drawPosition,
            null, bigGleamColor, MathHelper.PiOver2, origin, bigGleamScale);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture, drawPosition,
            null, bigGleamColor, 0f, origin, smallGleamScale);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture, drawPosition,
            null, smallGleamColor, MathHelper.PiOver2, origin,
            bigGleamScale * 0.6f);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, texture, drawPosition,
            null, smallGleamColor, 0f, origin, smallGleamScale * 0.6f);

        return false;
    }
}
