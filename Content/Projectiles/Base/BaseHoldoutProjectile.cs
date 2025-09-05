using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Base;

/// <summary>
/// 
/// </summary>
public abstract class BaseHoldoutProjectile : ModProjectile, ILocalizedModType, IModType
{
    public virtual void Defaults() { }
    public sealed override void SetDefaults()
    {
        Projectile.timeLeft = 10000;
        Projectile.tileCollide = Projectile.hostile = false;
        Projectile.ignoreWater = Projectile.friendly = Projectile.ContinuouslyUpdateDamageStats = Projectile.noEnchantmentVisuals = Projectile.netImportant = true;
        Projectile.penetrate = -1;
        Defaults();
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Item => Owner.HeldItem;
    public Vector2 Center;
    public Vector2 Mouse;
    public virtual bool SetItemTime => true;

    public bool TryUseMana() => Item.CheckManaBetter(Owner, Item.mana, true);
    public bool TryUseAmmo(out int type, out float speed, out int dmg, out float kb, out int ammoID)
    {
        Owner.PickAmmo(Item, out type, out speed, out dmg, out kb, out ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
        return Owner.HasAmmo(Item);
    }
    public SpriteEffects FixedDirection()
    {
        SpriteEffects effects = Projectile.direction == -Owner.gravDir ? SpriteEffects.FlipVertically : SpriteEffects.None;
        if (Owner.gravDir == -1 && Projectile.direction == -Owner.gravDir)
            effects |= SpriteEffects.FlipVertically;
        return effects;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public sealed override bool ShouldUpdatePosition() => false;
    public sealed override void AI()
    {
        if (ShouldDie())
        {
            DieEffect();
            return;
        }
        else
        {
            Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
            Mouse = Modded.mouseWorld;

            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            if (SetItemTime)
                Owner.SetDummyItemTime(2);
            SafeAI();
            Projectile.spriteDirection = Projectile.direction;
            Owner.itemRotation = MathHelper.WrapAngle(Projectile.velocity.ToRotation() * Projectile.direction);
        }
    }
    public virtual void SafeAI() { }
    public virtual bool ShouldDie()
    {
        return !Owner.Available() || (this.RunLocal() && !Modded.SafeMouseLeft.Current);
    }
    public virtual void DieEffect()
    {
        Projectile.Kill();
    }

    public sealed override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((sbyte)Projectile.spriteDirection);
        WriteExtraAI(writer);
    }

    public sealed override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.spriteDirection = reader.ReadSByte();
        GetExtraAI(reader);
    }
    public virtual void WriteExtraAI(BinaryWriter writer) { }
    public virtual void GetExtraAI(BinaryReader reader) { }
}
