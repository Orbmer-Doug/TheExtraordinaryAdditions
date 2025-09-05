using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

internal class LightripPortal : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true; Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Time;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
        Projectile.scale = 0f;
    }
    private const int Time = 60;
    public ref float Timer => ref Projectile.ai[0];
    public override void AI()
    {
        Timer++;
        Projectile.scale = Projectile.Opacity = GetLerpBump(0f, 12f, Time, Time - 12f, Timer) * 2.5f;
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnTwinkleParticle(Projectile.Center, Vector2.Zero, 35, new(1f), Color.DeepSkyBlue, 4);
    }
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor)
    {
        PixelationSystem.QueueTextureRenderAction(SpecialDraw, PixelationLayer.UnderProjectiles, null, ShaderRegistry.PortalShader);
        return false;
    }
    public override bool ShouldUpdatePosition() => false;
    public void SpecialDraw()
    {
        Texture2D noiseTexture = AssetRegistry.GetTexture(AdditionsTexture.FlameMap2);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = noiseTexture.Size() * 0.5f;

        Color col1 = ColorSwap(Color.Cyan, Color.Cyan * 2f, 1f);
        Color col2 = Color.DeepSkyBlue;

        Vector2 diskScale = Projectile.scale * new Vector2(.5f, 1f);
        ManagedShader portal = ShaderRegistry.PortalShader;

        portal.TrySetParameter("opacity", Projectile.Opacity);
        portal.TrySetParameter("color", col1);
        portal.TrySetParameter("secondColor", col2);
        portal.TrySetParameter("globalTime", Projectile.scale * 2f);
        portal.Render();

        Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, Projectile.rotation, origin, diskScale, SpriteEffects.None, 0f);

        portal.TrySetParameter("color", Color.Lerp(Color.White, Color.Cyan, .4f));
        portal.TrySetParameter("secondColor", Color.Lerp(Color.White, col2, .5f));
        portal.Render();
        Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, Projectile.rotation, origin, diskScale * .5f, SpriteEffects.None, 0f);
    }
}
