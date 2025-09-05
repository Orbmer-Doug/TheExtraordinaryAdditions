
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class OverloadedLightDart : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.OverloadedLightDart);
    public ref float Time => ref Projectile.ai[2];
    public ref float TypeOf => ref Projectile.ai[1];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;
    }
    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 66;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    public const float Supersonic = 25f;
    public override void SafeAI()
    {
        Time++;
        Projectile.velocity *= 1.016f;
        Projectile.FacingUp();

        if (Time < Supersonic)
        {
            Projectile.velocity *= .92f;
        }
        if (Time == Supersonic)
        {
            if (TypeOf == 1f)
                HeadToPlayer();

        }
        if (Time > Supersonic)
        {
            Projectile.extraUpdates = 6;
        }
    }
    internal void HeadToPlayer()
    {
        Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        Projectile.Center = Vector2.Lerp(Projectile.Center, closestPlayer.Center, 0.02f);
        Projectile.velocity = Projectile.SafeDirectionTo(closestPlayer.Center) * 28f;
    }
    public float TrailWidth(float completionRatio)
    {
        float tipInterpolant = MathF.Sqrt(1f - MathF.Pow(Utils.GetLerpValue(0.3f, 0f, completionRatio, true), 2f));
        float width = Utils.GetLerpValue(1f, 0.4f, completionRatio, true) * tipInterpolant * Projectile.scale * 8.5f;
        return width;
    }

    public Color TrailColor(float completionRatio)
    {
        float colorInterpolant = completionRatio;
        Color color = MulticolorLerp(colorInterpolant, Color.Gold, Color.Goldenrod, Color.Goldenrod * 1.6f, Color.Yellow);
        return color * Utils.GetLerpValue(1.2f, 5f, Projectile.velocity.Length(), true);
    }
    public float TelegraphWidthFunction(float completionRatio)
    {
        float telegraphInterpolant = Convert01To010(InverseLerp(0f, Supersonic - 4f, Time));
        return Projectile.width * .5f * telegraphInterpolant * MathHelper.SmoothStep(0.2f, 1f, Utils.GetLerpValue(0f, 0.3f, completionRatio, true));//Projectile.width * MathF.Sin(Utils.GetLerpValue(0f, LaserTelegraphTime, Time, true)); 
    }

    public Color TelegraphColorFunction(float completionRatio)
    {
        float endFadeOpacity = Utils.GetLerpValue(0f, 0.2f, completionRatio, true) * Utils.GetLerpValue(1f, 0.8f, completionRatio, true);

        float telegraphInterpolant = InverseLerp(0f, Supersonic - 4f, Time);
        Color telegraphColor = Color.Lerp(Color.PaleGoldenrod, Color.Gold, MathF.Pow(telegraphInterpolant, 0.6f)) * telegraphInterpolant;

        return telegraphColor * endFadeOpacity;
    }
    public void DrawTelegraph()
    {
        if (Asterlin.Myself is null) return;

        Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.Zero);
        if (TypeOf == 1f)
            laserDirection = Projectile.SafeDirectionTo(Main.player[Asterlin.Myself.target].Center).SafeNormalize(Vector2.Zero);

        Vector2 telegraphStart = Projectile.Center;
        Vector2 telegraphEnd = Projectile.Center + laserDirection * 2000f;

        Vector2[] telegraphPoints =
        [
                telegraphStart,
                Vector2.Lerp(telegraphStart, telegraphEnd, 0.25f),
                Vector2.Lerp(telegraphStart, telegraphEnd, 0.5f),
                Vector2.Lerp(telegraphStart, telegraphEnd, 0.75f),
                telegraphEnd
        ];


        ManagedShader shader = ShaderRegistry.SideStreakTrail;
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        DrawTelegraph();

        Color mainColor = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + Projectile.whoAmI * 0.12f) % 1, Color.LightGoldenrodYellow, Color.PaleGoldenrod, Color.LightYellow, Color.Yellow);

        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Projectile.DrawProjectileBackglow(Color.LightCyan, 3f, 100, 10);
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }

    public void DrawAdditive(SpriteBatch spriteBatch)
    {
        float bloomScale = Utils.GetLerpValue(0.8f, 2.4f, Projectile.velocity.Length(), true) * Projectile.scale;
        Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

        // Draw the bloom above the trail.
        Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[2] + Projectile.Size * 0.5f - Main.screenPosition, null, (Color.LightGoldenrodYellow * 0.2f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, bloomScale * 1.2f, 0, 0);
        Main.EntitySpriteDraw(bloomTexture, Projectile.oldPos[1] + Projectile.Size * 0.5f - Main.screenPosition, null, (Color.LightGoldenrodYellow * 0.5f) with { A = 0 }, 0, bloomTexture.Size() * 0.5f, bloomScale * 0.44f, 0, 0);
    }
}
