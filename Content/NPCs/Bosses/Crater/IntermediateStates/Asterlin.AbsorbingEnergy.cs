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
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_AbsorbingEnergy()
    {
        StateMachine.RegisterTransition(AsterlinAIType.AbsorbingEnergy, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Barrage, 1f } }, false, () =>
        {
            return FightStarted;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.AbsorbingEnergy, DoBehavior_AbsorbingEnergy);
    }

    public void DoBehavior_AbsorbingEnergy()
    {
        if (AITimer == 0)
        {
            NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<CondensedSoulMass>(), 0, 0f, -1);
        }

        if (Utility.FindProjectile(out Projectile mass, ModContent.ProjectileType<CondensedSoulMass>()))
        {
            SetHeadRotation(EyePosition.AngleTo(mass.Center + Vector2.UnitX * MathF.Cos(Main.GlobalTimeWrappedHourly * .5f) * 40f));
            SetRightHandTarget(mass.Center + Vector2.UnitY * MathF.Sin(Main.GlobalTimeWrappedHourly) * 50f);
            SetLeftLegRotation(-1.5f);
            SetRightLegRotation(-1.5f);
            SetDirection((mass.Center.X > NPC.Center.X).ToDirectionInt());
            SetLegFlamesInterpolant(0f);
        }

        NPC.dontTakeDamage = true;
    }
}
