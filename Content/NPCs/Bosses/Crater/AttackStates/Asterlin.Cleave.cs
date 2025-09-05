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
    public void LoadStateTransitions_Cleave()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Cleave, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Disintegration, 1f }, { AsterlinAIType.Lightripper, 1f } }, false, () =>
        {
            return Cleave_FadeTimer >= Cleave_FadeTime;
        });

        // State transition checks happen after behavior, meaning by the time it reaches back to behaviors setup with the meltdown will already have been set
        StateMachine.RegisterStateEntryCallback(AsterlinAIType.Cleave, () => { NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<TheTesselesticMeltdown>(), MediumAttackDamage, 0f); });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Cleave, DoBehavior_Cleave);
    }

    public static int Cleave_Extension => DifficultyBasedValue(11, 13, 17, 19, 21, 25);
    public static int Cleave_WaitTime => 102;
    public static int Cleave_TotalCycles => DifficultyBasedValue(5, 5, 6, 6, 7, 7);
    public static int Cleave_FadeTime => SecondsToFrames(1.8f);
    public static int Cleave_HoverTime => SecondsToFrames(2.2f);
    public int Cleave_Cycle
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = value;
    }
    public int Cleave_FadeTimer
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = value;
    }

    public void DoBehavior_Cleave()
    {
        if (AITimer < Cleave_HoverTime)
        {
            NPC.velocity = Vector2.SmoothStep(NPC.Center, Target.Center - Vector2.UnitY * RotatedHitbox.Height, .26f) - NPC.Center;
        }
        else
        {
            if (Cleave_Cycle < Cleave_TotalCycles)
            {
                if (AITimer >= Cleave_WaitTime)
                {
                    if (Staff != null)
                    {
                        Staff.CurrentState = Content.Projectiles.Magic.Late.TesselesticMeltdownProj.State.Barrage;
                        SetRightHandTarget(Vector2.SmoothStep(RightHandTarget, rightArm.RootPosition + PolarVector(400f, Staff.Projectile.velocity.ToRotation()), Utils.Remap(AITimer, Cleave_WaitTime, Cleave_WaitTime * 1.5f, .08f, .4f)));
                    }
                }

                if (AITimer % Cleave_WaitTime == (Cleave_WaitTime - 1))
                {
                    for (int j = 0; j < Cleave_Extension; j++)
                    {
                        Vector2 center = NPC.Center;
                        PillarFalling pillar =
                            Main.projectile[NPC.NewNPCProj(center, new Vector2((Target.Center.X - center.X) * 0.02f + j * Utils.NextFloat(Main.rand, -10f, 10f), 0f), ModContent.ProjectileType<PillarFalling>(), MediumAttackDamage, 0f)]
                            .As<PillarFalling>();
                        pillar.Time = -20 - j - Main.rand.Next(0, 8);
                    }
                    Cleave_Cycle++;
                }
            }
            else
            {
                if (Staff != null)
                    Staff.CurrentState = Content.Projectiles.Magic.Late.TesselesticMeltdownProj.State.Idle;
                Cleave_FadeTimer++;
            }

            NPC.velocity = Vector2.SmoothStep(NPC.Center, Target.Center - Vector2.UnitY * RotatedHitbox.Height, Utils.Remap(AITimer, Cleave_HoverTime, Cleave_HoverTime + 25, .26f, .06f)) - NPC.Center;
        }
    }
}
