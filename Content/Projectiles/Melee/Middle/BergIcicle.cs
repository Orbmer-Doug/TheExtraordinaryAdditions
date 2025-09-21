using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class BergIcicle : ModProjectile
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
        Projectile.timeLeft = 400;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Hit
    {
        get => Projectile.ai[1] == 1;
        set => Projectile.ai[1] = value.ToInt();
    }
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
            Projectile.rotation += Projectile.velocity.X.NonZeroSign() * Utils.Remap(Time, 0f, WaitTime, .28f, 0f);
            Projectile.velocity *= .94f;
        }
        else
        {
            if (!Hit)
            {
                if (Utility.FindProjectile(out Projectile p, ModContent.ProjectileType<BergcrusherSwing>(), Projectile.owner))
                {
                    BergcrusherSwing swing = p.As<BergcrusherSwing>();
                    if (swing.Rect().Intersects(Projectile.RotHitbox()) && swing.AngularVelocity > .03f && swing.Time > 5)
                    {
                        Hit = true;
                        Projectile.velocity = Projectile.Center.SafeDirectionTo(Modded.mouseWorld) * 14f;
                        this.Sync();
                    }
                }
            }
            if (Hit)
            {
                Projectile.FacingUp();
                if (Main.rand.NextBool(3))
                    ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * .02f, Main.rand.Next(30, 40), Main.rand.NextFloat(.3f, .5f), Color.SlateBlue, Color.LightBlue, null);
            }
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

    public override bool? CanDamage() => Hit == true ? null : false;

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