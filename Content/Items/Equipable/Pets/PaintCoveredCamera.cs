using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Pets;

public class PaintCoveredCamera : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PaintCoveredCamera);
    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.width = 52;
        Item.height = 28;
        Item.UseSound = SoundID.Item1 with { Pitch = .4f };
        Item.shoot = ModContent.ProjectileType<DoohickeyRun>();
        Item.buffType = ModContent.BuffType<DoohickeyBuff>();
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
        {
            player.AddBuff(Item.buffType, 3600, true, false);
        }
    }
}