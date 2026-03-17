using System.IO;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin
{
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(NPC.dontTakeDamage);
        writer.Write(NumUpdates);
        writer.Write(ExtraUpdates);
        writer.Write(NPC.Opacity);

        if (StateMachine != null)
        {
            writer.Write((int)CurrentState);
            writer.Write(AITimer);
        }

        Dialogue_SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.dontTakeDamage = reader.ReadBoolean();
        NumUpdates = reader.ReadInt32();
        ExtraUpdates = reader.ReadInt32();
        NPC.Opacity = reader.ReadSingle();

        if (StateMachine != null)
        {
            AsterlinAIType receivedState = (AsterlinAIType)reader.ReadInt32();
            int receivedTime = reader.ReadInt32();
            StateMachine.StateStack.Clear();
            CurrentState = receivedState;
            AITimer = receivedTime;
        }

        Dialogue_RecieveExtraAI(reader);
    }
}
