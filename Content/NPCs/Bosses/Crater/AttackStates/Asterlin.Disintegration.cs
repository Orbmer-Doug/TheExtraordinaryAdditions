using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> Disintegration_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Lightripper, 1f }, { AsterlinAIType.Tesselestic, .6f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Disintegration()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Disintegration, Disintegration_PossibleStates, false, () =>
        {
            return Disintegration_CurrentShot >= Disintegration_TotalShots && !AnyProjectile(ModContent.ProjectileType<VaporizingStar>());
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Disintegration, DoBehavior_Disintegration);
    }

    public static readonly int Disintegration_HoverTime = SecondsToFrames(1.5f);
    public static int Disintegration_TotalBeams => DifficultyBasedValue(10, 10, 12, 12, 15, 15);
    public static int Disintegration_TotalShots => DifficultyBasedValue(6, 6, 7, 7, 7, 8);
    public int Disintegration_CurrentShot
    {
        get => (int)ExtraAI[0];
        set => ExtraAI[0] = value;
    }
    public int Disintegration_Star1Index
    {
        get => (int)ExtraAI[1];
        set => ExtraAI[1] = value;
    }
    public int Disintegration_Star2Index
    {
        get => (int)ExtraAI[2];
        set => ExtraAI[2] = value;
    }

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
            if (this.RunServer())
            {
                Disintegration_Star1Index = NPC.NewNPCProj(NPC.Center + new Vector2(350f, -200f), Vector2.Zero, proj, HeavyAttackDamage, 0f, ai2: 1);
                Disintegration_Star2Index = NPC.NewNPCProj(NPC.Center + new Vector2(-350f, -200f), Vector2.Zero, proj, HeavyAttackDamage, 0f, ai2: -1);
                this.Sync();
            }
        }

        if (AITimer > Disintegration_HoverTime)
        {
            SetLookingStraight(true);

            Projectile star1 = Main.projectile?[Disintegration_Star1Index] ?? null;
            Projectile star2 = Main.projectile?[Disintegration_Star2Index] ?? null;
            if (star1 != null)
                SetRightHandTarget(star1.Center);
            if (star2 != null)
                SetLeftHandTarget(star2.Center);

            CasualHoverMovement(default, 400f, .11f, .95f);
        }
    }
}