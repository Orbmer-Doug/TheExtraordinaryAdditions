using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class HellfireHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TomeOfHellfire);

    public ref float Time => ref Projectile.ai[0];
    public ref float Radius => ref Projectile.ai[1];

    private const int Wait = 60;

    public override void Defaults()
    {
        Projectile.width = 28;
        Projectile.height = 30;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 40f, Projectile.Distance(Owner.Additions().mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Additions().mouseWorld), interpolant);
            if (Projectile.oldVelocity != Projectile.velocity)
                this.Sync();
        }

        Projectile.Center = Center + Projectile.velocity * Projectile.width;
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Owner.SetFrontHandBetter(0, Projectile.rotation);

        Projectile.Opacity = InverseLerp(0f, 14f, Time);
        Radius++;

        if (Time % Wait == Wait - 1f && TryUseMana())
        {
            Radius = 1;

            AdditionsSound.WaterSpell.Play(Projectile.Center, .8f, -.1f, .16f, 10);

            if (this.RunLocal())
            {
                for (int i = 0; i < Main.rand.Next(3, 5); i++)
                {
                    Vector2 newVelocity = (Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * 15f).RotatedByRandom(Main.rand.NextFloat(.24f, .4f)) * Main.rand.NextFloat(.7f, 1.05f);

                    int type = ModContent.ProjectileType<HellishNapalm>();
                    Projectile.NewProj(Projectile.Center, newVelocity, type, Projectile.damage, Projectile.knockBack);

                    for (int j = 0; j < 6; j++)
                        ParticleRegistry.SpawnDustParticle(Projectile.Center, newVelocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.6f, 1.2f), Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .5f), Color.OrangeRed, .1f, false, true);
                }
            }
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        SpriteEffects effects = FixedDirection();

        const int amount = 10;
        float backglowArea = Convert01To010(Radius / Wait) * 5f;
        Color backglowColor = Color.OrangeRed;
        Vector2 origin = tex.Size() * 0.5f;
        for (int i = 0; i < amount; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * backglowArea;
            Color glowAlpha = Projectile.GetAlpha(backglowColor * Projectile.Opacity) with { A = 0 };
            Main.spriteBatch.DrawBetter(tex, Projectile.Center + drawOffset, null, glowAlpha * 0.95f, Projectile.rotation, origin, Projectile.scale, effects);
        }

        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, effects);
        return false;
    }
}