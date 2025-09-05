using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class IcyShards : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.IcyShards);

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = 200;
    }

    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public const int WaitTime = 50;
    public override void AI()
    {
        if (Time == 0f)
        {
            Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
            Projectile.rotation = RandomRotation();
        }

        after ??= new(5, () => Projectile.Center);
        Projectile.Opacity = InverseLerp(0f, 12f, Time) * InverseLerp(0f, 10f, Projectile.timeLeft);
        Projectile.scale = InverseLerp(0f, 20f, Projectile.timeLeft);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, 0, 90, 1,
            0f, Projectile.ThisProjectileTexture().Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame), true, -.3f));

        if (Time < WaitTime)
        {
            int dir = (Projectile.identity % 2 == 1).ToDirectionInt();
            Projectile.velocity = Projectile.velocity.RotatedBy(.05f * dir);

            if (Time < (WaitTime - 10))
                Projectile.rotation += dir * InverseLerp(WaitTime, 0f, Time) * .5f;
            else
                Projectile.rotation = Projectile.rotation.SmoothAngleLerp(Projectile.AngleTo(Modded.mouseWorld) + MathHelper.PiOver2, .2f, .4f);
        }
        else if (Time == WaitTime)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.RotHitbox().Bottom, Vector2.Zero, 10, Projectile.rotation - MathHelper.PiOver2, new(.3f, 1f), 0f, 40f, Color.SlateBlue);
            if (this.RunLocal())
            {
                Projectile.velocity = Projectile.SafeDirectionTo(Modded.mouseWorld) * 15f;
                this.Sync();
            }
        }
        else if (Time > WaitTime)
        {
            Projectile.FacingUp();
        }

        if (Projectile.timeLeft < 20)
            Projectile.velocity *= .9f;

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 20; i++)
            ParticleRegistry.SpawnDustParticle(Projectile.RotHitbox().RandomPoint(), Projectile.velocity * Main.rand.NextFloat(.1f, .2f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f), Color.LightCyan);
        SoundID.Item49.Play(Projectile.Center, .7f, 0f, .1f, null, 20);
    }

    public override bool? CanDamage() => Time > WaitTime ? null : false;

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        after?.DrawFancyAfterimages(tex, [Color.DarkSlateBlue], .5f);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, lightColor * Projectile.Opacity, Projectile.rotation, frame.Size() / 2, Projectile.scale);
        return false;
    }
}
