using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> EnterPhase3_PossibleStates = new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Cleave, 1f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_EnterPhase3()
    {
        StateMachine.RegisterTransition(AsterlinAIType.EnterPhase3, EnterPhase3_PossibleStates, false, () => AITimer >= EnterPhase3_Length);
        StateMachine.RegisterStateBehavior(AsterlinAIType.EnterPhase3, DoBehavior_EnterPhase3);
    }

    public static int EnterPhase3_Length => SecondsToFrames(4f);
    public const float EnterPhase3_MaxHeatDistortionArea = 900f;
    public const float EnterPhase3_MaxHeatDistortionStrength = .7f;

    public void DoBehavior_EnterPhase3()
    {
        if (AITimer == 1)
            AdditionsSound.SteamRelease.Play(NPC.Center, 1.5f, -.2f);

        HeatDistortionArea = Animators.Sine.InOutFunction.Evaluate(AITimer, 0f, EnterPhase3_Length, 0f, EnterPhase3_MaxHeatDistortionArea);
        HeatDistortionStrength = Animators.MakePoly(3f).InFunction.Evaluate(AITimer, 0f, EnterPhase3_Length / 2f, 0f, EnterPhase3_MaxHeatDistortionStrength);
        VentGlowInterpolant = Utils.Remap(AITimer, 0f, 80f, 0f, .5f);

        ParticleRegistry.SpawnMistParticle(LeftVentPosition, Vector2.UnitX.RotatedByRandom(.08f) * Main.rand.NextFloat(8f, 24f) * -Direction, Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(130f, 180f));
        ParticleRegistry.SpawnMistParticle(RightVentPosition, Vector2.UnitX.RotatedByRandom(.08f) * Main.rand.NextFloat(8f, 24f) * -Direction, Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(130f, 180f));

        if (AITimer == EnterPhase3_Length / 2)
        {
            AdditionsSound.AsterlinChange.Play(NPC.Center, 1.2f, -.1f);
        }
        EyeGleamInterpolant = new Animators.PiecewiseCurve()
            .Add(0f, 1f, .5f, Animators.MakePoly(3f).OutFunction)
            .Add(1f, 0f, 1f, Animators.MakePoly(4f).InOutFunction)
            .Evaluate(InverseLerp(EnterPhase3_Length / 2, EnterPhase3_Length, AITimer));

        NPC.velocity = Vector2.SmoothStep(NPC.Center, Target.Center + new Vector2(300f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), -30f), .1f) - NPC.Center;
    }
}