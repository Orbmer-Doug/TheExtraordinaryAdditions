using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.SolarGuardian;

public class Sunray : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
    }
    private const int Lifetime = 45;
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 32;
        Projectile.alpha = 255;
        Projectile.penetrate = 4;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.MaxUpdates = 3;
        Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }
    public override void AI()
    {
        Projectile.oldPos[1] = Projectile.oldPos[0];
        float adjustedTimeLife = Projectile.timeLeft / Projectile.MaxUpdates;
        Projectile.Opacity = Projectile.scale = GetLerpBump(0f, 9f, Lifetime, Lifetime - 9f, adjustedTimeLife);
        Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 1.1f);

        if (Projectile.velocity.Length() < 16f) Projectile.velocity *= 1.03f;
    }

    public float WidthFunction(float completionRatio)
    {
        return Convert01To010(completionRatio) * Projectile.scale * Projectile.width;
    }

    public Color ColorFunction(Vector2 completionRatio)
    {
        return MulticolorLerp(Sin01(Projectile.identity / 3f + completionRatio.X * 20f + Main.GlobalTimeWrappedHourly * 1.1f),
            new Color(Main.rand.Next(255, 255), 172, 28), new Color(Main.rand.Next(255, 255), 172, 28));
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(BuffID.OnFire3, 180, true, false);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1);
        shader.Render();

        //PrimitiveTrail trail = new(Projectile.oldPos, shader, WidthFunction, ColorFunction, (c) => Projectile.Size / 2, 20);
        //trail.DrawTrail();
        return false;
    }
}
