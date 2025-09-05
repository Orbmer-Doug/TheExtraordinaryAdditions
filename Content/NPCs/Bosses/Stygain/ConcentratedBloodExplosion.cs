using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public class ConcentratedBloodExplosion : ModProjectile
{
    public static readonly float Lifetime = SecondsToFrames(1.1f);

    public ref float Radius => ref Projectile.ai[0];
    public bool Nuke
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public static Color DetermineExplosionColor()
    {
        Color c = Color.Lerp(Color.DarkRed, Color.IndianRed, 0.24f);
        c = Color.Lerp(c, Color.Crimson, Main.rand.NextFloat(.4f, .9f));
        return c with { A = 80 };
    }

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;
    }

    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = (int)Lifetime;
        Projectile.scale = 0.001f;

        Projectile.hostile = true;
        Projectile.friendly = false;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 16;
    }

    public NPC Owner => StygainHeart.Myself;
    public override void AI()
    {
        // Cause the wave to expand outward, along with its hitbox.
        Radius = Nuke ? Animators.Circ.OutFunction.Evaluate(Lifetime - Projectile.timeLeft, 0f, Lifetime, 0f, BloodMoonlet.Kaboom) : MathHelper.Lerp(Radius, 1200f, 0.39f);
        Projectile.scale = MathHelper.Lerp(0f, 1f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Projectile.Opacity = InverseLerp(2f, 15f, Projectile.timeLeft);
    }

    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        if (target.Center.WithinRange(Owner.Center, StygainHeart.BarrierSize))
        {
            modifiers.FinalDamage *= 0f;
            modifiers.Knockback *= 0f;
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius * 0.4f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.FractalNoise);
        DrawData explosionDrawData = new(tex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

        ManagedShader shockwaveShader = ShaderRegistry.ShockwaveShader;
        shockwaveShader.TrySetParameter("mainColor", DetermineExplosionColor().ToVector3());
        shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
        shockwaveShader.TrySetParameter("projPosition", Projectile.Center - Main.screenPosition);
        shockwaveShader.TrySetParameter("shockwaveOpacity", Projectile.Opacity * (Nuke ? .8f : .4f));
        shockwaveShader.Render();
        explosionDrawData.Draw(Main.spriteBatch);

        Main.spriteBatch.ResetToDefault();
        return false;
    }
}
