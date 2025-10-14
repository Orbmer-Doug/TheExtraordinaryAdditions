using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Pets;

public class CrimsonCalamari : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrimsonCalamari);
    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.width = 52;
        Item.height = 28;
        Item.UseSound = SoundID.NPCHit9;
        Item.shoot = ModContent.ProjectileType<LilBloodSquid>();
        Item.buffType = ModContent.BuffType<HorrorsBeyondYourComprehension>();
        Item.value = Item.sellPrice(0, 5, 0, 0);
        Item.rare = ItemRarityID.Master;
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
        {
            player.AddBuff(Item.buffType, 3600, true, false);
        }
    }
}
