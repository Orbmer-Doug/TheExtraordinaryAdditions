using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Barrage()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Barrage, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Swings, 1f }, { AsterlinAIType.RotatedDicing, 1f } }, false, () =>
        {
            return AITimer >= Barrage_TotalTime && !AnyProjectile(ModContent.ProjectileType<BarrageBeam>());
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Barrage, DoBehavior_Barrage);
    }

    public static int Barrage_AttackTime => SecondsToFrames(8f);
    public static int Barrage_FadeTime => SecondsToFrames(.8f);
    public static int Barrage_BeamRate => DifficultyBasedValue(30, 14, 12, 10, 8, 6);
    public static int Barrage_HoverTime => 40;
    public static int Barrage_BeamExpandTime => 44;
    public static int Barrage_BeamTime => 130;
    public static int Barrage_BeamFadeTime => 55;

    public static int Barrage_TotalTime => Barrage_AttackTime + Barrage_FadeTime;

    public void DoBehavior_Barrage()
    {
        if (AITimer >= Barrage_AttackTime)
            ZPosition = InverseLerp(Barrage_AttackTime, Barrage_TotalTime, AITimer);
        else
        {
            SetLookingStraight(true);
            ZPosition = InverseLerp(Barrage_FadeTime, 0f, AITimer);

            if (AITimer >= Barrage_FadeTime)
            {
                if (AITimer % Barrage_BeamRate == (Barrage_BeamRate - 1))
                {
                    if (this.RunServer())
                        NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BarrageBeam>(), MediumAttackDamage, 0f);
                }

                if (AITimer % (Barrage_BeamRate * 2) == (Barrage_BeamRate * 2 - 1))
                {
                    if (this.RunServer())
                        NPC.NewNPCProj(NPC.Center, Main.rand.NextVector2Circular(40f, 40f), ModContent.ProjectileType<DartBomb>(), MediumAttackDamage, 0f);
                }
            }
        }

        CasualHoverMovement();
    }
}
