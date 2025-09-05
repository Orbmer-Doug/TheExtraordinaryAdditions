using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_QuickHyperbeam()
    {
        StateMachine.RegisterTransition(AsterlinAIType.QuickHyperbeam, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.GabrielLeave, 1f } }, false, () =>
        {
            return FightStarted;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.QuickHyperbeam, DoBehavior_QuickHyperbeam);
    }

    public void DoBehavior_QuickHyperbeam()
    {

    }
}