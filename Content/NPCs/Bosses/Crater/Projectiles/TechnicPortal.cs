using Terraria;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Resources;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public class TechnicPortal : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public override bool IgnoreOwnerActivity => true;

    public override void SetDefaults()
    {
        Projectile.Size = new(250, 700);
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Asterlin.UnrelentingRush_PortalLifetime;
    }

    public override void SafeAI()
    {
        float comp = InverseLerp(Asterlin.UnrelentingRush_PortalLifetime, 0f, Projectile.timeLeft);

        if (Projectile.timeLeft < Asterlin.UnrelentingRush_PortalFadeOut)
        {
            Projectile.Opacity = MakePoly(3f)
                .InOutFunction(InverseLerp(0, Asterlin.UnrelentingRush_PortalFadeOut, Projectile.timeLeft));
            Projectile.scale = MakePoly(4f)
                .OutFunction(InverseLerp(0, Asterlin.UnrelentingRush_PortalFadeOut, Projectile.timeLeft));
        }
        else
        {
            Projectile.Opacity = MakePoly(3f).InOutFunction(InverseLerp(Asterlin.UnrelentingRush_PortalLifetime,
                Asterlin.UnrelentingRush_PortalLifetime - Asterlin.UnrelentingRush_PortalFadeIn, Projectile.timeLeft));
            Projectile.scale = MakePoly(2f).OutFunction(InverseLerp(Asterlin.UnrelentingRush_PortalLifetime,
                Asterlin.UnrelentingRush_PortalLifetime - Asterlin.UnrelentingRush_PortalFadeIn, Projectile.timeLeft));
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader shader = AssetRegistry.GennedShaders.PortalShaderAlt;
        return false;
    }
}
