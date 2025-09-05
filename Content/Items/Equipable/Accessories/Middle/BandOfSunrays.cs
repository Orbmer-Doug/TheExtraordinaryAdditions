using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

public class BandOfSunrays : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BandOfSunrays);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 248, 173));
    }

    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 26;
        Item.maxStack = 1;
        Item.defense = 2;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.accessory = true;
        Item.rare = ModContent.RarityType<UniqueRarity>();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            IEntitySource source = player.GetSource_ItemUse(Item, null);
            if (player.ownedProjectileCounts[ModContent.ProjectileType<LightSpirit>()] == 0)
            {
                StatModifier totalDamage = player.GetTotalDamage(player.GetBestClass());
                int damage = (int)((StatModifier)totalDamage).ApplyTo(150f);
                for (int i = 0; i < 3; i++)
                {
                    Projectile star = Main.projectile[player.NewPlayerProj(player.Center, Vector2.Zero, ModContent.ProjectileType<LightSpirit>(), damage, 0f, player.whoAmI, MathHelper.TwoPi * i / 3f)];
                    star.rotation = star.ai[0];
                    star.originalDamage = damage / 2;
                    star.netUpdate = true;
                }
            }
        }

        player.GetModPlayer<GlobalPlayer>().LightSpiritBand = true;
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.Gold.ToVector3() * .5f);
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        {
            recipe.AddIngredient(ModContent.ItemType<WrithingLight>(), 3);
            recipe.AddIngredient(ItemID.Shackle, 1);
            recipe.AddTile(TileID.MythrilAnvil);
        }
        recipe.Register();
    }
}