using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;

public class ConsumingVoid : ModProjectile, ILocalizedModType, IModType
{
    public const float HueShiftAcrossAfterimages = 0.2f;
    public ref float Time => ref Projectile.ai[1];

    public override string Texture => AssetRegistry.Invis;
    public class EnergySuckParticle
    {
        public int Time;

        public int Lifetime;

        public float Opacity;

        public Color DrawColor;

        public Vector2 Center;

        public Vector2 Velocity;

        public void Update(Vector2 destination)
        {
            Center += Velocity;
            Velocity = Vector2.Lerp(Velocity, (destination - Center) * 0.1f, 0.04f);
            Time++;
            Opacity = InverseLerp(0f, 205f, Center.Distance(destination)) * 0.56f;
        }
    }

    public List<EnergySuckParticle> Particles = [];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;

        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 35;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 60;
        Projectile.scale = 0.01f;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.MaxUpdates = 4;
        Projectile.timeLeft = Projectile.MaxUpdates * 120;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 16;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
    }

    public NPC target;
    public override void AI()
    {
        Time++;

        if (target.CanHomeInto())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * (26f / Projectile.MaxUpdates), .02f);
        }
        else
            target = NPCTargeting.GetStrongestNPC(new(Projectile.Center, 1000, false, true));

        Projectile.rotation += Projectile.velocity.X * 0.04f;
        Vector2 center = Projectile.Center;
        Lighting.AddLight(center, Color.DarkViolet.ToVector3() * 0.9f);
        Projectile.Opacity = InverseLerp(0f, 4f * Projectile.MaxUpdates, Time);
        Projectile.scale = Utils.Remap(Time, 0f, Projectile.MaxUpdates * 15f, 0.01f, 1.5f, true) * Utils.GetLerpValue(0f, Projectile.MaxUpdates * 16f, Projectile.timeLeft, true);
        Projectile.ExpandHitboxBy((int)(Projectile.scale * 62f));
       
        if (Time % 3f == 0f)
        {
            // Create suck energy particles.
            Vector2 energySpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Projectile.width * Main.rand.NextFloat(0.97f, 2.1f);
            Vector2 energyVelocity = (Projectile.Center - energySpawnPosition).RotatedBy(MathHelper.PiOver2) * 0.037f;
            Particles.Add(new()
            {
                Center = energySpawnPosition,
                Velocity = energyVelocity,
                Opacity = 1f,
                DrawColor = Color.Lerp(Color.Lerp(Color.BlueViolet, Color.Violet, Main.rand.NextFloat()), Color.White, Main.rand.NextFloat(.3f, .9f)),
                Lifetime = 30
            });
        }

        // Update all particles.
        Particles.RemoveAll(p => p.Time >= p.Lifetime);
        for (int i = 0; i < Particles.Count; i++)
        {
            var p = Particles[i];
            p.Update(Projectile.Center);
        }
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        behindProjectiles.Add(index);
    }

    public float WidthFunct(float c)
    {
        return Projectile.width * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        float velFade = Utils.GetLerpValue(2f, 10f, Projectile.velocity.Length(), true);
        return MulticolorLerp(MathHelper.Clamp(AperiodicSin(Time * .08f) * .5f, 0f, 1f), Color.Violet, Color.BlueViolet,
            Color.MediumPurple, Color.Purple, Color.DarkViolet) * Projectile.Opacity * MathHelper.SmoothStep(1f, 0f, c.X) * Utils.GetLerpValue(0.04f, 0.2f, c.X, true) * velFade;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawSwirl();
        Main.spriteBatch.SetBlendState(BlendState.Additive);
        DrawParticles();
        Main.spriteBatch.ResetBlendState();
        return false;
    }

    public void DrawSwirl()
    {
        Texture2D worleyNoise = AssetRegistry.GetTexture(AdditionsTexture.Cosmos);
        float spinRotation = Main.GlobalTimeWrappedHourly * 4.2f;

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
        ShaderRegistry.VortexShader.Render();
        const int amt = 30;
        for (int i = 0; i < amt; i++)
        {
            float completion = InverseLerp(0f, amt, i);
            Vector2 scale = MathHelper.Lerp(1f, 0f, completion) * Projectile.Size / worleyNoise.Size() * 2f;
            Vector2 drawOffset = Vector2.UnitY * Projectile.scale * 6f;
            Color color = MulticolorLerp(completion, Color.Violet, Color.BlueViolet, Color.DarkViolet) * Projectile.Opacity;
            Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
            //Main.spriteBatch.Draw(worleyNoise, drawPosition - drawOffset, null, color, -spinRotation, worleyNoise.Size() * 0.5f, scale, 0, 0f);
            Main.spriteBatch.Draw(worleyNoise, drawPosition , null, color, spinRotation + (completion * MathHelper.Pi), worleyNoise.Size() * 0.5f, scale, 0, 0f);
        }
        Main.spriteBatch.ExitShaderRegion();
    }

    public void DrawParticles()
    {
        Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
        float energyBaseScale = 1f;

        // Draw energy particles that get sucked in.
        foreach (EnergySuckParticle particle in Particles)
        {
            float squish = 0.21f;
            float rotation = particle.Velocity.ToRotation();
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 scale = new(energyBaseScale - energyBaseScale * squish * 0.3f, energyBaseScale * squish);
            Vector2 drawPosition = particle.Center - Main.screenPosition;

            Main.spriteBatch.Draw(bloom, drawPosition, null, particle.DrawColor * particle.Opacity * Projectile.Opacity * 0.8f, rotation, origin, scale * 0.32f, 0, 0f);
            Main.spriteBatch.Draw(bloom, drawPosition, null, particle.DrawColor * particle.Opacity * Projectile.Opacity, rotation, origin, scale * 0.27f, 0, 0f);
            Main.spriteBatch.Draw(bloom, drawPosition, null, Color.White * particle.Opacity * Projectile.Opacity * 0.9f, rotation, origin, scale * 0.24f, 0, 0f);
        }
    }
}
