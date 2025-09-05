using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_DesperationDrama()
    {
        StateMachine.RegisterTransition(AsterlinAIType.DesperationDrama, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.UnrelentingRush, 1f } }, false, () =>
        {
            return AITimer >= DesperationDrama_MaxTime;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.DesperationDrama, DoBehavior_DesperationDrama);
    }

    public bool DesperationDrama_BeginDialogue
    {
        get => NPC.AdditionsInfo().ExtraAI[0] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public static readonly float DesperationDrama_CameraScrollTime = SecondsToFrames(2f);
    public static readonly float DesperationDrama_ScreenPullupTime = SecondsToFrames(.8f);
    public static readonly float DesperationDrama_ChannelFindTime = SecondsToFrames(.5f);
    public float DesperationDrama_DialogueTime => FullDialogue.MaxProgress * 60;
    public float DesperationDrama_MaxTime => DesperationDrama_DialogueTime - (DesperationDrama_CameraScrollTime + DesperationDrama_ScreenPullupTime + DesperationDrama_ChannelFindTime);

    public const float DesperationDrama_MaxHeatDistortionArea = 1200f;
    public const float DesperationDrama_MaxHeatDistortionStrength = 1.2f;

    public LoopedSound Ominous;
    public void DoBehavior_DesperationDrama()
    {
        if (Dialogue_Manager == null)
        {
            Dialogue_Manager = new(Vector2.Zero, .9f, .2f, 0f);
            Dialogue_Manager.AddSentence(FullDialogue);
        }

        Ominous ??= new(AssetRegistry.GetSound(AdditionsSound.PipIdle), () => NPC.active && CurrentState == AsterlinAIType.DesperationDrama && !Main.gameMenu);
        Ominous?.Update(position: () => NPC.Center, volume: () => InverseLerp(FullDialogue.GetTimeToSnippet(1), FullDialogue.GetTimeToSnippet(2), AITimer) * .4f, pitch: () => 1f);

        HeatDistortionArea = Animators.Sine.InOutFunction.Evaluate(AITimer, SecondsToFrames(2.6f), SecondsToFrames(4f), EnterPhase3_MaxHeatDistortionArea, DesperationDrama_MaxHeatDistortionArea);
        HeatDistortionStrength = Animators.MakePoly(3f).InFunction.Evaluate(AITimer, SecondsToFrames(2.6f), SecondsToFrames(4f), EnterPhase3_MaxHeatDistortionStrength, DesperationDrama_MaxHeatDistortionStrength);
        //VentGlowInterpolant = Utils.Remap(AITimer, 0f, 120f, .5f, 1f);
        GlowInterpolant = Utils.Remap(AITimer, FullDialogue.GetTimeToSnippet(16), FullDialogue.GetTimeToSnippet(17), 0f, .4f);

        SetLegFlamesInterpolant(InverseLerp(70f, 0f, AITimer));
        SetLeftLegRotation(LeftLegRotation.AngleLerp(-1.5f * Direction, .2f));
        SetRightLegRotation(RightLegRotation.AngleLerp(-1.5f * Direction, .2f));
        SetHeadRotation(EyePosition.AngleTo(EyePosition + PolarVector(400f, Direction == -1 ? MathHelper.PiOver4 : -(MathHelper.PiOver4 + MathHelper.Pi))));

        NPC.velocity.X *= .6f;
        NPC.velocity.Y += .4f;
        NPC.noGravity = false;
        NPC.noTileCollide = false;

        CameraSystem.SetCamera(NPC.Center - Vector2.UnitY * 200f, Animators.MakePoly(2.3f).InOutFunction(InverseLerp(0f, DesperationDrama_CameraScrollTime, AITimer)));

        int wait = (int)(DesperationDrama_CameraScrollTime + DesperationDrama_ScreenPullupTime + DesperationDrama_ChannelFindTime);
        if (AITimer < wait)
        {
            Dialogue_ScreenInterpolant = InverseLerp(DesperationDrama_CameraScrollTime, DesperationDrama_CameraScrollTime + DesperationDrama_ScreenPullupTime, AITimer);
            Dialogue_FindingChannel = true;
        }
        else
        {
            float startToFade = wait + FullDialogue.GetTimeToSnippet(17);
            if (AITimer > startToFade)
            {
                Dialogue_ScreenInterpolant = 1f - InverseLerp(startToFade, startToFade + DesperationDrama_ScreenPullupTime, AITimer);
                Dialogue_FindingChannel = true;
            }
            else
                Dialogue_FindingChannel = false;

            if (!DesperationDrama_BeginDialogue)
            {
                Dialogue_Manager.Start();
                DesperationDrama_BeginDialogue = true;
            }
            Dialogue_Manager.Update(.02f);

            int heatstink = wait + FullDialogue.GetTimeToSnippet(2);
            if (AITimer == heatstink)
                AdditionsSound.SteamRelease.Play(NPC.Center, 1.5f, -.3f);
            if (AITimer >= heatstink)
            {
                ParticleRegistry.SpawnMistParticle(LeftVentPosition, Vector2.UnitX.RotatedByRandom(.08f) * Main.rand.NextFloat(8f, 24f) * -Direction, Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(130f, 180f));
                ParticleRegistry.SpawnMistParticle(RightVentPosition, Vector2.UnitX.RotatedByRandom(.08f) * Main.rand.NextFloat(8f, 24f) * -Direction, Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(130f, 180f));
            }

            if (AITimer == (wait + FullDialogue.GetTimeToSnippet(15)))
                AdditionsSound.AsterlinChange.Play(NPC.Center, 2f, .3f);
        }
    }
}