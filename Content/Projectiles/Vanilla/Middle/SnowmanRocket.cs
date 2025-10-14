using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class SnowmanRocket : ModProjectile
{
    public override string Texture => ProjectileID.MiniNukeSnowmanRocketII.GetTerrariaProj();

    /// <summary>
    /// thanks red real convenient
    /// </summary>
    public enum RocketType
    {
        /// <summary>
        /// Regular
        /// </summary>
        One,
        /// <summary>
        /// Regular, destroys tiles
        /// </summary>
        Two,
        /// <summary>
        /// Big
        /// </summary>
        Three,
        /// <summary>
        /// Big, destroys tiles
        /// </summary>
        Four,
        /// <summary>
        /// Weak, erases liquid
        /// </summary>
        Dry,
        /// <summary>
        /// Weak, makes water
        /// </summary>
        Wet,
        /// <summary>
        /// Weak, makes honey
        /// </summary>
        Honey,
        /// <summary>
        /// Weak, makes lava
        /// </summary>
        Lava,
        /// <summary>
        /// Weak, makes more rockets
        /// </summary>
        Cluster1,
        /// <summary>
        /// Regular, makes more rockets 
        /// </summary>
        Cluster2,
        /// <summary>
        /// Huge
        /// </summary>
        MiniNuke1,
        /// <summary>
        /// Huge, destroys tiles
        /// </summary>
        MiniNuke2,
    }

    public override void SetDefaults()
    {
        Projectile.aiStyle = -1;
        Projectile.width = Projectile.height = 14;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
    }

    /// <summary>
    /// Would use a getter with RocketType but just in case the projectile overrides get in we may need this
    /// </summary>
    public ref float State => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => MathHelper.SmoothStep(Projectile.width, 0f, c), ColorFunct, null, 40);

        switch (State)
        {
            case (int)RocketType.One:
                break;
            case (int)RocketType.Two:
                break;
            case (int)RocketType.Three:
                break;
            case (int)RocketType.Four:
                break;
            case (int)RocketType.Dry:
                if (Projectile.wet)
                    Projectile.Kill();
                break;
            case (int)RocketType.Wet:
                if (Projectile.wet)
                    Projectile.Kill();
                break;
            case (int)RocketType.Honey:
                if (Projectile.wet)
                    Projectile.Kill();
                break;
            case (int)RocketType.Lava:
                if (Projectile.wet)
                    Projectile.Kill();
                break;
            case (int)RocketType.Cluster1:
                break;
            case (int)RocketType.Cluster2:
                break;
            case (int)RocketType.MiniNuke1:
                break;
            case (int)RocketType.MiniNuke2:
                break;
        }

        if (Time > 30f)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 500, true), out NPC target))
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center + target.velocity) * 16f, 1f / 12f);
        }
        if (Time % 4 == 3)
        {
            ParticleRegistry.SpawnBloomPixelParticle(Back, -Projectile.velocity.RotatedByRandom(.5f) * Main.rand.NextFloat(.35f, .7f), Main.rand.Next(20, 30),
                Main.rand.NextFloat(.3f, .8f), Color.LightCyan, Color.White, null, .7f);
        }

        Lighting.AddLight(Projectile.RotHitbox().Bottom, Color.SkyBlue.ToVector3() * Projectile.Opacity);

        if (Projectile.velocity.X < 0f)
        {
            Projectile.spriteDirection = -1;
            Projectile.rotation = (float)Math.Atan2(-Projectile.velocity.Y, -Projectile.velocity.X) - MathHelper.PiOver2;
        }
        else
        {
            Projectile.spriteDirection = 1;
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.PiOver2;
        }

        Projectile.Opacity = InverseLerp(0f, 10f, Time);

        points.Update(Back);

        Time++;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.Kill();
        return true;
    }
    public override void OnKill(int timeLeft)
    {
        Projectile.tileCollide = false;
        Projectile.Opacity = 0f;
        Projectile.velocity = Vector2.Zero;
        Projectile.timeLeft = 3;

        const int SmallSize = 48;
        const int RegularSize = 128;
        const int BigSize = 200;
        const int HugeSize = 250;

        float boomSize = 3f;
        Vector2 pos = Projectile.Center;
        Point posit = Projectile.Center.ToTileCoordinates();
        switch (State)
        {
            case (int)RocketType.One:
                Projectile.knockBack = 8f;
                Projectile.Resize(RegularSize, RegularSize);
                break;
            case (int)RocketType.Two:
                Projectile.knockBack = 10f;
                Projectile.Resize(RegularSize, RegularSize);
                Kaboom();
                break;
            case (int)RocketType.Three:
                Projectile.knockBack = 10f;
                Projectile.Resize(BigSize, BigSize);
                break;
            case (int)RocketType.Four:
                Projectile.knockBack = 12f;
                Projectile.Resize(BigSize, BigSize);
                boomSize = 5f;
                Kaboom();
                break;

            // Beautiful method names am i right
            case (int)RocketType.Dry:
                Projectile.knockBack = 12f;
                Projectile.Resize(SmallSize, SmallSize);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(posit, 3.5f, DelegateMethods.SpreadDry);

                break;
            case (int)RocketType.Wet:
                Projectile.knockBack = 12f;
                Projectile.Resize(SmallSize, SmallSize);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(posit, 3f, DelegateMethods.SpreadWater);

                break;
            case (int)RocketType.Honey:
                Projectile.knockBack = 12f;
                Projectile.Resize(SmallSize, SmallSize);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(posit, 3f, DelegateMethods.SpreadHoney);

                break;
            case (int)RocketType.Lava:
                Projectile.knockBack = 12f;
                Projectile.Resize(SmallSize, SmallSize);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.Kill_DirtAndFluidProjectiles_RunDelegateMethodPushUpForHalfBricks(posit, 3f, DelegateMethods.SpreadLava);

                break;
            case (int)RocketType.Cluster1:
                Projectile.knockBack = 8f;
                Projectile.Resize(SmallSize, SmallSize);
                break;
            case (int)RocketType.Cluster2:
                Projectile.knockBack = 10f;
                Projectile.Resize(RegularSize, RegularSize);
                Kaboom();
                break;
            case (int)RocketType.MiniNuke1:
                Projectile.knockBack = 12f;
                Projectile.Resize(HugeSize, HugeSize);
                break;
            case (int)RocketType.MiniNuke2:
                Projectile.knockBack = 14f;
                Projectile.Resize(HugeSize, HugeSize);
                boomSize = 7f;
                Kaboom();
                break;
        }

        if (State == (int)RocketType.Cluster1 || State == (int)RocketType.Cluster2)
        {
            float randRot = RandomRotation();
            for (float i = 0f; i < 1f; i += 1f / 6f)
            {
                float rand = randRot + i * MathHelper.TwoPi;
                Vector2 vel = rand.ToRotationVector2() * (4f + Main.rand.NextFloat() * 2f);
                vel += Vector2.UnitY * -1f;
                int cluster = Projectile.NewProj(Projectile.Center, vel,
                    State == (int)RocketType.Cluster1 ? ProjectileID.ClusterSnowmanFragmentsI : ProjectileID.ClusterSnowmanFragmentsII, Projectile.damage / 2, 0f, Projectile.owner);
                Main.projectile[cluster].timeLeft -= Main.rand.Next(30);
            }
        }

        void Kaboom()
        {
            int leftTileX = (int)(pos.X / 16f - boomSize);
            int rightTileX = (int)(pos.X / 16f + boomSize);
            int downTileY = (int)(pos.Y / 16f - boomSize);
            int upTileY = (int)(pos.Y / 16f + boomSize);

            if (leftTileX < 0)
                leftTileX = 0;

            if (rightTileX > Main.maxTilesX)
                rightTileX = Main.maxTilesX;

            if (downTileY < 0)
                downTileY = 0;

            if (upTileY > Main.maxTilesY)
                upTileY = Main.maxTilesY;

            bool wallSplode2 = Projectile.ShouldWallExplode(pos, (int)boomSize, leftTileX, rightTileX, downTileY, upTileY);
            Projectile.ExplodeTiles(pos, (int)boomSize, leftTileX, rightTileX, downTileY, upTileY, wallSplode2);
        }

        for (int i = 0; i < 40; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * (i / 40) + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(1f, 5f);
            int life = Main.rand.Next(20, 40);
            float scale = Main.rand.NextFloat(.3f, .5f);
            Color color = MulticolorLerp(Main.rand.NextFloat(), Color.LightCyan, Color.Cyan, Color.SkyBlue, Color.DeepSkyBlue, Color.BlueViolet);
            switch (Projectile.height)
            {
                case SmallSize:
                    if (i == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                        ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, life / 2, 0f, Vector2.One, 0f, SmallSize, color);
                    }

                    ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale * 70f, color, Main.rand.NextFloat(.4f, 1f), true);
                    ParticleRegistry.SpawnBloomLineParticle(pos, vel * 2, life / 2, scale, color);
                    ParticleRegistry.SpawnCloudParticle(pos, vel, color, Color.DarkBlue, life, scale * 120f, Main.rand.NextFloat(.5f, .8f), 2);
                    break;
                case RegularSize:
                    if (i == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.1f, Pitch = -.1f }, Projectile.Center);
                        ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, life / 2, 0f, Vector2.One, 0f, RegularSize, color);
                    }

                    ParticleRegistry.SpawnGlowParticle(pos, vel * 1.2f, life + 3, scale * 70f * 1.4f, color, Main.rand.NextFloat(.4f, 1f), true);
                    ParticleRegistry.SpawnBloomLineParticle(pos, vel * 2.2f, life / 2, scale * 1.2f, color);
                    ParticleRegistry.SpawnCloudParticle(pos, vel * 1.1f, color, Color.DarkBlue, life, scale * 120f, Main.rand.NextFloat(.5f, .8f), 2);
                    break;
                case BigSize:
                    if (i == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.2f, Pitch = -.15f }, Projectile.Center);
                        ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, life / 2, 0f, Vector2.One, 0f, BigSize, color);
                    }

                    ParticleRegistry.SpawnGlowParticle(pos, vel, life + 5, scale * 70f * 1.85f, color, Main.rand.NextFloat(.4f, 1f), true);
                    ParticleRegistry.SpawnBloomLineParticle(pos, vel * 2.5f, life / 2, scale * 1.4f, color);
                    ParticleRegistry.SpawnCloudParticle(pos, vel * 1.4f, color, Color.DarkBlue, life, scale * 120f, Main.rand.NextFloat(.6f, .8f), 2);
                    break;
                case HugeSize:
                    if (i == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.4f, Pitch = -.2f }, Projectile.Center);
                        ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, life / 2, 0f, Vector2.One, 0f, HugeSize, color);
                    }

                    ParticleRegistry.SpawnGlowParticle(pos, vel * 1.4f, life + 20, scale * 70f * 2f, color, Main.rand.NextFloat(.4f, 1f), true);
                    ParticleRegistry.SpawnBloomLineParticle(pos, vel * 3f, life / 2, scale * 1.6f, color);
                    ParticleRegistry.SpawnCloudParticle(pos, vel * 1.8f, color, Color.DarkBlue, life, scale * 120f, Main.rand.NextFloat(.7f, .9f), 2);
                    break;
            }
        }

        Projectile.Damage();
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hitInfo, int damageDone)
    {
        target.AddBuff(BuffID.Frostburn, 120);
    }
    private Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color bright = Color.LightCyan.Lerp(Color.White, Sin01(Main.GlobalTimeWrappedHourly * 3f));
        Color mid = Color.SkyBlue.Lerp(Color.DeepSkyBlue, Sin01(Main.GlobalTimeWrappedHourly * 1.2f));
        Color dark = Color.DarkBlue.Lerp(Color.DarkViolet.Lerp(Color.DarkBlue, .8f), Cos01(Main.GlobalTimeWrappedHourly));
        return MulticolorLerp(c.X, bright, mid, dark) * Projectile.Opacity;
    }

    public TrailPoints points = new(5);
    public Texture2D Tex => ModContent.Request<Texture2D>(State switch
    {
        (int)RocketType.One => ProjectileID.RocketSnowmanI.GetTerrariaProj(),
        (int)RocketType.Two => ProjectileID.RocketSnowmanII.GetTerrariaProj(),
        (int)RocketType.Three => ProjectileID.RocketSnowmanIII.GetTerrariaProj(),
        (int)RocketType.Four => ProjectileID.RocketSnowmanIV.GetTerrariaProj(),
        (int)RocketType.Dry => ProjectileID.DrySnowmanRocket.GetTerrariaProj(),
        (int)RocketType.Wet => ProjectileID.WetSnowmanRocket.GetTerrariaProj(),
        (int)RocketType.Honey => ProjectileID.HoneySnowmanRocket.GetTerrariaProj(),
        (int)RocketType.Lava => ProjectileID.LavaSnowmanRocket.GetTerrariaProj(),
        (int)RocketType.Cluster1 => ProjectileID.ClusterSnowmanRocketI.GetTerrariaProj(),
        (int)RocketType.Cluster2 => ProjectileID.ClusterSnowmanRocketII.GetTerrariaProj(),
        (int)RocketType.MiniNuke1 => ProjectileID.MiniNukeSnowmanRocketI.GetTerrariaProj(),
        (int)RocketType.MiniNuke2 => ProjectileID.MiniNukeSnowmanRocketII.GetTerrariaProj(),
        _ => ProjectileID.RocketSnowmanI.GetTerrariaProj(),
    }, AssetRequestMode.AsyncLoad).Value;

    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;
            ManagedShader fire = ShaderRegistry.SmoothFlame;
            fire.TrySetParameter("heatInterpolant", 6f * Projectile.Opacity);
            fire.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 1);
            trail.DrawTrail(fire, points.Points, 100, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        Vector2 pos = Projectile.Center - Main.screenPosition;
        Vector2 orig = Tex.Size() / 2;
        Main.spriteBatch.Draw(Tex, pos, null, lightColor * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale,
            Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

        return false;
    }
    public Vector2 Back => Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * (Tex.Width / 2 - 8f);
}
