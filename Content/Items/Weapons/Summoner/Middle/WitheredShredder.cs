using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class WitheredShredder : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WitheredShredder);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 114;
        Item.knockBack = 2f;
        Item.mana = 10;
        Item.width = 78;
        Item.height = 80;
        Item.useTime = 16;
        Item.useAnimation = 16;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
        Item.UseSound = SoundID.Item44;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Summon;
        Item.buffType = ModContent.BuffType<FlockOfRazorShields>();
        Item.shoot = ModContent.ProjectileType<WitheredShredderShield>();
        Item.noUseGraphic = true;
    }

    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        position = Main.MouseWorld;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);
        Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
        return false;
    }
}
