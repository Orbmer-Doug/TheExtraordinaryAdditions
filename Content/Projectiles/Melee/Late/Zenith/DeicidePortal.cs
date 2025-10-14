using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Zenith;

public class DeicidePortal : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public static readonly int Life = SecondsToFrames(8);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 210;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Life;
        Projectile.DamageType = DamageClass.Melee;
    }
    public ref float Time => ref Projectile.ai[0];
    public bool Fade
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public int SpearIndex
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public override void AI()
    {
        if (SearchForSpear() && Projectile.timeLeft > 20)
        {
            Projectile proj = Main.projectile?[SpearIndex] ?? null;
            if (proj != null)
            {
                FinalStrikeHoldout spear = proj.As<FinalStrikeHoldout>();
                if (proj.RotHitbox().Intersects(Projectile.RotHitbox()) && spear.CurrentState == FinalStrikeHoldout.FinalStrikeState.Fire)
                {
                    spear.StateTime = 0f;
                    spear.PortalIndex = Projectile.whoAmI;
                    spear.CurrentState = FinalStrikeHoldout.FinalStrikeState.Wait;
                }
            }
        }
        if (Fade)
        {
            Projectile.timeLeft = 20;
            Fade = !Fade;
        }

        Projectile.scale = Projectile.Opacity = InverseLerp(0f, 20f, Time) * InverseLerp(0f, 20f, Projectile.timeLeft);
        Time++;
    }

    public bool SearchForSpear()
    {
        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (proj.type == ModContent.ProjectileType<FinalStrikeHoldout>() && proj.owner == Projectile.owner)
            {
                SpearIndex = proj.whoAmI;
                return true;
            }
        }
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D noiseTexture = AssetRegistry.GetTexture(AdditionsTexture.FlameMap1);
        Vector2 origin = noiseTexture.Size() * 0.5f;
        Main.spriteBatch.EnterShaderRegion();

        Color col1 = ColorSwap(Color.Wheat, Color.Wheat * 2f, 1f);
        Color col2 = Color.White;

        Vector2 diskScale = Projectile.height * Projectile.scale * new Vector2(1f, 1f);
        ManagedShader portal = ShaderRegistry.PortalShader;

        portal.TrySetParameter("opacity", Projectile.Opacity);
        portal.TrySetParameter("color", col1);
        portal.TrySetParameter("secondColor", col2);
        portal.TrySetParameter("globalTime", Projectile.scale * 1.2f);
        portal.Render();

        Main.spriteBatch.Draw(noiseTexture, ToTarget(Projectile.Center, diskScale * 2), null, Color.White, Projectile.rotation, origin, SpriteEffects.None, 0f);

        portal.TrySetParameter("secondColor", col2 * 2f);
        portal.Render();
        Main.spriteBatch.Draw(noiseTexture, ToTarget(Projectile.Center, diskScale * 2), null, Color.White, Projectile.rotation, origin, SpriteEffects.None, 0f);

        Main.spriteBatch.ExitShaderRegion();

        return false;
    }

    public override bool ShouldUpdatePosition() => false;
}
