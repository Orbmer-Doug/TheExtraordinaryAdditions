using System.IO;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((bool)NPC.dontTakeDamage);
        Dialouge_SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.dontTakeDamage = (bool)reader.ReadBoolean();
        Dialogue_RecieveExtraAI(reader);
    }
}
