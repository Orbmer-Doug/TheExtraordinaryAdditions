using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class SupernovaGas : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CloudParticle);

    public ref float LightPower => ref Projectile.ai[0];

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 184;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = 210;
        Projectile.hide = true;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Projectile.scale = Main.rand.NextFloat(1f, 1.7f);
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.localAI[0] = 1f;
        }
        Color color = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6);
        Vector3 val = ((Color)color).ToVector3();
        float lightPowerBelow = ((Vector3)val).Length() / (float)Math.Sqrt(3.0);
        LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
        Projectile.Opacity = Utils.GetLerpValue(210f, 195f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 90f, Projectile.timeLeft, true);
        Projectile.rotation += Projectile.velocity.X * 0.004f;
        Projectile.velocity *= 0.985f;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        behindNPCsAndTiles.Add(index);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Color val = Color.Lerp(Color.DarkMagenta, Color.Crimson, (float)Cos01(Main.GlobalTimeWrappedHourly * 0.67f - 1f * 29f));

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D texture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
        Vector2 origin = texture.Size() * 0.5f;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity * 0.5f;
        Color drawColor = val * opacity;
        Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale;
        Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, scale, 0, 0f);
        Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        return false;
    }
}
