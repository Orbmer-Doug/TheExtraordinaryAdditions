using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Summon;

public class WorldShatteredFragment : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WorldShatteredFragment);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemNoGravity[Item.type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.useAnimation = 10;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.consumable = false;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<ShatteredComet>();
        Item.shootSpeed = 10f;
        Item.UseSound = SoundID.Item88;
    }
    public override bool CanUseItem(Player player)
    {
        int proj = ModContent.ProjectileType<ShatteredComet>();
        return player.ownedProjectileCounts[proj] <= 0;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        int proj = ModContent.ProjectileType<ShatteredComet>();
        if (player.ownedProjectileCounts[proj] <= 0)
        {
            float speed = 30f;

            Vector2 targetpos = Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY);
            position = player.Center - new Vector2(Main.rand.NextFloat(401) * player.direction, 800f);
            position.Y -= 200;
            Vector2 heading = targetpos - position;

            if (heading.Y < 0f)
            {
                heading.Y *= -1f;
            }

            if (heading.Y < 20f)
            {
                heading.Y = 20f;
            }

            heading.Normalize();
            heading *= speed;
            heading.Y += Main.rand.Next(-10, 11) * 0.02f;
            Projectile.NewProjectile(source, position, heading, proj, 0, 0f, player.whoAmI);
        }
        return false;
    }
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
    }
    public override void PostUpdate()
    {
        // Make fire effects in world
        if (Main.GameUpdateCount % 5f == 0f)
        {
            Vector2 pos = Item.Center;
            Vector2 vel = RandomVelocity(2f, 2f, 4f);
            Color color = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, Main.rand.NextFloat(.3f, .9f));
            float scale = Main.rand.NextFloat(.3f, .5f);
            int life = Main.rand.Next(17, 30);

        }
        Lighting.AddLight(Item.Center, Color.Cyan.ToVector3());
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("CosmiliteBar", out ModItem CosmiliteBar) && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil))
        {
            recipe.AddIngredient(CosmiliteBar.Type, 10);
            recipe.AddIngredient(ItemID.MeteoriteBar, 30);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.LunarBar, 20);
            recipe.AddIngredient(ItemID.MeteoriteBar, 30);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}
