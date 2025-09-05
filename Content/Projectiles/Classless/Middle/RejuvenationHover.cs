using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class RejuvenationHover : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RejuvenationArtifact);

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 40;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public Player Owner => Main.player[Projectile.owner];
    public List<Player> Players => PlayerTargeting.ClosestPlayers(new(Projectile.Center, 400));
    public ref float Time => ref Projectile.ai[0];
    public ref float Offset => ref Projectile.ai[1];

    public override void OnKill(int timeLeft)
    {
        foreach (Player player in Players)
            player.GetModPlayer<RejuvenationPlayer>().HealWait = 0;
    }

    public static readonly int timeForHeal = SecondsToFrames(7);
    public override void AI()
    {
        if (!Owner.Available() || Owner.Additions().HealingArtifact == false)
        {
            Projectile.Kill();
            return;
        }
        else
            Projectile.timeLeft = 2;

        foreach (Player player in Players)
        {
            if (player == null || player.dead || player.active == false)
                continue;

            ref int wait = ref player.GetModPlayer<RejuvenationPlayer>().HealWait;
            if (wait < 40f)
                wait++;

            if (Time % timeForHeal == timeForHeal - 1 && wait >= 40f)
            {
                if (Projectile.soundDelay <= 0)
                {
                    AdditionsSound.etherealSwordSwoosh.Play(Projectile.Center, 0f, .2f);
                    Projectile.soundDelay = 1;
                }

                player.Heal(10);
                ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, player.velocity,
                    Main.rand.Next(30, 40), 0f, new(1f), 0f, .1f, Color.DarkRed);

                for (int i = 0; i < 24; i++)
                {
                    ParticleRegistry.SpawnSparkleParticle(player.Center, -Vector2.UnitY.RotatedByRandom(.4f) * Main.rand.NextFloat(1f, 7f),
                        Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, .7f), Color.Red, Color.Crimson, 1.4f);

                    ParticleRegistry.SpawnGlowParticle(player.RotHitbox().RandomPoint(), Vector2.UnitY * -Main.rand.NextFloat(2f, 7f),
                        Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f), Color.Red, .5f);
                }
            }
            player.GetModPlayer<RejuvenationPlayer>().BeingHealed = true;
        }

        Projectile.rotation = Projectile.rotation.AngleLerp(Owner.velocity.X * 0.03f, 0.1f);
        Projectile.Center = Owner.MountedCenter + new Vector2(0f, -100f)
             + (100f * new Vector2(MathF.Cos(Offset) * 2, MathF.Sin(Offset * 2)));

        Offset = (Offset + .015f) % MathHelper.TwoPi;

        after ??= new(6, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0));

        if (Main.rand.NextBool(12))
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularLimited(140f, 140f, .5f, 1f);
            Vector2 vel = Main.rand.NextVector2CircularLimited(3f, 3f, .2f, 1f);
            int life = Main.rand.Next(40, 90);
            float scale = Main.rand.NextFloat(.4f, .8f);
            Color col = Color.Red.Lerp(Color.Crimson, Main.rand.NextFloat(.4f, .8f));
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, col, Color.Crimson, Projectile.Center, 1.4f, 5);
        }

        Lighting.AddLight(Projectile.Center, Color.IndianRed.ToVector3() * Projectile.Opacity);
        Time++;
    }

    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    private float WidthFunct(float c)
    {
        return Projectile.height * .2f * (QuadraticBump(c) + .5f);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        foreach (Player player in Main.player)
        {
            if (player == null || player.dead || !player.active)
                continue;

            void draw()
            {
                float completion = player.GetModPlayer<RejuvenationPlayer>().Completion;
                ManagedShader prim = ShaderRegistry.SpecialLightningTrail;
                prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoise), 1);

                ManualTrailPoints points = new(20);
                points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.SafeDirectionTo(player.Center) * (Projectile.Center.Distance(player.Center) * completion), 20));

                OptimizedPrimitiveTrail line = new(WidthFunct,
                    (c, pos) => MulticolorLerp(c.X + Sin01(Main.GlobalTimeWrappedHourly), Color.Red, Color.IndianRed, Color.PaleVioletRed, Color.Red * 1.8f) * completion, null, 20);
                line.DrawTrail(prim, points.Points, 90);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }

        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;
        Color col = Lighting.GetColor(Projectile.Center.ToTileCoordinates()) * Projectile.Opacity;
        float heal = InverseLerp(0f, timeForHeal, Time % timeForHeal);
        Projectile.DrawProjectileBackglow(Color.Crimson * heal, heal * 4f);
        after?.DrawFancyAfterimages(tex, [Color.Crimson]);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, col, Projectile.rotation, orig, Projectile.scale, 0);
        return false;
    }
}

public class RejuvenationPlayer : ModPlayer
{
    public int HealWait;
    public float Completion => InverseLerp(0f, 40f, HealWait);
    public bool BeingHealed;
    public override void ResetEffects()
    {
        BeingHealed = false;
    }
    public override void UpdateDead()
    {
        HealWait = 0;
        BeingHealed = false;
    }
    public override void PostUpdateMiscEffects()
    {
        if (BeingHealed)
            Player.lifeMagnet = true;
    }
    public override void UpdateLifeRegen()
    {
        if (BeingHealed)
            Player.lifeRegenTime += 50;
    }
}