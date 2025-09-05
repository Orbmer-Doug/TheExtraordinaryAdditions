using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Swings()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Swings, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.RotatedDicing, 1f }, { AsterlinAIType.Barrage, 1f } }, false, () =>
        {
            return NPC.AdditionsInfo().ExtraAI[0] >= Swings_MaxSwingCount && Sword == null;
        });
        StateMachine.RegisterStateEntryCallback(AsterlinAIType.Swings, () => { NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<CyberneticSword>(), MediumAttackDamage, 0f); });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Swings, DoBehavior_Swings);
    }

    public static int Swings_MaxSwingCount => DifficultyBasedValue(4, 5, 6, 7, 8, 9);
    public static int Swings_DartAmount => DifficultyBasedValue(8, 12, 10, 12, 12, 8);
    public static int Swings_DartWaves => DifficultyBasedValue(1, 1, 2, 2, 2, 1);
    public static int Swings_SwingSpeed => DifficultyBasedValue(50, 40, 35, 30, 30, 16);

    public void DoBehavior_Swings()
    {
        Vector2 hoverDestination = Target.Center + new Vector2((NPC.Center.X > Target.Center.X).ToDirectionInt() * 175f, -60f);
        float distanceToDestination = NPC.Distance(hoverDestination);
        Vector2 idealVelocity = NPC.SafeDirectionTo(hoverDestination) * MathHelper.Min(distanceToDestination, 10f);
        NPC.SimpleFlyMovement(Vector2.Lerp(idealVelocity, (hoverDestination - NPC.Center) * 0.15f, InverseLerp(280f, 540f, distanceToDestination)), 0.7f);
    }
}
