using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Early;

public class FungalSatchel : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FungalSatchel);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.AliceBlue);
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 40;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.accessory = true;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, new Color(95, 110, 255).ToVector3() * 1.5f);
        player.GetModPlayer<FungalSatchelPlayer>().Equipped = true;
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, new Color(95, 110, 255).ToVector3() * 1f);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        {
            recipe.AddIngredient(ItemID.GlowingMushroom, 150);
            recipe.AddIngredient(ItemID.ShinePotion, 3);
            recipe.AddIngredient(ItemID.Leather, 5);
            recipe.AddTile(TileID.WorkBenches);
        }
        recipe.Register();
    }
}

public sealed class FungalSatchelPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        OnHitWithAnything(item, target, hit, damageDone);
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        OnHitWithAnything(proj, target, hit, damageDone);
    }

    private void OnHitWithAnything(Entity entity, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped && hit.Crit && Main.rand.NextBool() && Player.CountOwnerProjectiles(ModContent.ProjectileType<HealingFungus>()) <= 1)
        {
            Vector2 pos = target.RandAreaInEntity();
            Vector2 vel = Utility.GetHomingVelocity(pos, Player.Center, Player.velocity, 6f);
            if (Main.myPlayer == Player.whoAmI)
                Player.NewPlayerProj(pos, vel, ModContent.ProjectileType<HealingFungus>(), 0, 0f, Player.whoAmI);
        }
    }
}