using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TheExtraordinaryAdditions.Core.Globals;

public class AdditionsNPCInfo : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return lateInstantiation && entity.type >= NPCID.Count && entity.ModNPC.Mod.Name == AdditionsMain.Instance.Name;
    }

    public const byte TotalExtraAISlots = 30;
    public float[] ExtraAI = new float[TotalExtraAISlots];

    public override void SetDefaults(NPC npc)
    {
        // Initialize the slots
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = 0f;
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        for (int i = 0; i < ExtraAI.Length; i++)
            binaryWriter.Write((float)ExtraAI[i]);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        for (int i = 0; i < ExtraAI.Length; i++)
            ExtraAI[i] = (float)binaryReader.ReadSingle();
    }
}
