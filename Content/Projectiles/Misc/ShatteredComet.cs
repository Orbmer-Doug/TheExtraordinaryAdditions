using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.World.Subworlds;
using TheExtraordinaryAdditions.Core.Graphics.ScreenEffects;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Misc;

public class ShatteredComet : ModProjectile
{
    public ref float Heat => ref Projectile.ai[0];
    public ref float State => ref Projectile.ai[1];
    public static readonly int HeatTime = SecondsToFrames(3);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 22;
    }

    public override void SetDefaults()
    {
        Projectile.extraUpdates = 0;
        Projectile.width = Projectile.height = 72;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;

        Projectile.timeLeft = SecondsToFrames(30);

        Projectile.netImportant = true;
    }

    public override bool? CanDamage()
    {
        return false;
    }
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.LightCyan.ToVector3() * 1.7f);
        if (Heat > 0)
            Heat--;
        switch (State)
        {
            case 0:
                ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center - Projectile.velocity,
                    -Projectile.velocity.RotatedByRandom(.13f) * Main.rand.NextFloat(.3f, .76f), 40, Main.rand.NextFloat(.3f, .6f), Color.SkyBlue);

                Projectile.rotation += .4f;
                break;
            case 1:
                CheckForPlayer();
                Stick();
                if (Heat > 0f)
                {
                    Vector2 pos = Projectile.RandAreaInEntity();
                    Vector2 vel = RandomVelocity(2f, 5f, 8f);
                    Color color = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, Main.rand.NextFloat(.3f, .9f));
                    float scale = Main.rand.NextFloat(.3f, .5f);
                    int life = Main.rand.Next(17, 30);
                    ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, color, .876f);
                    ParticleRegistry.SpawnGlowParticle(pos, vel * 2, life, scale, color * .8f);
                }
                break;
        }
    }
    private void CheckForPlayer()
    {
        Player player = Main.player[Projectile.owner];
        if (this.RunLocal())
        {
            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                CloudedCrater.ClientWorldDataTag = CloudedCrater.SafeWorldDataToTag("Client", false);
                if (this.RunLocal())
                {
                    if (SubworldSystem.IsActive<CloudedCrater>())
                    {
                        // Clear out any active screenshake shaders because the projectile cant deactivate it in time when exiting
                        //if (Main.netMode != NetmodeID.Server && ShaderRegistry.ScreenShakeShader.IsActive())
                        //  ShaderRegistry.ScreenShakeShader.Deactivate();

                        SubworldSystem.Exit();
                    }
                    else
                        SubworldSystem.Enter<CloudedCrater>();
                }
            }
        }
    }
    private void Stick()
    {
        try
        {
            int num3 = (int)(Projectile.position.X / 16f) - 1;
            int num4 = (int)((Projectile.position.X + Projectile.width) / 16f) + 2;
            int num5 = (int)(Projectile.position.Y / 16f) - 1;
            int num6 = (int)((Projectile.position.Y + Projectile.height) / 16f) + 2;
            if (num3 < 0)
            {
                num3 = 0;
            }
            if (num4 > Main.maxTilesX)
            {
                num4 = Main.maxTilesX;
            }
            if (num5 < 0)
            {
                num5 = 0;
            }
            if (num6 > Main.maxTilesY)
            {
                num6 = Main.maxTilesY;
            }
            Vector2 vector = default;
            for (int j = num3; j < num4; j++)
            {
                for (int k = num5; k < num6; k++)
                {
                    if (Main.tile[j, k] == null || !Main.tile[j, k].HasActuator || !Main.tileSolid[Main.tile[j, k].TileType] || Main.tileSolidTop[Main.tile[j, k].TileType])
                    {
                        continue;
                    }
                    vector.X = j * 16;
                    vector.Y = k * 16;
                    if (!(Projectile.position.X + Projectile.width - 4f > vector.X) || !(Projectile.position.X + 4f < vector.X + 16f) || !(Projectile.position.Y + Projectile.height - 4f > vector.Y) || !(Projectile.position.Y + 4f < vector.Y + 16f))
                    {
                        continue;
                    }
                    Projectile.velocity.X = 0f;
                    Projectile.velocity.Y = -0.2f;
                }
            }
        }
        catch
        {
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Heat = HeatTime;
        SoundEngine.PlaySound(SoundID.Item89, Projectile.Center);
        Projectile.Center += oldVelocity * 2f;

        State = 1f;

        for (int i = 0; i < 9; i++)
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 10, RandomRotation(), new(Main.rand.NextFloat(.47f, .77f), 1f), 0f, .6f, new(219, 194, 229));

        for (int i = 0; i < 50; i++)
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(50f, 50f);
            Vector2 vel = Main.rand.NextVector2CircularEdge(5f, 5f);
            float scale = Main.rand.NextFloat(.4f, .7f);
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, 50, .4f, Color.Cyan);
        }
        for (int i = 0; i < 24; i++)
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(10f, 10f);
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.46f) * Main.rand.NextFloat(1.4f, 1.9f);

            ParticleRegistry.SpawnMistParticle(pos, vel, Main.rand.NextFloat(.2f, .5f), Color.Cyan, Color.Transparent, 167);
            ParticleRegistry.SpawnSparkParticle(pos, vel, 80, Main.rand.NextFloat(1.1f, 1.6f), Color.White);
        }

        Projectile.velocity *= 0;
        return false;
    }
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (State == 1f)
        {
            behindNPCsAndTiles.Add(index);
        }
        else
        {
            Projectile.hide = false;
        }
    }
    public float WidthFunction(float completionRatio) => Projectile.width * 1.4f;

    public Color ColorFunction(float completionRatio)
    {
        float colorInterpolant = MathF.Pow(Math.Abs(MathF.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)), 3f) * 0.5f;
        return Color.Lerp(Color.AntiqueWhite, Color.LightCyan, colorInterpolant) * Projectile.Opacity * 0.3f;
    }
    public float TrailWidthFunction(float completionRatio) => MathHelper.SmoothStep(Projectile.width * 2, 8f, completionRatio) * Projectile.scale;

    public static Color TrailColorFunction(float completionRatio)
    {
        float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
        Color startingColor = Color.Lerp(Color.LightSkyBlue, Color.White, 0.4f);
        Color middleColor = Color.Lerp(Color.DarkBlue, Color.Cyan, 0.2f);
        Color endColor = Color.Lerp(Color.DarkCyan, Color.Cyan, 0.67f);
        return MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
    }
    public void putInDRAW()
    {
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Draw base texture
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        float heatInterpolant = Utils.GetLerpValue(0f, HeatTime, Heat, true);
        Projectile.DrawProjectileBackglow(ColorSwap(Color.Cyan, Color.LightSkyBlue, 5f), AperiodicSin(Main.GlobalTimeWrappedHourly * .7f) * 4.5f, 180, 14);
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, direction, 0);
        if (heatInterpolant > 0f)
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Cyan * heatInterpolant, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.1f, direction, 0);

        return false;
    }
}