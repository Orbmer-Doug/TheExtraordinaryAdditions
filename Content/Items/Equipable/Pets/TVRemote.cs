using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Pets;

public class TVRemote : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TVRemote);
    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.width = 50;
        Item.height = 36;
        Item.UseSound = AssetRegistry.GetSound(AdditionsSound.AsterlinHit);
        Item.shoot = ModContent.ProjectileType<FloatingScreen>();
        Item.buffType = ModContent.BuffType<JudgingAsterlin>();
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
