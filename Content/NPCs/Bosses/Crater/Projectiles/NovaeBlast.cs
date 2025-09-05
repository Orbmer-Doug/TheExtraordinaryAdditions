using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class NovaeBlast : ProjOwnedByNPC<Asterlin>
{
    public override bool IgnoreOwnerActivity => true;
    public static readonly int Lifetime = SecondsToFrames(1f);

    public ref float Radius => ref Projectile.ai[0];
    public const int MaxRadius = 800;
    public static Color DetermineExplosionColor()
    {
        Color c = Color.Lerp(Color.Goldenrod, Color.Wheat, 0.24f);
        c = Color.Lerp(c, Color.Gold, Main.rand.NextFloat(.4f, .9f));
        return c with { A = 80 };
    }

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.scale = 0.001f;

        Projectile.hostile = true;
        Projectile.friendly = false;
    }

    public override void SafeAI()
    {
        // Cause the wave to expand outward, along with its hitbox
        Radius = MathHelper.Lerp(Radius, MaxRadius, 0.039f);
        Projectile.scale = MathHelper.Lerp(.8f, 1.2f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Projectile.Opacity = InverseLerp(2f, 15f, Projectile.timeLeft);

        // Randomly create small light particles
        float lightVelocityArc = MathHelper.Pi * InverseLerp(Lifetime, 0f, Projectile.timeLeft);
        for (int i = 0; i < 6; i++)
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Unit() * Radius * Projectile.scale * Main.rand.NextFloat(0.75f, 0.96f);
            Vector2 vel = (pos - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(lightVelocityArc) * Main.rand.NextFloat(2f, 25f);
            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, Main.rand.Next(25, 44), Main.rand.NextFloat(.34f, .61f), Color.Yellow.Lerp(Color.Wheat, Main.rand.NextFloat(.7f)));
        }
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {

    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius * 0.4f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise);
        DrawData explosionDrawData = new(tex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

        ManagedShader shockwaveShader = ShaderRegistry.LightShockwave;
        shockwaveShader.TrySetParameter("mainColor", DetermineExplosionColor().ToVector3());
        shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
        shockwaveShader.TrySetParameter("projPosition", Projectile.Center - Main.screenPosition);
        shockwaveShader.TrySetParameter("shockwaveOpacity", Projectile.Opacity);
        shockwaveShader.Render();
        explosionDrawData.Draw(Main.spriteBatch);

        Main.spriteBatch.ResetToDefault();
        return false;
    }
}
