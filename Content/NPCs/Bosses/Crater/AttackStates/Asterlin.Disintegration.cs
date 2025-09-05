using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Disintegration()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Disintegration, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Cleave, 1f }, { AsterlinAIType.Lightripper, 1f } }, false, () =>
        {
            return Disintegration_CurrentShot >= Disintegration_TotalShots && !AnyProjectile(ModContent.ProjectileType<VaporizingStar>());
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Disintegration, DoBehavior_Disintegration);
    }

    public static int Disintegration_HoverTime => SecondsToFrames(1.5f);
    public static int Disintegration_TotalBeams => DifficultyBasedValue(10, 10, 12, 12, 15, 15);
    public static int Disintegration_TotalShots => DifficultyBasedValue(6, 6, 7, 7, 7, 8);
    public int Disintegration_CurrentShot
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }
    public Projectile Disintegration_Star1;
    public Projectile Disintegration_Star2;

    public void DoBehavior_Disintegration()
    {
        int proj = ModContent.ProjectileType<VaporizingStar>();

        if (AITimer < Disintegration_HoverTime)
        {
            // Slightly hover above the player
            Vector2 airTarget = Target.Center - Vector2.UnitY * 250f + Target.Velocity * 4f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(airTarget) * Math.Max(2f, NPC.Distance(airTarget)) * 0.2f, 0.08f) * Utils.GetLerpValue(90f, 75f, AITimer, true);
        }
        if (AITimer == Disintegration_HoverTime)
        {
            // Create the stars
            Disintegration_Star1 = Main.projectile[NPC.NewNPCProj(NPC.Center + new Vector2(350f, -200f), Vector2.Zero, proj, HeavyAttackDamage, 0f)];
            Disintegration_Star1.As<VaporizingStar>().TypeOf = 1;

            Disintegration_Star2 = Main.projectile[NPC.NewNPCProj(NPC.Center + new Vector2(-350f, -200f), Vector2.Zero, proj, HeavyAttackDamage, 0f)];
            Disintegration_Star2.As<VaporizingStar>().TypeOf = -1;
        }
        if (AITimer > Disintegration_HoverTime)
        {
            SetLookingStraight(true);
            if (Disintegration_Star1 != null)
                SetRightHandTarget(Disintegration_Star1.Center);
            if (Disintegration_Star2 != null)
                SetLeftHandTarget(Disintegration_Star2.Center);

            CasualHoverMovement(default, 400f, .11f, .95f);
        }
    }
}