using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

// orbital light beams near players + radial dart emission in cyclic pattern (not immediate circle) + pushing fireballs away
public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_UnveilingZenith()
    {
        StateMachine.RegisterTransition(AsterlinAIType.UnveilingZenith, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.GabrielLeave, 1f } }, false, () =>
        {
            return UnveilingZenith_WaitTimer >= UnveilingZenith_WaitTime;
        });
        StateMachine.RegisterStateEntryCallback(AsterlinAIType.UnveilingZenith, () => 
        {

        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.UnveilingZenith, DoBehavior_UnveilingZenith);
    }

    public static int UnveilingZenith_StarCollapseTime => SecondsToFrames(.3f);
    public static int UnveilingZenith_BeamReleaseRate => DifficultyBasedValue(SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f), SecondsToFrames(1f));

    public int UnveilingZenith_WaitTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }

    public int UnveilingZenith_NeededAmount
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = value;
    }

    public int UnveilingZenith_CurrentAmount
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[2];
        set => NPC.AdditionsInfo().ExtraAI[2] = value;
    }

    public int UnveilingZenith_CollapseTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[3];
        set => NPC.AdditionsInfo().ExtraAI[3] = value;
    }

    public void DoBehavior_UnveilingZenith()
    {
        if (AITimer == 0)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (this.RunServer())
                    NPC.NewNPCProj(player.Center, Main.rand.NextVector2Circular(10f, 10f), ModContent.ProjectileType<ConvergentFireball>(), 0, 0f, 0f, 0f, 0f, 0f, 0f);
            }

            PlayerCount(out int total, out _);
            UnveilingZenith_NeededAmount = total;
        }

        if (UnveilingZenith_CurrentAmount >= UnveilingZenith_NeededAmount)
        {
            UnveilingZenith_CollapseTimer++;

            if (UnveilingZenith_CollapseTimer >= UnveilingZenith_StarCollapseTime)
            {
                if (this.RunServer())
                    NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DisintegrationNova>(), Asterlin.SuperHeavyAttackDamage * 5, 100f);
            }
        }

        float velocity = Utils.Remap(AITimer, 0f, UnveilingZenith_StarBuildTime / 2, 30f, 3f);
        float amt = Utils.Remap(AITimer, 0f, UnveilingZenith_StarBuildTime / 2, .32f, .04f);
        Vector2 target = Target.Position + new Vector2(400f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), Target.Velocity.Y * 15f);
        NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(target) * MathF.Min(NPC.Center.Distance(target), velocity), amt);

        SetRightHandTarget(rightArm.RootPosition + PolarVector(400f, -MathHelper.PiOver2));
    }
}