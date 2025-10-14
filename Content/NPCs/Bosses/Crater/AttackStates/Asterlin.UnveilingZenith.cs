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
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_UnveilingZenith()
    {
        StateMachine.RegisterTransition(AsterlinAIType.UnveilingZenith, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.GabrielLeave, 1f } }, false, () =>
        {
            return AITimer >= UnveilingZenith_TotalTime && !Utility.AnyProjectile(ModContent.ProjectileType<BarrageBeam>());
        });
        StateMachine.RegisterTransition(AsterlinAIType.UnveilingZenith, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.GetScrewed, 1f } }, false, () =>
        {
            return UnveilingZenith_CollapseTimer >= UnveilingZenith_StarCollapseTime;
        });

        StateMachine.RegisterStateEntryCallback(AsterlinAIType.UnveilingZenith, () =>
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (this.RunServer())
                    NPC.NewNPCProj(player.Center - Vector2.UnitY * 400f, Main.rand.NextVector2Circular(10f, 10f), ModContent.ProjectileType<ConvergentFireball>(), 0, 0f, 0f, 0f, 0f, 0f, 0f);
            }
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.UnveilingZenith, DoBehavior_UnveilingZenith);
    }

    public static int UnveilingZenith_StarCollapseTime => SecondsToFrames(.3f);
    public static int UnveilingZenith_FlameReleaseRate => DifficultyBasedValue(SecondsToFrames(.4f), SecondsToFrames(.3f), SecondsToFrames(.25f), SecondsToFrames(.22f), SecondsToFrames(.2f), SecondsToFrames(.17f));
    public static int UnveilingZenith_BeamReleaseRate => DifficultyBasedValue(SecondsToFrames(.5f), SecondsToFrames(.4f), SecondsToFrames(.3f), SecondsToFrames(.25f), SecondsToFrames(.2f), SecondsToFrames(.2f));
    public static int UnveilingZenith_TotalTime => SecondsToFrames(20.8f);
    public static float UnveilingZenith_BlurAmount => .4f;

    public int UnveilingZenith_CurrentAmount
    {
        get => (int)ExtraAI[0];
        set => ExtraAI[0] = value;
    }

    public int UnveilingZenith_CollapseTimer
    {
        get => (int)ExtraAI[1];
        set => ExtraAI[1] = value;
    }

    public void DoBehavior_UnveilingZenith()
    {
        BarrageBeamManager.Golden = true;
        PlayerCount(out int total, out _);
        if (UnveilingZenith_CurrentAmount >= total)
        {
            UnveilingZenith_CollapseTimer++;

            if (UnveilingZenith_CollapseTimer >= UnveilingZenith_StarCollapseTime)
            {
                if (this.RunServer())
                    NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DisintegrationNova>(), Asterlin.SuperHeavyAttackDamage * 5, 100f);
                foreach (Projectile p in Utility.AllProjectilesByID(ModContent.ProjectileType<ConvergentFireball>()))
                    p.Kill();
            }
        }

        SetMotionBlurInterpolant(InverseLerp(0f, ConvergentFireball.ScaleUpTime, AITimer) * UnveilingZenith_BlurAmount);

        if (AITimer < UnveilingZenith_TotalTime)
        {
            Vector2 pos = Vector2.Lerp(LeftVentPosition, RightVentPosition, .5f);
            if (AITimer % UnveilingZenith_BeamReleaseRate == (UnveilingZenith_BeamReleaseRate - 1))
            {
                if (this.RunServer())
                    NPC.NewNPCProj(pos, Vector2.Zero, ModContent.ProjectileType<BarrageBeam>(), LightAttackDamage, 0f);
            }
        }

        float velocity = 4f;
        float amt = .1f;
        Vector2 target = Target.Position + new Vector2(400f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), Target.Velocity.Y * 15f);
        NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(target) * MathF.Min(NPC.Center.Distance(target), velocity), amt);

        SetRightHandTarget(RightArm.RootPosition + PolarVector(400f, -MathHelper.PiOver2));
    }
}