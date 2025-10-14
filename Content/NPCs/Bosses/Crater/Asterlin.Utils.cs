using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

/// Utilities that help with positioning, fields, etc.
public partial class Asterlin : ModNPC
{
    public const int AngledWidth = 128;
    public const int TotalHeight = 278;
    public const int FrontWidth = 118;
    public float LegHeight => 90 * ZPosition;
    public float AngledLeftArmLength => 74 * ZPosition;
    public float AngledRightArmLength => 84 * ZPosition;
    public float StraightArmLength => 88 * ZPosition;

    public void SearchForArsenalWeapons()
    {
        Sword = null;
        Gun = null;
        Staff = null;
        Hammer = null;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            // Wittle down to projectiles from the mod
            if (projectile.ModProjectile == null || projectile.type < ProjectileID.Count)
                continue;
            if (projectile.ModProjectile.Mod.Name != AdditionsMain.Instance.Name)
                continue;

            // Check if its a projectile owned from asterlin
            ProjOwnedByNPC<Asterlin> owned = projectile.ModProjectile as ProjOwnedByNPC<Asterlin>;
            if (owned == null)
                continue;

            // Search through the types
            if (Sword == null && projectile.type == ModContent.ProjectileType<CyberneticSword>())
            {
                SwordIndex = projectile.whoAmI;
                Sword = Main.projectile[SwordIndex].As<CyberneticSword>();
                this.Sync();
            }

            if (Gun == null && projectile.type == ModContent.ProjectileType<TheTechnicBlitzripper>())
            {
                GunIndex = projectile.whoAmI;
                Gun = Main.projectile[GunIndex].As<TheTechnicBlitzripper>();
                this.Sync();
            }

            if (Staff == null && projectile.type == ModContent.ProjectileType<TheTesselesticMeltdown>())
            {
                StaffIndex = projectile.whoAmI;
                Staff = Main.projectile[StaffIndex].As<TheTesselesticMeltdown>();
                this.Sync();
            }

            if (Hammer == null && projectile.type == ModContent.ProjectileType<JudgementHammer>())
            {
                HammerIndex = projectile.whoAmI;
                Hammer = Main.projectile[HammerIndex].As<JudgementHammer>();
                this.Sync();
            }
        }
    }

    public void CasualHoverMovement(float xPos = 14.5f, float yPos = 200f, float sharpness = .07f, float smoothness = .94f)
    {
        float hoverHorizontalWaveSine = MathF.Sin(MathHelper.TwoPi * AITimer / 96f);
        float hoverVerticalWaveSine = MathF.Sin(MathHelper.TwoPi * AITimer / 120f);
        Vector2 hoverDestination = Target.Center + new Vector2(Target.Velocity.X * xPos, ZPosition * -40f - yPos);
        hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 40f;
        hoverDestination.Y -= hoverVerticalWaveSine * ZPosition * 8f;

        NPC.SmoothFlyNear(hoverDestination, sharpness, smoothness);
    }
}
