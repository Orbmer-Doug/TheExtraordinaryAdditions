using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.ScreenEffects;
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
    public bool Nuke
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Rot => ref Projectile.ai[2];

    public float Interpolant => InverseLerp(0f, HemoglobTelegraph.TeleTime, Time);
    public const float Kaboom = 10000f;
    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(c => Projectile.height / 2, (c, pos) => Color.Crimson * MathHelper.SmoothStep(1f, 0f, c.X), null, 20);

        Projectile.VelocityBasedRotation();
        Lighting.AddLight(Projectile.Center, Color.Crimson.ToVector3() * 3f);

        if (Owner != null && Nuke)
        {
            Rot = (Rot + .2f) % MathHelper.TwoPi;
            Projectile.Center = Owner.Center + new Vector2(0f, -StygainHeart.BarrierSize * Interpolant * 1.5f) + PolarVector(new Vector2(Owner.width, .4f) * (1f - Interpolant), Rot);

            Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularLimited(300f, 300f, .4f, 1f);
            Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
            if (Time % 2 == 1)
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(50, 90), Main.rand.NextFloat(.4f, .8f), Color.DarkRed, Color.Crimson, Projectile.Center, 2f, 6);
            if (Main.rand.NextBool(3))
                ParticleRegistry.SpawnCloudParticle(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f), Color.IndianRed,
                    Color.DarkRed, Main.rand.Next(30, 40), Main.rand.NextFloat(40f, 70f), Main.rand.NextFloat(.5f, .9f), 1);
            if (Time % 10 == 9)
                ParticleRegistry.SpawnMenacingParticle(Projectile.RandAreaInEntity(), -Vector2.UnitY * Main.rand.NextFloat(1f, 5f), 50, Main.rand.NextFloat(.8f, 1f), Color.DarkRed);
        }

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

        if (!Nuke)
        {
            sb.EnterShaderRegion();

            Texture2D telegraphBase = AssetRegistry.InvisTex;
            ManagedShader circle = ShaderRegistry.CircularAoETelegraph;
            circle.TrySetParameter("opacity", InverseLerp(0f, 60f, Time) * .78f);
            circle.TrySetParameter("color", Color.Lerp(Color.MediumVioletRed, Color.PaleVioletRed, 0.7f * (float)Math.Pow(Sin01(Main.GlobalTimeWrappedHourly), 3.0)));
            circle.TrySetParameter("secondColor", Color.Lerp(Color.MediumVioletRed, Color.White, 0.4f));
            circle.Render();

            Main.EntitySpriteDraw(telegraphBase, drawPosition, null, Color.White, 0f, telegraphBase.Size() / 2f, 1000f, 0, 0f);

            sb.ExitShaderRegion();
        }

        if (trail != null && !trail._disposed && cache != null)
            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, cache.Points, 40);

        Texture2D texture = Projectile.ThisProjectileTexture();
        if (Nuke)
            Projectile.DrawProjectileBackglow(Color.Crimson, 12f * Interpolant, 20, 20);
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.DarkRed), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }

    public override bool CanHitPlayer(Player target) => false;
    public override void OnKill(int timeLeft)
    {
        if (Nuke)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (player != null && !player.dead && player.WithinRange(Projectile.Center, Kaboom))
                    player.velocity += Projectile.SafeDirectionTo(player.Center) * 12f;
            }
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, 30, .4f, Kaboom);
            ScreenShakeSystem.New(new(18f, 1.2f), Projectile.Center);
            AdditionsSound.GaussBoomLittle.Play(Projectile.Center, .8f, -.4f);
        }

        else
        {
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, 15, .12f, 1200f);
            ScreenShakeSystem.New(new(10f, .7f), Projectile.Center);
            AdditionsSound.GaussBoomLittle.Play(Projectile.Center, 1f, 0f, .1f, 10);
        }

        Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ConcentratedBloodExplosion>(),
            Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, Nuke.ToInt());
    }
}