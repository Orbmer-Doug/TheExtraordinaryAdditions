using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class TechnicPortal : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
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
            Projectile.Opacity = MakePoly(3f).InOutFunction(InverseLerp(0, Asterlin.UnrelentingRush_PortalFadeOut, Projectile.timeLeft));
            Projectile.scale = MakePoly(4f).OutFunction(InverseLerp(0, Asterlin.UnrelentingRush_PortalFadeOut, Projectile.timeLeft));
        }
        else
        {
            Projectile.Opacity = MakePoly(3f).InOutFunction(InverseLerp(Asterlin.UnrelentingRush_PortalLifetime, Asterlin.UnrelentingRush_PortalLifetime - Asterlin.UnrelentingRush_PortalFadeIn, Projectile.timeLeft));
            Projectile.scale = MakePoly(2f).OutFunction(InverseLerp(Asterlin.UnrelentingRush_PortalLifetime, Asterlin.UnrelentingRush_PortalLifetime - Asterlin.UnrelentingRush_PortalFadeIn, Projectile.timeLeft));
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader shader = AssetRegistry.GetShader("PortalShaderAlt");
        void portal()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            shader.TrySetParameter("scale", Projectile.scale);
            shader.TrySetParameter("coolColor", Color.DarkCyan.ToVector3());
            shader.TrySetParameter("mediumColor", Color.Cyan.ToVector3());
            shader.TrySetParameter("hotColor", Color.DeepSkyBlue.ToVector3());
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseZoomedOut), 2, SamplerState.LinearWrap);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Projectile.Size), null, Color.White, Projectile.rotation, tex.Size() / 2);
        }
        PixelationSystem.QueueTextureRenderAction(portal, PixelationLayer.OverNPCs, null, shader);
        return false;
    }
}
