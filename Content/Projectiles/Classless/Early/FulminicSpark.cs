using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;

public class FulminicSpark : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public const int Life = 60;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Life;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.penetrate = 1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Power => ref Projectile.ai[1];
    public float Completion => Animators.MakePoly(5).OutFunction(InverseLerp(0f, Life, Time));

    private List<Line> Branches = [];
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (Time == 0f)
        {
            Branches = CreateBolt(Projectile.Center, Projectile.velocity, 1f);
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity.BetweenNum(0f, .05f))
            Projectile.Kill();

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (Line line in Branches)
        {
            int width = 16;
            if (new Rectangle((int)line.a.X - width / 2, (int)line.a.Y - width / 2, width, width).Intersects(targetHitbox))
                return true;
        }

        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Branches == null || Branches.Count == 0)
            return false;
        foreach (Line line in Branches)
        {
            PixelationSystem.QueueTextureRenderAction(() => line.Draw(MulticolorLerp(Completion, Color.White, Color.LightPink, Color.Violet, Color.DarkViolet)
                * Projectile.Opacity), PixelationLayer.OverNPCs, BlendState.Additive);
        }
        return false;
    }
}