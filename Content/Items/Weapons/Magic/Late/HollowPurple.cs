using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class HollowPurple : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HollowPurple);
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 15, false));

        ItemID.Sets.AnimatesAsSoul[Type] = true;

        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Purple);
    }
    public override void SetDefaults()
    {
        Item.damage = 19500;
        Item.DamageType = DamageClass.Magic;
        Item.width = Item.height = 50;
        Item.useTime = Item.useAnimation = 2;
        Item.knockBack = 0;
        Item.rare = ModContent.RarityType<ShadowRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<TheCursedTechnique>();
        Item.crit = 0;
        Item.mana = 40;
        Item.shootSpeed = 10f;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
    }
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
        return false;
    }

    private static void DrawPurple(Vector2 drawPosition)
    {
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Vector2 scale = new Vector2(TheCursedTechnique.MaxSize / 4) / pixel.Size();
        Color color = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly), TheCursedTechnique.Purple);

        ManagedShader sphere = ShaderRegistry.MagicSphere;
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1, SamplerState.LinearWrap);
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 2, SamplerState.LinearWrap);
        sphere.TrySetParameter("resolution", TheCursedTechnique.Resolution);
        sphere.TrySetParameter("posterizationPrecision", 18f);
        sphere.TrySetParameter("mainColor", color.ToVector3());
        sphere.Render();

        Main.spriteBatch.Draw(pixel, drawPosition, null, Color.White, 0f, pixel.Size() / 2, scale, 0, 0f);
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

        DrawPurple(position);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Main.spriteBatch.PrepareForShaders();
        DrawPurple(Item.position - Main.screenPosition + Item.Size / 2);
        Main.spriteBatch.ExitShaderRegion();

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("ShadowspecBar", out ModItem ShadowspecBar) && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity) && calamityMod.TryFind("DraedonsForge", out ModTile DraedonsForge))
        {
            recipe.AddIngredient(ItemID.NebulaArcanum, 1);
            recipe.AddIngredient(CoreofCalamity.Type, 7);
            recipe.AddIngredient(ShadowspecBar.Type, 10);
            recipe.AddTile(DraedonsForge.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.NebulaArcanum, 1);
            recipe.AddIngredient(ItemID.LunarBar, 50);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}