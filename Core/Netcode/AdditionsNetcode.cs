using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Tools;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.UI.GodDummyUI;
//using static Terraria.ModLoader.BackupIO;

namespace TheExtraordinaryAdditions.Core.Netcode;

public enum AdditionsModMessageType : byte
{
    SyncSuperBloodMoon,
    SyncBossDefeats,
    SpawnNPCOnPlayer,
    SpawnGodDummy,
    DeleteGodDummy,
}

/// <summary>
/// Facilitates all handling of packets in the mod to make things work on multiplayer <br></br>
/// Reminder: Check if something is synced by checking <see cref="MessageID"/> and looking for it in <see cref="NetMessage.SendData(int, int, int, Terraria.Localization.NetworkText, int, float, float, float, int, int, int)"/>
/// </summary>
public class AdditionsNetcode
{
    public static void HandlePackets(Mod mod, BinaryReader reader, int whoAmI)
    {
        try
        {
            AdditionsModMessageType msgType = (AdditionsModMessageType)reader.ReadByte();
            switch (msgType)
            {
                case AdditionsModMessageType.SyncSuperBloodMoon:
                    int sender = reader.ReadInt32();
                    SuperBloodMoonSystem.SuperBloodMoon = reader.ReadBoolean();
                    if (Main.netMode == NetmodeID.Server)
                        SyncAdditionsBloodMoon(sender);
                    break;

                case AdditionsModMessageType.SyncBossDefeats:
                    int sender2 = reader.ReadInt32();
                    BossDownedSaveSystem.downedRegistry.Clear();
                    int downedBossesCount = reader.ReadInt32();
                    for (int i = 0; i < downedBossesCount; i++)
                        BossDownedSaveSystem.downedRegistry.Add(reader.ReadString());
                    if (Main.netMode == NetmodeID.Server)
                        SyncBossDefeats(sender2);
                    break;

                case AdditionsModMessageType.SpawnNPCOnPlayer:
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    int npcType = reader.ReadInt32();
                    int player = reader.ReadInt32();
                    Vector2 spawnPosition = reader.ReadVector2();
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int spawnedNPC = NPC.NewNPC(new EntitySource_WorldEvent(), x, y, npcType, Target: player);
                        NetMessage.SendData(MessageID.SyncNPC, -1, player, null, spawnedNPC);
                    }
                    break;

                case AdditionsModMessageType.SpawnGodDummy:
                    int x2 = reader.ReadInt32();
                    int y2 = reader.ReadInt32();
                    int life = reader.ReadInt32();
                    int defense = reader.ReadInt32();
                    float size = reader.ReadSingle();
                    bool gravity = reader.ReadBoolean();
                    float rotation = reader.ReadSingle();
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int index = NPC.NewNPC(new EntitySource_WorldEvent(), x2, y2, ModContent.NPCType<GodDummyNPC>());
                        NPC dum = Main.npc[index];
                        dum.life = dum.lifeMax = life;
                        dum.defense = defense;
                        dum.scale = size;
                        dum.noGravity = gravity;
                        dum.rotation = rotation;
                        dum.netUpdate = true;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
                    }
                    break;

                case AdditionsModMessageType.DeleteGodDummy:
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        GodDummy.DeleteDummies();
                    break;
            }
        }
        catch (Exception e)
        {
            if (e is EndOfStreamException eose)
                AdditionsMain.Instance.Logger.Error("Failed to parse Additions packet: Packet was too short, missing data, or otherwise corrupt.", eose);
            else if (e is ObjectDisposedException ode)
                AdditionsMain.Instance.Logger.Error("Failed to parse Additions packet: Packet reader disposed or destroyed.", ode);
            else if (e is IOException ioe)
                AdditionsMain.Instance.Logger.Error("Failed to parse Additions packet: An unknown I/O error occurred.", ioe);
            else
                throw; // this either will crash the game or be caught by TML's packet policing
        }
    }

    public static void SyncAdditionsBloodMoon(int sender)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket netMessage = AdditionsMain.Instance.GetPacket();
        netMessage.Write((byte)AdditionsModMessageType.SyncSuperBloodMoon);
        netMessage.Write(sender);
        netMessage.Write(SuperBloodMoonSystem.SuperBloodMoon);

        netMessage.Send(-1, sender);
    }

    public static void SyncBossDefeats(int sender)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket netMessage = AdditionsMain.Instance.GetPacket();
        netMessage.Write((byte)AdditionsModMessageType.SyncBossDefeats);
        netMessage.Write(sender);
        netMessage.Write(BossDownedSaveSystem.downedRegistry.Count);
        for (int i = 0; i < BossDownedSaveSystem.downedRegistry.Count; i++)
            netMessage.Write(BossDownedSaveSystem.downedRegistry[i]);

        netMessage.Send(-1, sender);
    }

    public static void NewNPC_ClientSide(Vector2 spawnPosition, int npcType, Player player)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPosition.X, (int)spawnPosition.Y, npcType, Target: player.whoAmI);
            return;
        }

        ModPacket netMessage = AdditionsMain.Instance.GetPacket();
        netMessage.Write((byte)AdditionsModMessageType.SpawnNPCOnPlayer);
        netMessage.Write((int)spawnPosition.X);
        netMessage.Write((int)spawnPosition.Y);
        netMessage.Write(npcType);
        netMessage.Write(player.whoAmI);
        netMessage.Send();
    }

    public static void SpawnGodDummy(Vector2 spawnPosition)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            NPC dum = Main.npc[NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<GodDummyNPC>())];
            dum.life = dum.lifeMax = DummyUI.MaxLife;
            dum.defense = DummyUI.Defense;
            dum.scale = DummyUI.Size;
            dum.noGravity = DummyUI.Gravity;
            dum.rotation = DummyUI.Rotation;
            return;
        }

        ModPacket netMessage = AdditionsMain.Instance.GetPacket();
        netMessage.Write((byte)AdditionsModMessageType.SpawnGodDummy);
        netMessage.Write((int)spawnPosition.X);
        netMessage.Write((int)spawnPosition.Y);
        netMessage.Write((int)DummyUI.MaxLife);
        netMessage.Write((int)DummyUI.Defense);
        netMessage.Write((float)DummyUI.Size);
        netMessage.Write((bool)DummyUI.Gravity);
        netMessage.Write((float)DummyUI.Rotation);

        netMessage.Send();
    }

    public static void SyncWorld()
    {
        if (Main.dedServ)
            NetMessage.SendData(MessageID.WorldData, -1, -1, null, 0, 0f, 0f, 0f, 0, 0, 0);
    }
}