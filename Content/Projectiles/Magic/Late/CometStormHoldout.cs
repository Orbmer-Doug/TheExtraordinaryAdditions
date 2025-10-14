using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class CometStormHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CometStormHoldout);

    public ref float Time => ref Projectile.ai[0];

    public override void Defaults()
    {
        Projectile.width = 102;
        Projectile.height = 166;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.Opacity = InverseLerp(0f, 16f, Time);
        Projectile.Center = Center + PolarVector(Animators.MakePoly(2.8f).OutFunction.Evaluate(Projectile.width * .1f, Projectile.width * .4f + 27f, Projectile.Opacity), Projectile.velocity.ToRotation());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(63.4f);
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation());

        Vector2 top = Projectile.Center + PolarVector(Projectile.height / 2, Projectile.velocity.ToRotation()) + PolarVector(16f, Projectile.velocity.ToRotation() - MathHelper.PiOver2);

        // Release comets
        if (Time % 7f == 6f)
        {
            SoundID.Item88.Play(top, .8f, 0f, .1f);

            if (this.RunLocal() && TryUseMana())
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = Owner.Center - new Vector2(Main.rand.NextFloat(-Main.LogicCheckScreenWidth / 4, Main.LogicCheckScreenWidth / 4), 800f + (Main.rand.NextFloat(20f, 50) * i));
                    Vector2 velocity = Mouse - position;

                    if (velocity.Y < 0f)
                        velocity.Y *= -1f;

                    if (velocity.Y < 20f)
                        velocity.Y = 20f;

                    velocity.Normalize();
                    velocity *= 22;
                    velocity.Y += Main.rand.NextFloat(-.1f, .1f);
                    velocity.X += Main.rand.NextFloat(-2f, 2f);
                    Projectile.NewProj(position, velocity, ModContent.ProjectileType<Comet>(), Projectile.damage, 0f, Owner.whoAmI);
                }
            }
            ParticleRegistry.SpawnPulseRingParticle(top, Vector2.Zero, 20, RandomRotation(), new(Main.rand.NextFloat(.3f, .7f), 1f), 0f, Main.rand.NextFloat(120f, 180f), Color.Cyan);
        }

        ParticleRegistry.SpawnBloomPixelParticle(top + Main.rand.NextVector2Circular(100f, 100f), RandomVelocity(2f, 4f, 10f), Main.rand.Next(18, 24),
            Main.rand.NextFloat(.4f, .8f), Color.Cyan, Color.DarkCyan * .6f, top, 1.2f, 8);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
        return false;
    }
}
