using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class LooseSawblade : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LooseSawblade);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(112, 117, 59));
    }
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 99;
    }
    public override void SetDefaults()
    {
        Item.damage = 20;
        Item.knockBack = 1.5f;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.rare = ItemRarityID.Orange;
        Item.useTime = 25;
        Item.useAnimation = 25;
        Item.width = 26;
        Item.height = 26;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.UseSound = SoundID.Item1;
        Item.shootSpeed = 11f;
        Item.shoot = ModContent.ProjectileType<LooseSawbladeProj>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.crit = 10;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.DamageType = DamageClass.Ranged;
    }
    public ref int Counter => ref Main.LocalPlayer.Additions().LooseSawbladeCounter;
    public const int CountNeeded = 300;
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = Item.ThisItemTexture();

        if (Counter >= CountNeeded)
        {
            Color backglow = Color.DarkRed;
            Vector2 spinPoint = -Vector2.UnitY * (9f * (Sin01(Main.GlobalTimeWrappedHourly) * .5f + .5f));
            float rotation = Main.GlobalTimeWrappedHourly * 2f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 spinStart = position + Utils.RotatedBy(spinPoint, (double)(rotation - (float)Math.PI * i / 4f), default);
                Color glowAlpha = Item.GetAlpha(backglow);
                glowAlpha.A = 125;
                spriteBatch.Draw(texture, spinStart, null, glowAlpha * .85f, 0f, origin, scale, 0, 0f);
            }
        }

        spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, scale, 0, 0f);
        return false;
    }
    public override void UpdateInventory(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            bool item = player.HeldItem.ModItem is LooseSawblade;
            if (item && Counter <= CountNeeded)
            {
                Counter++;
            }
            else if (!item)
                Counter = 0;

            if (Counter <= CountNeeded)
            {
                if (Counter % 50 == 49)
                {
                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Volume = .5f }, player.Center);
                }
                if (Counter % 100 == 99)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / 30f + offsetAngle).ToRotationVector2() * 5f;
                        Dust dust = Dust.NewDustPerfect(player.Center, DustID.Bone, shootVelocity, default, default, 1.6f);
                        dust.noGravity = true;
                    }

                    SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Volume = .5f, Pitch = 1.3f }, player.Center);
                }
            }

            if (Counter == CountNeeded)
            {
                SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 1.3f }, player.Center);
                CombatText.NewText(player.Hitbox, Color.SandyBrown, "Charged!", true);
            }
        }
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (Counter >= CountNeeded)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(1.5f, 2f);
                LooseSawbladeProj saw = Main.projectile[Projectile.NewProjectile(source, position, vel, type, damage * 2, knockback * 2, player.whoAmI)].As<LooseSawbladeProj>();
                saw.Supercharged = true;
                saw.Sync();
            }
        }
        else
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 0f, 0f);
        }

        Counter = 0;
        return false;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(150);
        recipe.AddIngredient(ItemID.Bone, 4);
        recipe.AddRecipeGroup("AnySilverBar", 1);
        recipe.Register();
    }
}
