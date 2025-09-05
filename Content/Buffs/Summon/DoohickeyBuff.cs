using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class DoohickeyBuff : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DoohickeyBuff);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        bool _ = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<DoohickeyRun>());
    }
}