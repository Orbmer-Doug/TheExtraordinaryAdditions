using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class PlayerUtils
{
    public static Player.CompositeArmStretchAmount ToStretchAmount(this float interpolant)
    {
        return interpolant switch
        {
            < 0.25f => Player.CompositeArmStretchAmount.None,
            < 0.5f => Player.CompositeArmStretchAmount.Quarter,
            < 0.75f => Player.CompositeArmStretchAmount.ThreeQuarters,
            _ => Player.CompositeArmStretchAmount.Full
        };
    }

    public static int GetFreeInventorySlot(Player plr)
    {
        for (int k = 0; k < 49; k++)
        {
            Item item = plr.inventory[k];

            if (item is null || item.IsAir)
                return k;
        }

        return -1;
    }
    
    extension(Player player)
    {
        public void SetFrontHandBetter(Player.CompositeArmStretchAmount stretch, float rotation) =>
            player.SetCompositeArmFront(true, stretch,
                (rotation - MathHelper.PiOver2) * player.gravDir + ((int) player.gravDir == -1 ? MathHelper.Pi : 0f));

        public void SetBackHandBetter(Player.CompositeArmStretchAmount stretch, float rotation) =>
            player.SetCompositeArmBack(true, stretch,
                (rotation - MathHelper.PiOver2) * player.gravDir + ((int) player.gravDir == -1 ? MathHelper.Pi : 0f));

        public Vector2 GetFrontHandPositionImproved(bool addGfXOffY = true)
        {
            Player.CompositeArmData arm = player.compositeFrontArm;
            Vector2 position = player
                .GetFrontHandPosition(arm.stretch, (arm.rotation + player.fullRotation) * player.gravDir).Floor();
            if ((int) player.gravDir == -1)
                position.Y = player.position.Y + player.height + (player.position.Y - position.Y);

            if (addGfXOffY)
                position += Vector2.UnitY * player.gfxOffY;
            return position;
        }

        public Vector2 GetBackHandPositionImproved(bool addGfXOffY = true)
        {
            Player.CompositeArmData arm = player.compositeBackArm;
            Vector2 position = player
                .GetBackHandPosition(arm.stretch, (arm.rotation + player.fullRotation) * player.gravDir).Floor();
            if ((int) player.gravDir == -1)
                position.Y = player.position.Y + player.height + (player.position.Y - position.Y);

            if (addGfXOffY)
                position += Vector2.UnitY * player.gfxOffY;
            return position;
        }

        public DamageClass GetBestClass()
        {
            float bestDamage = 1f;
            DamageClass bestClass = DamageClass.Generic;

            float melee = player.GetTotalDamage<MeleeDamageClass>().Additive;
            if (melee > bestDamage)
            {
                bestDamage = melee;
                bestClass = DamageClass.Melee;
            }

            float ranged = player.GetTotalDamage<RangedDamageClass>().Additive;
            if (ranged > bestDamage)
            {
                bestDamage = ranged;
                bestClass = DamageClass.Ranged;
            }

            float magic = player.GetTotalDamage<MagicDamageClass>().Additive;
            if (magic > bestDamage)
            {
                bestDamage = magic;
                bestClass = DamageClass.Magic;
            }

            float summon = player.GetTotalDamage<SummonDamageClass>().Additive * .75f;
            if (summon > bestDamage)
            {
                bestDamage = summon;
                bestClass = DamageClass.Summon;
            }

            return bestClass;
        }

        public float UsedMinions(int? ofType = null)
        {
            float usedMinions = 0;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (ofType != null)
                {
                    if (p.type != ofType.Value)
                        continue;
                }

                if (p != null && p.minion && p.owner == player.whoAmI)
                    usedMinions += p.minionSlots;
            }

            return usedMinions;
        }

        public bool ShouldConsumeAmmo(Item item) =>
            player.IsAmmoFreeThisShot(item, player.ChooseAmmo(item), player.ChooseAmmo(item).type);

        public bool Available() => player != null && player.active && !player.dead &&
                                   !player.ghost && !player.CCed && !player.noItems;

        public bool OnGround()
            => player.velocity.Y == 0f;

        public bool WasOnGround()
            => player.oldVelocity.Y == 0f;

        public bool InventoryHas(params int[] items) =>
            player.inventory.Any(item => items.Contains(item.type));

        public bool StandingStill(float velocity = 0.05f) => player.velocity.Length() < velocity;

        public bool IsUnderwater() =>
            Collision.DrownCollision(player.position, player.width, player.height, player.gravDir);

        public bool InSpace()
        {
            float x = Main.maxTilesX / 4200f;
            x *= x;
            return (float) ((player.position.Y / 16f - (60f + 10f * x)) / (Main.worldSurface / 6.0)) < 1f;
        }

        public bool GiveIFrames(int frames, bool blink = false)
        {
            bool anyIFramesWouldBeGiven = false;
            for (int j = 0; j < player.hurtCooldowns.Length; j++)
            {
                if (player.hurtCooldowns[j] < frames)
                {
                    anyIFramesWouldBeGiven = true;
                }
            }

            if (!anyIFramesWouldBeGiven)
            {
                return false;
            }

            player.immune = true;
            player.immuneNoBlink = !blink;
            player.immuneTime = frames;
            for (int i = 0; i < player.hurtCooldowns.Length; i++)
            {
                if (player.hurtCooldowns[i] < frames)
                    player.hurtCooldowns[i] = frames;
            }

            return true;
        }

        public void HideAccessories(bool hideHeadAccs = true, bool hideBodyAccs = true,
            bool hideLegAccs = true, bool hideShield = true)
        {
            if (hideHeadAccs)
            {
                player.face = -1;
            }

            if (hideBodyAccs)
            {
                player.handon = -1;
                player.handoff = -1;
                player.back = -1;
                player.front = -1;
                player.neck = -1;
            }

            if (hideLegAccs)
            {
                player.shoe = -1;
                player.waist = -1;
            }

            if (hideShield)
            {
                player.shield = -1;
            }
        }

        /// <summary>
        /// Make a new projectile from a source of a player
        /// </summary>
        public int CreatePlayerProj(Vector2 center, Vector2 velocity, int type, int damage,
            float knockback, int owner = -1,
            float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float extra0 = 0f, float extra1 = 0f)
        {
            IEntitySource source = player.GetSource_FromThis();
            int index = Projectile.NewProjectile(source, center, velocity, type, damage, knockback, owner, ai0, ai1,
                ai2);
            Projectile projectile = Main.projectile[index];
            if (index >= 0 && index < Main.maxProjectiles)
                projectile.netUpdate = true;

            if (projectile.ModProjectile != null && projectile.ModProjectile.Mod == AdditionsMain.Instance)
            {
                projectile.AdditionsInfo().ExtraAI[0] = extra0;
                projectile.AdditionsInfo().ExtraAI[1] = extra1;
            }

            return index;
        }
        
        public Item HeldMouseItem()
        {
            return !Main.mouseItem.IsAir ? Main.mouseItem : player.HeldItem;
        }
    }
}
