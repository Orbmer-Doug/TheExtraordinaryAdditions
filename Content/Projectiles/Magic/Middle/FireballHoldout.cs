using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class FireballHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Fireball);
    public override int AssociatedItemID => ModContent.ItemType<Fireball>();
    public override int IntendedProjectileType => ModContent.ProjectileType<FireballHoldout>();

    public override void Defaults()
    {
        Projectile.width = 32;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];

    public static readonly Color[] FireColors =
        [
        new(255, 219, 25),
        new(255, 153, 0),
        new(255, 98, 0),
        new(255, 65, 0),
        ];

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Mouse), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Mouse), interpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.Center = Center + Projectile.velocity * Projectile.width * .5f;
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation);
        if (Projectile.direction == -1)
            Projectile.rotation += MathHelper.Pi;

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Time % Item.useTime == Item.useTime - 1 && HasMana())
        {
            AdditionsSound.FireballShort.Play(Projectile.Center, .6f, 0f, .1f, 30);
            Vector2 dir = Projectile.Center.SafeDirectionTo(Modded.mouseWorld);
            Projectile.NewProj(Projectile.Center, dir * Item.shootSpeed,
                ModContent.ProjectileType<FireballProj>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);

            for (int i = 0; i < 22; i++)
            {
                Vector2 vel = dir.RotatedByRandom(.4f) * Main.rand.NextFloat(2f, 6f);
                int life = Main.rand.Next(30, 40);
                float scale = Main.rand.NextFloat(.5f, .8f);
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel, life, scale, FireColors[Main.rand.Next(FireColors.Length - 1)]);
            }
        }

        if (Time % 4f == 3f)
        {
            ParticleRegistry.SpawnSquishyPixelParticle(Projectile.RandAreaInEntity(), -Vector2.UnitY.RotatedByRandom(.4f) * Main.rand.NextFloat(2f, 6f),
                Main.rand.Next(80, 120), Main.rand.NextFloat(.4f, .8f), FireColors[Main.rand.Next(FireColors.Length - 1)], Color.OrangeRed, 4, true);
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(tex, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() * .5f, 1, Projectile.direction.ToSpriteDirection());
        return false;
    }
}
