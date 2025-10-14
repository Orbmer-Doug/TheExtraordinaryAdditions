using System.IO;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((bool)NPC.dontTakeDamage);
        writer.Write((int)NumUpdates);
        writer.Write((int)ExtraUpdates);
        writer.Write((float)NPC.Opacity);

        if (StateMachine != null)
        {
            writer.Write((int)CurrentState);
            writer.Write((int)AITimer);
        }

        Dialouge_SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.dontTakeDamage = (bool)reader.ReadBoolean();
        NumUpdates = (int)reader.ReadInt32();
        ExtraUpdates = (int)reader.ReadInt32();
        NPC.Opacity = (float)reader.ReadSingle();

        if (StateMachine != null)
        {
            AsterlinAIType receivedState = (AsterlinAIType)reader.ReadInt32();
            int receivedTime = (int)reader.ReadInt32();
            StateMachine.StateStack.Clear();
            CurrentState = receivedState;
            AITimer = receivedTime;
        }

        Dialogue_RecieveExtraAI(reader);
    }
}
