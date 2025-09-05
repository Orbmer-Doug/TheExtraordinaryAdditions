using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public class HemoglobTelegraph : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 4500;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = TeleTime;
    }

    public ref float Time => ref Projectile.ai[0];
    public const int TeleTime = 250;
    
    public override void SafeAI()
    {
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Create a circle to indicate the safe zone
        SpriteBatch sb = Main.spriteBatch;

        sb.EnterShaderRegion();

        Texture2D telegraphBase = AssetRegistry.InvisTex;
        ManagedShader circle = ShaderRegistry.InverseCircularAOE;
        circle.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1, SamplerState.LinearWrap);

        Color main = Color.Lerp(Color.MediumVioletRed, Color.PaleVioletRed, 0.7f * (float)Math.Pow(Sin01(Main.GlobalTimeWrappedHourly), 3.0));
        Color outer = Color.Lerp(Color.MediumVioletRed, Color.White, 0.4f);

        circle.TrySetParameter("MainColor", main.ToVector3());
        circle.TrySetParameter("OuterColor", outer.ToVector3());
        float completion = InverseLerp(0f, TeleTime, Time);
        float opacity = Convert01To010(completion) - .35f;
        circle.TrySetParameter("brightness", opacity);
        circle.Render();

        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(telegraphBase, drawPosition, null, Color.White, 0f, telegraphBase.Size() / 2f, StygainHeart.BarrierSize * 8, 0, 0f);

        sb.ExitShaderRegion();

        // Draw evenly spaced arrows to emphasize to potentially offscreen players where to go
        // Otherwise they may just see a fog appear and kaboom
        const int Count = 16;
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.HemoglobTeleArrow);
        Vector2 orig = tex.Size() / 2;

        float timer = TeleTime - Projectile.timeLeft;
        float prog = timer / TeleTime;

        // The amount of rays
        for (int i = 0; i < Count; i++)
        {
            // Fluctuate in size
            float size = 2.5f + Utils.GetLerpValue(0.8f, 1f, Cos01(i / Count + Main.GlobalTimeWrappedHourly * -(float)Math.PI), clamped: true) * 0.15f;

            // The distance apart to multiply by
            for (int k = 1; k <= 4; k++)
            {
                Vector2 pos = Projectile.Center + (MathHelper.TwoPi * i / Count).ToRotationVector2() * ((-(StygainHeart.BarrierSize + 64) + Animators.BezierEase(timer) * 64) * k);
                Color col = Color.Crimson * (float)Math.Sin(prog * MathHelper.TwoPi * 4 - i) * (float)Math.Sin(prog * MathHelper.Pi);

                // Point to the end
                float rot = pos.SafeDirectionTo(Projectile.Center).ToRotation();

                sb.Draw(tex, pos - Main.screenPosition, null, col, rot, orig, size, 0, 0);
            }
        }
        return false;
    }
}
