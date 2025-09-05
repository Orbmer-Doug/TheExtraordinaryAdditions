using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class StarWater : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StarWater);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();

    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Offset);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Offset = reader.ReadVector2();
    }

    public override void SetDefaults()
    {
        Projectile.width = 54;
        Projectile.height = 14;
        Projectile.timeLeft = 400;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];

    public bool Released
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public float Completion => Animators.MakePoly(4).InFunction(InverseLerp(0f, 40f, Time));

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return Color.Lerp(Color.DarkSlateBlue, Color.MidnightBlue, completionRatio.X) * UltrasmoothStep(1f, 0, completionRatio.X);
    }

    internal float WidthFunction(float completionRatio)
    {
        return Projectile.width * 0.45f * MathHelper.SmoothStep(0.1f, 1f, Utils.GetLerpValue(0f, 0.3f, completionRatio, true));
    }

    public override void AI()
    {
        if (Owner == null || !Owner.active || Owner.HeldItem.type != ModContent.ItemType<StarlessSea>() || Main.bloodMoon)
        {
            Projectile.Kill();
            return;
        }
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 20);

        Lighting.AddLight(Projectile.Center, Color.DarkViolet.ToVector3() * .9f * Projectile.Opacity);

        if (Released)
        {
            cache.Update(Projectile.RotHitbox().Right);

            if (VelLength < 128f)
                Projectile.velocity *= 1.095f;
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        else
        {
            Projectile.timeLeft = 400;
            Projectile.Center = Vector2.Lerp(Projectile.Center, Vector2.Lerp(Owner.Center, Owner.Center + new Vector2(0f, -300f) + Offset, Completion), .75f);
            Projectile.Opacity = Completion;
            if (this.RunLocal())
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(ModdedOwner.mouseWorld + Offset) * 1.1f, .4f);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
        }

        Projectile.FacingRight();

        if (Time % 7 == 6)
        {
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(),
                Main.rand.NextVector2Circular(3f, 3f) + -Projectile.velocity.ClampLength(0f, 2f),
                Main.rand.Next(30, 40), Main.rand.NextFloat(.2f, .6f) * Projectile.Opacity, Color.DarkSlateBlue, Color.White, null, 1.2f);
        }


        Time++;
    }

    public override bool ShouldUpdatePosition() => Released;
    public override bool? CanDamage() => Released ? null : false;
    public float VelLength => Projectile.velocity.Length();
    public override bool OnTileCollide(Vector2 velocityChange)
    {
        if (VelLength > 3f)
        {
            int amt = (int)MathHelper.Clamp(VelLength, 10f, 32f) / 2;
            for (int i = 0; i < amt; i++)
            {
                Vector2 vel = -Projectile.velocity.RotatedByRandom(Main.rand.NextFloat(.2f, .45f)) * Main.rand.NextFloat(.4f, .85f);
                ParticleRegistry.SpawnBloomLineParticle(Projectile.RotHitbox().RandomPoint(), vel, Main.rand.Next(20, 50), Main.rand.NextFloat(.5f, .7f), Color.MediumSlateBlue);
            }
        }

        Projectile.Kill();
        return false;
    }
    
    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Shatter with { Volume = .35f, Pitch = -.2f });
        ParticleRegistry.SpawnTwinkleParticle(Projectile.Center, Vector2.Zero, 20, new(1.3f), Color.SlateBlue, 6);

        if (this.RunLocal() && Owner.HeldItem.type == ModContent.ItemType<StarlessSea>())
        {
            int amt = (int)MathHelper.Clamp(VelLength, 1f, 14f);
            for (int i = 0; i < amt; i++)
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = -Projectile.velocity.RotatedByRandom(MathHelper.TwoPi);
                vel *= Main.rand.NextFloat(.2f, .3f);
                int proj = ModContent.ProjectileType<StarWaterBreak>();
                Projectile.NewProj(pos, vel, proj, Projectile.damage / (amt + 10), Projectile.knockBack, Projectile.owner, 0f);
            }
        }
    }
    
    public TrailPoints cache = new(6);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;
            if (Released)
            {
                ManagedShader shader = ShaderRegistry.WaterCurrent;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WaterNoise), 1);
                trail.DrawTrail(shader, cache.Points, 30);
            }
        }

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }

}