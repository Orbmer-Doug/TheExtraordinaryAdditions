using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.CrossUI;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

public class AncientBoon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AncientBoon);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(252, 255, 99));
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
        var modPlayer = player.GetModPlayer<ElementalBalance>();
        modPlayer.ElementalResourceRegenRate *= 1.111f;

        if (!player.slowFall && player.wingTime < player.wingTimeMax && !player.controlJump && player.miscCounter % 2 == 0)
            player.wingTime += 1.8f;
        
        player.statDefense *= 1.16f;
        player.GetModPlayer<AncientBoonPlayer>().Equipped = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SoulofFlight, 40);
        recipe.AddIngredient(ModContent.ItemType<CoreofCalamity>(), 5);
        recipe.AddIngredient(ModContent.ItemType<GalacticaSingularity>(), 12);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 5);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}

public sealed class AncientBoonPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;
    public override void PostHurt(Player.HurtInfo info)
    {
        if (!Equipped)
            return;

        if (Main.myPlayer == Player.whoAmI)
        {
            if (info.Damage > 0)
            {
                int dmg = (int)Player.GetTotalDamage<GenericDamageClass>().ApplyTo(800f);
                float rand = RandomRotation();
                int dir = Main.rand.NextBool().ToDirectionInt();
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vel = PolarVector(6f, MathHelper.TwoPi * InverseLerp(0f, 8f, i) + rand);
                    AncientRetaliation proj = Main.projectile[Player.NewPlayerProj(Player.MountedCenter, vel,
                        ModContent.ProjectileType<AncientRetaliation>(), dmg, 1f, Player.whoAmI)].As<AncientRetaliation>();
                    proj.Direction = dir;
                }
            }
        }
    }
}