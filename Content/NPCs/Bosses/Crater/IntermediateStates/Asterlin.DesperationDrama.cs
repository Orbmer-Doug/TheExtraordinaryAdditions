using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> DesperationDrama_PossibleStates = new Dictionary<AsterlinAIType, float> { { AsterlinAIType.UnrelentingRush, 1f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_DesperationDrama()
    {
        StateMachine.RegisterTransition(AsterlinAIType.DesperationDrama, DesperationDrama_PossibleStates, false, () =>
        {
            return AITimer >= DesperationDrama_MaxTime;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.DesperationDrama, DoBehavior_DesperationDrama);
    }

    public bool DesperationDrama_BeginDialogue
    {
        get => ExtraAI[0] == 1f;
        set => ExtraAI[0] = value.ToInt();
    }
    public static readonly float DesperationDrama_CameraScrollTime = SecondsToFrames(2f);
    public static readonly float DesperationDrama_ScreenPullupTime = SecondsToFrames(.8f);
    public static readonly float DesperationDrama_ChannelFindTime = SecondsToFrames(.5f);
    public static readonly int DesperationDrama_Wait = (int)(DesperationDrama_CameraScrollTime + DesperationDrama_ScreenPullupTime + DesperationDrama_ChannelFindTime);

    public static readonly float DesperationDrama_MaxTime = DialogueTime + DesperationDrama_Wait;

    public const float DesperationDrama_MaxHeatDistortionArea = 1200f;
    public const float DesperationDrama_MaxHeatDistortionStrength = 1.2f;

    public LoopedSoundInstance Ominous;
    public void DoBehavior_DesperationDrama()
    {
        if (Dialogue_Manager == null)
        {
            Dialogue_Manager = new(Vector2.Zero, .9f, .2f, 0f);
            Dialogue_Manager.AddSentence(FullDialogue);
        }

        Ominous ??= LoopedSoundManager.CreateNew(new(AdditionsSound.PipIdle, () => InverseLerp(TimeToTemp, TimeToHeatsink, AITimer) * .4f),
            () => CurrentState != AsterlinAIType.DesperationDrama || AdditionsLoopedSound.NPCNotActive(NPC));
        Ominous?.Update(NPC.Center);

        HeatDistortionArea = Animators.Sine.InOutFunction.Evaluate(AITimer, SecondsToFrames(2.6f), SecondsToFrames(4f), EnterPhase3_MaxHeatDistortionArea, DesperationDrama_MaxHeatDistortionArea);
        HeatDistortionStrength = Animators.MakePoly(3f).InFunction.Evaluate(AITimer, SecondsToFrames(2.6f), SecondsToFrames(4f), EnterPhase3_MaxHeatDistortionStrength, DesperationDrama_MaxHeatDistortionStrength);
        GlowInterpolant = Utils.Remap(AITimer, TimeToUhOh, TimeToLast, 0f, .4f);
        PowerInterpolant = Utils.Remap(AITimer, TimeToUhOh, TimeToLast, 0f, 1f);

        SetLegFlamesInterpolant(InverseLerp(70f, 0f, AITimer));
        SetLeftLegRotation(LeftLegRotation.AngleLerp(-1.5f * Direction, .2f));
        SetRightLegRotation(RightLegRotation.AngleLerp(-1.5f * Direction, .2f));
        SetHeadRotation(EyePosition.AngleTo(EyePosition + PolarVector(400f, Direction == -1 ? MathHelper.PiOver4 : -(MathHelper.PiOver4 + MathHelper.Pi))));

        NPC.velocity.X *= .6f;
        NPC.velocity.Y += .4f;
        NPC.noGravity = false;
        NPC.noTileCollide = false;

        CameraSystem.SetCamera(NPC.Center - Vector2.UnitY * 200f, Animators.MakePoly(2.3f).InOutFunction(InverseLerp(0f, DesperationDrama_CameraScrollTime, AITimer)));

        if (AITimer < DesperationDrama_Wait)
        {
            Dialogue_ScreenInterpolant = InverseLerp(DesperationDrama_CameraScrollTime, DesperationDrama_CameraScrollTime + DesperationDrama_ScreenPullupTime, AITimer);
            Dialogue_FindingChannel = true;
            this.Sync();
        }
        else
        {
            float startToFade = DesperationDrama_Wait + TimeToLast;
            if (AITimer > startToFade)
            {
                Dialogue_ScreenInterpolant = 1f - InverseLerp(startToFade, startToFade + DesperationDrama_ScreenPullupTime, AITimer);
                Dialogue_FindingChannel = true;
                this.Sync();
            }
            else
            {
                if (Dialogue_FindingChannel)
                {
                    Dialogue_FindingChannel = false;
                    this.Sync();
                }
            }

            if (!DesperationDrama_BeginDialogue)
            {
                Dialogue_Manager.Start();
                DesperationDrama_BeginDialogue = true;
                this.Sync();
            }
            Dialogue_Manager.Update(.02f);

            // sorry multiplayer nerds guess you'll just have to WAIT
            // though legitimately I dont know why this isn't syncing and i dont wanna make a packet
            if (Main.netMode == NetmodeID.SinglePlayer && Keys.LeftAlt.GetKeyDown())
            {
                AITimer = (int)startToFade;
                Dialogue_Manager.CurrentProgress = DialogueTime;
                this.Sync();
            }

            int heatstink = DesperationDrama_Wait + TimeToHeatsink;
            if (AITimer == heatstink)
                AdditionsSound.SteamRelease.Play(NPC.Center, 1.5f, -.3f);
            if (AITimer >= heatstink)
            {
                ParticleRegistry.SpawnMistParticle(LeftVentPosition, Vector2.UnitX.RotatedByRandom(.08f) * Main.rand.NextFloat(8f, 24f) * -Direction, Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(130f, 180f));
                ParticleRegistry.SpawnMistParticle(RightVentPosition, Vector2.UnitX.RotatedByRandom(.08f) * Main.rand.NextFloat(8f, 24f) * -Direction, Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(130f, 180f));
            }

            if (AITimer == (DesperationDrama_Wait + TimeToChange))
                AdditionsSound.AsterlinChange.Play(NPC.Center, 2f, .3f);
        }
    }
}