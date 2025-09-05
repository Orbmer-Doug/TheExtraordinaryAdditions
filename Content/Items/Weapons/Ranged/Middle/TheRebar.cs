using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class TheRebar : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Rebar);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void SetDefaults()
    {
        Item.damage = 38;
        Item.scale = 1;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 10;
        Item.height = 36;
        Item.useTime = Item.useAnimation = 5;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = false;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.UseSound = SoundID.Item1;
        Item.shoot = ModContent.ProjectileType<Rebar>();
        Item.autoReuse = true;
        Item.shootSpeed = 11f;
        Item.crit = 0;
        Item.noUseGraphic = true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile((IEntitySource)(object)source, position, velocity, ModContent.ProjectileType<Rebar>(), damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }
    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        Vector2 muzzleOffset = Vector2.Normalize(velocity) * 25f;

        if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 7, 0))
        {
            position += muzzleOffset;
        }
    }
}