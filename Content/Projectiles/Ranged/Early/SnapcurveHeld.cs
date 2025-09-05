using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class SnapcurveHeld : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<CrystallineSnapcurve>();
    public override int IntendedProjectileType => ModContent.ProjectileType<SnapcurveHeld>();
    public override void Defaults()
    {
        Projectile.width = Projectile.height = 176;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.MaxUpdates = 2;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float LimbRotation => ref Projectile.ai[1];
    public ref float Switch => ref Projectile.ai[2];
    public override void OnSpawn(IEntitySource source)
    {
        Switch = -1;
        this.Sync();
    }

    public override void SafeAI()
    {
        Item item = Owner.HeldItem;

        bool left = this.RunLocal() && Owner.Additions().MouseLeft.Current;
        bool activatingShoot = Switch == -1 && left && !Main.mapFullscreen && !Owner.mouseInterface;
        if (this.RunLocal() && Owner.HasAmmo(Item) && activatingShoot)
        {
            Switch = 1;
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CrystallineBlast>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f, Projectile.whoAmI);
            this.Sync();
        }

        if (this.RunLocal())
        {
            float aimInterpolant = Utils.GetLerpValue(10f, 40f, Projectile.Distance(Owner.Additions().mouseWorld), true);
            Vector2 oldVelocity = Projectile.velocity;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Additions().mouseWorld), aimInterpolant);
            if (Projectile.velocity != oldVelocity)
                this.Sync();
        }
        Projectile.Center = Center + PolarVector(24f, Projectile.velocity.ToRotation());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.spriteDirection = Projectile.direction;
        Owner.ChangeDir(Projectile.direction);
        Owner.SetFrontHandBetter(0, Projectile.velocity.ToRotation());
        Owner.SetBackHandBetter(0, Projectile.velocity.ToRotation());

        float reel = InverseLerp(0f, item.useAnimation, Time);
        float close = InverseLerp(0f, 7f, Time);

        if (Projectile.FinalExtraUpdate())
        {
            switch (Switch)
            {
                case 0:
                    Owner.itemTime = Owner.itemAnimation = 0;
                    LimbRotation = Animators.Circ.OutFunction.Evaluate(LimbRotation, 0f, close);
                    if (close >= 1f)
                    {
                        Switch = -1;
                        Time = 0f;
                        this.Sync();
                    }
                    break;
                case 1:
                    LimbRotation = Animators.MakePoly(2).OutFunction.Evaluate(0f, .65f, reel);
                    if (reel >= 1f || !Modded.MouseLeft.Current)
                    {
                        Switch = 0;
                        Time = 0f;
                        this.Sync();
                    }
                    break;
            }

            if (Switch > -1)
            {
                Time++;
                this.Sync();
            }
        }
    }

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrystallineSnapcurve);

    public static readonly Texture2D limb1 = AssetRegistry.GetTexture(AdditionsTexture.CrystallineSnapcurveProjLimb1);
    public static readonly Texture2D limb2 = AssetRegistry.GetTexture(AdditionsTexture.CrystallineSnapcurveProjLimb2);
    public override bool PreDraw(ref Color lightColor)
    {
        Color color = lightColor * Projectile.Opacity;

        float rot1 = Projectile.rotation - LimbRotation * -1;
        float rot2 = Projectile.rotation + LimbRotation * -1;

        Vector2 correction = -Vector2.UnitX.RotatedBy(Projectile.rotation) * 20f;
        Vector2 pos1 = Projectile.Center + PolarVector(30f, rot1 + .65f) + correction;
        Vector2 pos2 = Projectile.Center + PolarVector(30f, rot2 - .65f) + correction;

        Main.EntitySpriteDraw(limb1, pos1 - Main.screenPosition, null, color, rot1, limb1.Size() * .5f, Projectile.scale, 0);
        Main.EntitySpriteDraw(limb2, pos2 - Main.screenPosition, null, color, rot2, limb2.Size() * .5f, Projectile.scale, 0);

        return false;
    }
}