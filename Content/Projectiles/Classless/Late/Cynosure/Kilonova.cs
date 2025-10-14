using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class Kilonova : ModProjectile
{
    public ref float Time => ref Projectile.ai[0];

    public static int Lifetime => SecondsToFrames(8f);

    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 25000;

    public override void SetDefaults()
    {
        Projectile.width = 200;
        Projectile.height = 200;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = true;
        Projectile.timeLeft = Lifetime;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.scale = 0.001f;
    }

    public override void AI()
    {
        // Grow over time.
        Projectile.scale = Utils.Remap(Time, 0f, 30f, 0f, 20f);

        // Dissipate at the end.
        Projectile.Opacity = InverseLerp(8f, 120f, Projectile.timeLeft);

        Time++;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        behindProjectiles.Add(index);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader nova = ShaderRegistry.Supernova;
        float interpolant = InverseLerp(0f, Lifetime, Time);
        nova.TrySetParameter("supernovaColor1", CollapsingStar.SupernovaColor(interpolant).ToVector3());
        nova.TrySetParameter("supernovaColor2", (CollapsingStar.SupernovaColor(interpolant) * .7f).ToVector3());
        nova.TrySetParameter("generalOpacity", Projectile.Opacity);
        nova.TrySetParameter("scale", Projectile.scale);
        nova.TrySetParameter("brightness", InverseLerp(20f, 4f, Projectile.scale) * 3f + 2.25f);
        nova.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1);
        nova.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 2);
        nova.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos2), 3);
        nova.Render();

        Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, nova.Shader.Value);
        Texture2D tex = AssetRegistry.InvisTex;
        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity * 0.42f, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * 400f, 0, 0f);
        Main.spriteBatch.ExitShaderRegion();

        return false;
    }
}