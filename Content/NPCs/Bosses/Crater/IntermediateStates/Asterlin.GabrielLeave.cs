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
    public void LoadStateTransitions_GabrielLeave()
    {
        StateMachine.RegisterStateBehavior(AsterlinAIType.GabrielLeave, DoBehavior_GabrielLeave);
    }

    public void DoBehavior_GabrielLeave()
    {

    }
}
