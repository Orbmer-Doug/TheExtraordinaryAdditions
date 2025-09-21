using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class ExsanguinationLance : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ExsanguinationLance);

    public Vector2 Offset;
    public override void SendAI(BinaryWriter writer)
    {
        writer.WriteVector2(Offset);
    }
    public override void ReceiveAI(BinaryReader reader)
    {
        Offset = reader.ReadVector2();
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Free
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool Released
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public int PlayerIndex
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }
    public ref float ReleaseTime => ref Projectile.Additions().ExtraAI[1];
    public ref float Rot => ref Projectile.Additions().ExtraAI[2];
    public bool HitPlayer
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }

    public Vector2 CurrentOffset => PolarVector(Owner.width * .7f, Owner.AdditionsInfo().ExtraAI[1] + Rot);
    public Vector2 Destination => Owner.Center + CurrentOffset;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
    }

    public override void SetDefaults()
    {
        Projectile.width = 120;
        Projectile.height = 16;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        CooldownSlot = ImmunityCooldownID.Bosses;
        Projectile.tileCollide = false;
    }

    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, 24);
        
        if (!Free)
        {
            if (Owner != null && Owner.active)
            {
                Projectile.timeLeft = 30;
            }
            else
            {
                Projectile.Kill();
                return;
            }
        }

        Projectile.Opacity = InverseLerp(0f, 25f, Time) * InverseLerp(0f, 20f, Projectile.timeLeft);
        Lighting.AddLight(Projectile.Center, Color.DarkRed.ToVector3() * 1.5f * Projectile.Opacity);
        Projectile.FacingRight();
        if (Free)
        {
            if (Projectile.velocity.Length() < 100f)
                Projectile.velocity *= 1.045f;
        }

        if (HitPlayer)
        {
            if (PlayerIndex >= 0 && PlayerIndex < Main.maxPlayers)
            {
                Player target = Main.player[PlayerIndex];
                if (target == null)
                {
                    HitPlayer = Released = false;
                }
                else
                {
                    Projectile.position = target.position + Offset;
                    if (Projectile.position != Projectile.oldPosition)
                        this.Sync();
                }
            }
        }
        else if (!Free)
        {
            if (!Released)
            {
                ReleaseTime = 0f;
                Projectile.velocity = Projectile.SafeDirectionTo(Target.Center + CurrentOffset);
                Projectile.Center = Vector2.SmoothStep(Projectile.Center, Destination, .2f);
                this.Sync();
            }
            else
            {
                if (ReleaseTime == 0f)
                {
                    Vector2 pos = Projectile.RotHitbox().Left;
                    ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.5f, 1f) * 90f, Projectile.velocity, 60, Color.Crimson, Projectile.velocity.ToRotation());
                    for (int i = 0; i < 14; i++)
                    {
                        ParticleRegistry.SpawnBloomLineParticle(pos, Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(5f, 20f), Main.rand.Next(30, 60),
                            Main.rand.NextFloat(.3f, .6f), Color.DarkRed);

                        ParticleRegistry.SpawnBloomPixelParticle(pos, Projectile.velocity.RotatedByRandom(.67f) * Main.rand.NextFloat(3f, 14f),
                            Main.rand.Next(20, 70), Main.rand.NextFloat(.4f, .8f), Color.Crimson, Color.DarkRed, null, 1.5f, 3);
                    }

                    Projectile.velocity *= 20f;
                }

                if (Projectile.velocity.Length() < 100f)
                    Projectile.velocity *= 1.045f;

                ReleaseTime++;
            }
        }

        cache.Update(Projectile.RotHitbox().Right);

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().Left, Projectile.BaseRotHitbox().Right, Projectile.height - 5f);
    }

    public override bool CanHitPlayer(Player target) => !Free && Released && !HitPlayer || Free;
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        StygainHeart.ApplyLifesteal(this, target, info.Damage);
        ParticleRegistry.SpawnBloodStreakParticle(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(.25f) * 2f,
            30, Main.rand.NextFloat(.5f, .8f), Color.DarkRed);

        if (!HitPlayer)
        {
            if (!Free)
            {
                if (Projectile.timeLeft > 50)
                    Projectile.timeLeft = 50;
            }
            float scale = Main.rand.NextFloat(.4f, .6f);
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 20 + RandomRotation()).ToRotationVector2() * 10f * Main.rand.NextBool().ToDirectionInt();
                ParticleRegistry.SpawnDustParticle(Projectile.RotHitbox().Right, vel, 40, scale, Color.Crimson, .06f, false, true, true);
            }

            PlayerIndex = target.whoAmI;
            Offset = Projectile.position - target.position;
            Offset -= Projectile.velocity;
            HitPlayer = true;
            this.Sync();
        }
    }
    
    public TrailPoints cache = new(24);
    public OptimizedPrimitiveTrail trail;
    public float WidthFunct(float c) => Projectile.height / 2 * GetLerpBump(0f, .75f, 1f, .25f, c) * Projectile.Opacity;
    public Color ColorFunct(SystemVector2 c, Vector2 position) => MulticolorLerp(c.X + Sin01(Main.GlobalTimeWrappedHourly * 2f), Color.Crimson, Color.Red, Color.DarkRed) * MathHelper.SmoothStep(1f, 0f, c.X);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1);
            trail.DrawTrail(shader, cache.Points, 70);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D tex = Projectile.ThisProjectileTexture();
        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, tex.Size() / 2, Projectile.scale, 0, 0f);
        return false;
    }
}
