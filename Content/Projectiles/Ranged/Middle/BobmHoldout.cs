using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class BobmHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BobmOnAStick);
    public override void Defaults()
    {
        Projectile.width = 89;
        Projectile.height = 15;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = Center + Projectile.velocity * 40f;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        InconspicouslySearchForSuspectToExecuteMaliciousDeeds();
    }

    public void InconspicouslySearchForSuspectToExecuteMaliciousDeeds()
    {
        void FoundSuspect()
        {
            if (Owner.grapCount > 0)
                return;

            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.width;
            if (this.RunLocal())
                Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<StickBoom>(), Projectile.damage, 0f, Owner.whoAmI);
            Owner.mount.Dismount(Owner);
        }

        foreach (Player player in Main.ActivePlayers)
        {
            if (!player.WithinRange(Projectile.Center, Main.screenWidth))
                continue;
            if (player.Available() && player.RotHitbox().Intersects(Projectile.RotHitbox()) && player.whoAmI != Projectile.owner)
                FoundSuspect();
        }
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.WithinRange(Projectile.Center, Main.screenWidth))
                continue;
            if (npc.RotHitbox().Intersects(Projectile.RotHitbox()))
                FoundSuspect();
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() / 2, Projectile.scale, FixedDirection());
        return false;
    }
}