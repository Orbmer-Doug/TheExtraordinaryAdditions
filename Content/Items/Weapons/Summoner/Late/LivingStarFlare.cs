using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;

public class LivingStarFlare : ModItem
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.ItemNoGravity[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 565;
        Item.knockBack = 2f;
        Item.mana = 10;
        Item.shoot = ModContent.ProjectileType<LivingStarFlareMinion>();
        Item.buffType = ModContent.BuffType<LittleStar>();
        Item.width = Item.height = 16;
        Item.useTime = Item.useAnimation = 10;
        Item.DamageType = DamageClass.Summon;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.UseSound = SoundID.DD2_BetsyFlameBreath;
        Item.rare = ModContent.RarityType<LegendaryRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.noMelee = true;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, player.Additions().mouseWorld, Utils.NextVector2Circular(Main.rand, 2f, 2f), Item.shoot, damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    private static void DrawStar(Vector2 drawPosition)
    {
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.FlameMap1);
        ManagedShader fireball = ShaderRegistry.FireballShader;
        fireball.SetTexture(noise, 1, SamplerState.AnisotropicWrap);
        fireball.TrySetParameter("mainColor", Color.Lerp(Color.Goldenrod, Color.Gold, 0.3f).ToVector3());
        fireball.TrySetParameter("resolution", new Vector2(60f));
        fireball.TrySetParameter("time", Main.GlobalTimeWrappedHourly * (0.04f + 0.32f));
        fireball.TrySetParameter("opacity", 1f);

        Texture2D invis = AssetRegistry.GetTexture(AdditionsTexture.Invisible);
        fireball.Render();
        Main.spriteBatch.Draw(invis, drawPosition, null, Color.White, 0f, invis.Size() * 0.5f, 60f, SpriteEffects.None, 0f);
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

        DrawStar(position);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Main.spriteBatch.PrepareForShaders();
        DrawStar(Item.position - Main.screenPosition + Item.Size / 2);
        Main.spriteBatch.ExitShaderRegion();

        return false;
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.Gold.ToVector3() * 1.6f);
    }
}
