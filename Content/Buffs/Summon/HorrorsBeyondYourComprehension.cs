using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class HorrorsBeyondYourComprehension : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HorrorsBeyondYourComprehension);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        bool _ = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<LilBloodSquid>());
    }
}