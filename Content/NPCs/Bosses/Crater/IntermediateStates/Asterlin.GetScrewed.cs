using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

/// Do a silly animation upon killing all players
public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_GetScrewed()
    {
        StateMachine.RegisterTransition(AsterlinAIType.GetScrewed, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Swings, 1f }, { AsterlinAIType.RotatedDicing, 1f }, { AsterlinAIType.Barrage, 1f } }, false, () =>
        {
            return FightStarted;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.GetScrewed, DoBehavior_GetScrewed);
    }

    public void DoBehavior_GetScrewed()
    {

    }
}
