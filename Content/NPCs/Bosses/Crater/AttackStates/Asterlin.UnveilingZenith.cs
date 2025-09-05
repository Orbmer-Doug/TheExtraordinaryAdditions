using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_UnveilingZenith()
    {
        StateMachine.RegisterTransition(AsterlinAIType.UnveilingZenith, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.GabrielLeave, 1f } }, false, () =>
        {
            return UnveilingZenith_WaitTimer >= UnveilingZenith_WaitTime;
        });
        StateMachine.RegisterStateEntryCallback(AsterlinAIType.UnveilingZenith, () => { NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<VaporizingSupergiant>(), SuperHeavyAttackDamage, 0f); });
        StateMachine.RegisterStateBehavior(AsterlinAIType.UnveilingZenith, DoBehavior_UnveilingZenith);
    }

    public static float UnveilingZenith_StarBuildTime => SecondsToFrames(30f);
    public static float UnveilingZenith_StarCollapseTime => SecondsToFrames(.3f);
    public static int UnveilingZenith_BeamReleaseRate => DifficultyBasedValue(SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f));
    public static float UnveilingZenith_WaitTime => SecondsToFrames(.3f);

    public int UnveilingZenith_WaitTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }

    public void DoBehavior_UnveilingZenith()
    {
        float velocity = Utils.Remap(AITimer, 0f, UnveilingZenith_StarBuildTime / 2, 30f, 3f);
        float amt = Utils.Remap(AITimer, 0f, UnveilingZenith_StarBuildTime / 2, .32f, .04f);
        Vector2 target = Target.Position + new Vector2(400f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), Target.Velocity.Y * 15f);
        NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(target) * MathF.Min(NPC.Center.Distance(target), velocity), amt);

        SetRightHandTarget(rightArm.RootPosition + PolarVector(400f, -MathHelper.PiOver2));
    }
}