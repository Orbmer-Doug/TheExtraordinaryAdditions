using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Pets;

public class JellyfishSnack : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.JellyfishSnack);
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.WispinaBottle);
        Item.damage = 0;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.width = 30;
        Item.height = 32;
        Item.UseSound = SoundID.NPCHit18;
        Item.shoot = ModContent.ProjectileType<JellyfishVro>();
        Item.buffType = ModContent.BuffType<BubbleMan>();
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
