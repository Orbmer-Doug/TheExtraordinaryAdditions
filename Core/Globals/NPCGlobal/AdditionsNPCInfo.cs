using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TheExtraordinaryAdditions.Core.Globals.NPCGlobal;

public class AdditionsNPCInfo : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        if (lateInstantiation)
        {
            if (entity.ModNPC == null || entity.type < NPCID.Count || entity.ModNPC.Mod != AdditionsMain.Instance || entity.ModNPC.Mod.Name != AdditionsMain.Instance.Name)
                return false;
            return true;
        }
        return false;
    }

    public const byte TotalExtraAISlots = 30;
    public float[] ExtraAI = new float[TotalExtraAISlots];

    public override void SetDefaults(NPC npc)
    {
        for (int i = 0; i < TotalExtraAISlots; i++)
            ExtraAI[i] = 0f;
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        for (int i = 0; i < TotalExtraAISlots; i++)
            binaryWriter.Write((float)ExtraAI[i]);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        for (int i = 0; i < TotalExtraAISlots; i++)
            ExtraAI[i] = (float)binaryReader.ReadSingle();
    }
}
