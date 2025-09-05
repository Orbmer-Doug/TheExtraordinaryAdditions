using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Netcode;

// Not reliable
public class AdditionsNetcode
{
    public static void SyncWorld()
    {
        if (Main.dedServ)
            NetMessage.SendData(MessageID.WorldData, -1, -1, null, 0, 0f, 0f, 0f, 0, 0, 0);
    }
}