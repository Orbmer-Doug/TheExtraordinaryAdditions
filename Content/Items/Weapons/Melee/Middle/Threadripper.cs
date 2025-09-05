using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class Threadripper : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Threadripper);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.Yoyo[Item.type] = true;
        ItemID.Sets.GamepadExtraRange[Item.type] = 15;
        ItemID.Sets.GamepadSmartQuickReach[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 40;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.damage = 110;
        Item.knockBack = 2.5f;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.autoReuse = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.UseSound = SoundID.Item1;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<ThreadripperYo>();
        Item.shootSpeed = 36f;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
    }
}
