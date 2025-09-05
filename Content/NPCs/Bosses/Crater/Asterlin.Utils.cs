using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

/// Utilities that help with positioning, networking, and other misc.
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
            if (projectile.type == ModContent.ProjectileType<CyberneticSword>())
                Sword = projectile.As<CyberneticSword>();
            if (projectile.type == ModContent.ProjectileType<TheTechnicBlitzripper>())
                Gun = projectile.As<TheTechnicBlitzripper>();
            if (projectile.type == ModContent.ProjectileType<TheTesselesticMeltdown>())
                Staff = projectile.As<TheTesselesticMeltdown>();
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
