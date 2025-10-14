using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class BloodMoonlet : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BloodMoonlet);

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 50;
        Projectile.hostile = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 180;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => Projectile.height / 2, (c, pos) => Color.Crimson * MathHelper.SmoothStep(1f, 0f, c.X), null, 20);

        Projectile.VelocityBasedRotation();
        Lighting.AddLight(Projectile.Center, Color.Crimson.ToVector3() * 3f);

        cache ??= new(20);
        cache.Update(Projectile.Center);
        Time++;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        SpriteBatch sb = Main.spriteBatch;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;

        sb.EnterShaderRegion();

        Texture2D telegraphBase = AssetRegistry.InvisTex;
        ManagedShader circle = ShaderRegistry.CircularAoETelegraph;
        circle.TrySetParameter("opacity", InverseLerp(0f, 60f, Time) * .78f);
        circle.TrySetParameter("color", Color.Lerp(Color.MediumVioletRed, Color.PaleVioletRed, 0.7f * (float)Math.Pow(Sin01(Main.GlobalTimeWrappedHourly), 3.0)));
        circle.TrySetParameter("secondColor", Color.Lerp(Color.MediumVioletRed, Color.White, 0.4f));
        circle.Render();

        Main.EntitySpriteDraw(telegraphBase, drawPosition, null, Color.White, 0f, telegraphBase.Size() / 2f, 1000f, 0, 0f);

        sb.ExitShaderRegion();

        if (trail != null && !trail.Disposed && cache != null)
            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, cache.Points, 40, false, false);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.DarkRed), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }

    public override bool CanHitPlayer(Player target) => false;
    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnBlurParticle(Projectile.Center, 35, .12f, 1200f);
        ScreenShakeSystem.New(new(10f, .7f), Projectile.Center);
        AdditionsSound.GaussBoomLittle.Play(Projectile.Center, 1f, 0f, .1f, 10);

        if (this.RunServer())
            SpawnProjectile(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ConcentratedBloodExplosion>(),
                Projectile.damage, Projectile.knockBack);
    }
}