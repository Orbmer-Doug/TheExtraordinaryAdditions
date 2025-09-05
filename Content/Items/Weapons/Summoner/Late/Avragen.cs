using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;

public class Avragen : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Avragen);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Lerp(Color.Violet, Color.PaleVioletRed, (float)MathF.Sin(Main.GlobalTimeWrappedHourly * 6f)));
    }
    public override void SetDefaults()
    {
        Item.damage = 2050;
        Item.knockBack = 4f;
        Item.mana = 10;
        Item.shoot = ModContent.ProjectileType<AvragenMinion>();
        Item.buffType = ModContent.BuffType<AvragenPresence>();
        Item.width = (Item.height = 74);
        Item.useTime = (Item.useAnimation = 10);
        Item.DamageType = DamageClass.Summon;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.UseSound = SoundID.DD2_BetsyFlameBreath;
        Item.rare = ModContent.RarityType<ShadowRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.noMelee = true;
    }

    public override bool CanUseItem(Player player)
    {
        if (player.ownedProjectileCounts[Item.shoot] <= 0)
        {
            return player.maxMinions >= 12;
        }
        return false;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);
        int minion = Projectile.NewProjectile((IEntitySource)(object)source, Main.MouseWorld, Utils.NextVector2Circular(Main.rand, 2f, 2f), ModContent.ProjectileType<AvragenMinion>(), damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("ShadowspecBar", out ModItem ShadowspecBar) && calamityMod.TryFind("YharonsKindleStaff", out ModItem YharonsKindleStaff) && calamityMod.TryFind("EndoHydraStaff", out ModItem EndoHydraStaff) && calamityMod.TryFind("DraedonsForge", out ModTile DraedonsForge))
        {
            recipe.AddIngredient(YharonsKindleStaff.Type, 1);
            recipe.AddIngredient(EndoHydraStaff.Type, 1);
            recipe.AddIngredient(ShadowspecBar.Type, 5);
            recipe.AddTile(DraedonsForge.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.LunarBar, 25);
            recipe.AddIngredient(ItemID.EmpressBlade, 1);
            recipe.AddTile(TileID.LunarMonolith);
        }
        recipe.Register();
    }
}
