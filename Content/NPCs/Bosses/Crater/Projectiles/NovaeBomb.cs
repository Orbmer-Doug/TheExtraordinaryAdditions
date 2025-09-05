using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.ScreenEffects;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class NovaeBomb : ProjOwnedByNPC<Asterlin>
{
    public override bool IgnoreOwnerActivity => true;
    public override string Texture => AssetRegistry.Invis;
    public static readonly int Lifetime = SecondsToFrames(2);
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 32;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override bool? CanDamage() => false;
    public ref float Radius => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float Animation => ref Projectile.ai[2];
    public override void SafeAI()
    {
        Projectile.FacingUp();
        Projectile.velocity *= .995f;
        Radius = Animators.MakePoly(5f).OutFunction.Evaluate(Time, 0f, 14f, 0f, 140f);

        Time++;
        Animation += .01f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        Projectile.Kill();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);
    }

    public override void OnKill(int timeLeft)
    {
        float off = RandomRotation();
        ParticleRegistry.SpawnFlash(Projectile.Center, 40, .8f, NovaeBlast.MaxRadius);
        ScreenShakeSystem.New(new(.9f, .2f), Projectile.Center);
        AdditionsSound.BraveSpecial2A.Play(Projectile.Center, 1f, 0f, .1f);
        SpawnProjectile(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<NovaeBlast>(), Projectile.damage, 0f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader shader = AssetRegistry.GetShader("NovaeFlames");
        shader.TrySetParameter("time", Animation);
        shader.TrySetParameter("resolution", new Vector2(Radius * .1f));
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        shader.Render("AutoloadPass", false, false);
        Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(Radius)), null, Color.White, Projectile.rotation, tex.Size() / 2, 0);
        Main.spriteBatch.ExitShaderRegion();

        Main.spriteBatch.EnterShaderRegion();
        Texture2D telegraphBase = AssetRegistry.InvisTex;
        ManagedShader circle = ShaderRegistry.CircularAoETelegraph;

        float opac = Animators.MakePoly(3f).InOutFunction.Evaluate(Time, 0f, 30f, 0f, 1.8f);
        circle.TrySetParameter("opacity", opac);
        circle.TrySetParameter("color", Color.Lerp(Color.Gold, Color.Goldenrod, MathF.Pow(Sin01(Main.GlobalTimeWrappedHourly * 2f), 2)));
        circle.TrySetParameter("secondColor", Color.DarkGoldenrod);
        circle.Render();

        Main.spriteBatch.DrawBetterRect(telegraphBase, ToTarget(Projectile.Center, new(NovaeBlast.MaxRadius)), null, Color.White, 0f, telegraphBase.Size() / 2f);
        Main.spriteBatch.ExitShaderRegion();
        return false;
    }
}
