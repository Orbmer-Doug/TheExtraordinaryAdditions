using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class BubbleMan : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BubbleMan);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        bool _ = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<JellyfishVro>());
    }
}