using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class EpidemicHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Epidemic);
    public override int IntendedProjectileType => ModContent.ProjectileType<EpidemicHoldout>();
    public override int AssociatedItemID => ModContent.ItemType<Epidemic>();

    public ref float Time => ref Projectile.ai[0];

    public ref float LeftCounter => ref Projectile.ai[1];
    public ref float RightCounter => ref Projectile.ai[2];
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 8;
    }

    public override void Defaults()
    {
        Projectile.width = 92;
        Projectile.height = 76;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void SafeAI()
    {
        Projectile.SetAnimation(8, 6);

        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 40f, Projectile.Distance(Modded.mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Modded.mouseWorld), interpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + Projectile.velocity * Projectile.width * .4f;
        Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(0, Projectile.rotation);

        if (this.RunLocal() && Modded.MouseRight.Current)
        {
            int spear = ModContent.ProjectileType<EpidemicSpear>();
            if (Owner.CountOwnerProjectiles(spear) <= 0 && Owner.HeldItem.CheckManaBetter(Owner, 15, true))
            {
                Projectile.NewProj(Projectile.Center, Vector2.Zero, spear, Projectile.damage * 3, Projectile.knockBack, Projectile.owner, 0f, Projectile.whoAmI);
            }

            if (RightCounter % 2f == 1f)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(80f, 80f);
                Vector2 vel = pos.SafeDirectionTo(pos);
                int life = Main.rand.Next(20, 30);
                float size = Main.rand.NextFloat(.3f, .6f);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, size, Color.LawnGreen, Color.OliveDrab, Projectile.Center, 1f, 7);
            }

            if (RightCounter < EpidemicSpear.TotalCharge)
                RightCounter++;
        }
        else if (RightCounter > 0f)
            RightCounter = 0f;

        if (Modded.MouseLeft.Current)
        {
            float wait = 60f * Owner.GetTotalAttackSpeed<MagicDamageClass>();
            if (this.RunLocal() && LeftCounter % wait == wait - 1f && Owner.HeldItem.CheckManaBetter(Owner, 8, true))
            {
                AdditionsSound.WaterSpell.Play(Projectile.Center, 1f, 0f, 0f, 0);
                Projectile.NewProj(Projectile.Center, Projectile.SafeDirectionTo(Modded.mouseWorld) * 15f, ModContent.ProjectileType<EpidemicLob>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            LeftCounter++;
        }
        else
            LeftCounter = 0f;

        Time++;
    }

    private void DrawRing(SpriteBatch sb, Vector2 pos, float w, float h, float rotation, float prog, Color color)
    {
        Texture2D ring = AssetRegistry.GetTexture(AdditionsTexture.EpidemicCircle);

        ManagedShader effect = ShaderRegistry.MagicRing;
        effect.TrySetParameter("time", rotation);
        effect.TrySetParameter("cosine", (float)Math.Cos(rotation));
        effect.TrySetParameter("firstCol", color.ToVector3());
        effect.TrySetParameter("secondCol", Color.DarkSeaGreen.ToVector3());
        effect.TrySetParameter("opacity", prog * .75f);

        sb.EnterShaderRegion(BlendState.Additive, effect.Shader.Value);

        Rectangle target = ToTarget(pos, (int)(60 * (w + prog)), (int)(60 * (h + prog)));
        sb.Draw(ring, target, null, color * prog, Projectile.velocity.ToRotation(), ring.Size() / 2, 0, 0);

        sb.ExitShaderRegion();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects effects = Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

        Vector2 origin = frame.Size() * 0.5f;

        if (RightCounter > 0)
        {
            float interpolant = InverseLerp(0f, EpidemicSpear.TotalCharge, RightCounter);
            Color col = Color.Lerp(Color.DarkOliveGreen, Color.LawnGreen, interpolant) * interpolant;
            float scale = 2f - interpolant;
            DrawRing(Main.spriteBatch, Projectile.Center, scale, scale, Main.GameUpdateCount / 20f, interpolant, col);
        }

        Main.spriteBatch.DrawBetter(texture, Projectile.Center, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, effects);

        return false;
    }
}