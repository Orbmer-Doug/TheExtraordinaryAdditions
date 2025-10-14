using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class EtherealClaymore : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EtherealClaymore);

    public override void SetDefaults()
    {
        Item.width = 126;
        Item.height = 126;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.useTime = 40;
        Item.useAnimation = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 1f;
        Item.autoReuse = true;
        Item.damage = 450;
        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shootSpeed = 10;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<EtherealSwing>();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.LightBlue);
    }

    public override bool CanUseItem(Player player)
    {
        return !player.channel && player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<MythicScrap>(), 12);
        recipe.AddIngredient(ItemID.Ectoplasm, 14);
        recipe.AddCondition(Condition.InSkyHeight, Condition.TimeDay);

        recipe.AddTile(TileID.MythrilAnvil);

        recipe.AddOnCraftCallback(new Recipe.OnCraftCallback(OnCreated));

        recipe.Register();
    }

    public static void OnCreated(Recipe recipe, Item item, List<Item> consumedItems, Item destinationStack)
    {
        Vector2 pos = Main.LocalPlayer.Center;
        AdditionsSound.LightningStrike.Play(pos, .7f);
        ParticleRegistry.SpawnThunderParticle(pos, 30, 1.6f, new(1f), 0f, Color.CornflowerBlue);
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnLightningArcParticle(pos, Main.rand.NextVector2CircularLimited(200f, 200f, .5f, 1f), Main.rand.Next(10, 20), Main.rand.NextFloat(.4f, .6f), Color.CornflowerBlue);
        }
    }
}